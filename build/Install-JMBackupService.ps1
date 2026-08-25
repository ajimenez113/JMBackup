<#
.SYNOPSIS
    Instala JMBackup como servicio de Windows.

.DESCRIPTION
    Registra el servicio de Windows apuntando al ejecutable publicado, le otorga a la
    cuenta que lo corre el derecho "Iniciar sesión como servicio" (SeServiceLogonRight),
    abre la regla de firewall entrante para el puerto configurado, y activa las rutas
    largas de NTFS (LongPathsEnabled), necesarias para el escaneo local/UNC (ver
    `LocalStorageBackend`, fase 1).

    Por defecto crea (o reutiliza) una cuenta de servicio local dedicada, sin acceso a
    nada fuera de lo que se le otorgue a mano carpeta por carpeta — el modelo más
    seguro, a costa de tener que darle permiso a cada carpeta de origen/destino que
    esté fuera de `%ProgramData%`. Si en cambio los respaldos van sobre todo a
    recursos de red con tu propia identidad, o preferís que el servicio ya tenga
    acceso a tus carpetas personales sin configurar nada carpeta por carpeta, pasá
    `-ExistingAccountUsername` para que corra con una cuenta de usuario que ya existe
    en vez de crear una dedicada (ver también `Set-JMBackupServiceAccount.ps1`, para
    cambiar la cuenta de una instalación que ya está hecha).

    Requiere ejecutarse elevado (Administrador).

.PARAMETER InstallPath
    Carpeta donde está publicado JMBackup.Api.exe
    (`dotnet publish src/JMBackup.Api -c Release -r win-x64 --self-contained
    -p:PublishSingleFile=true -o publish`). Por defecto, `publish\` junto a `build\`.

.PARAMETER ServiceAccountName
    Nombre de la cuenta de servicio local dedicada que se crea por defecto. Si ya
    existe, se reutiliza y se le asigna una contraseña nueva (la cuenta nunca inicia
    sesión interactiva, así que no hay nada que se rompa al rotarla). Se ignora si se
    pasa `-ExistingAccountUsername`.

.PARAMETER ExistingAccountUsername
    En vez de crear una cuenta de servicio dedicada, corre el servicio con una cuenta
    de usuario que ya existe (formato DOMINIO\usuario o EQUIPO\usuario). Pide la
    contraseña de forma interactiva y segura — no queda en el historial ni en ningún
    archivo.

.PARAMETER Port
    Puerto TCP de la interfaz web/API, para la regla de firewall entrante. Debe
    coincidir con `Web:Port` en la configuración del servicio (por defecto 8483,
    ver `WebSettings.cs`).

.EXAMPLE
    .\Install-JMBackupService.ps1 -InstallPath "C:\Program Files\JMBackup"

.EXAMPLE
    .\Install-JMBackupService.ps1 -ExistingAccountUsername "GESTIONADORA\ajimenez"
#>
[CmdletBinding()]
param(
    [string]$InstallPath = (Join-Path $PSScriptRoot "..\publish"),
    [string]$ServiceAccountName = "JMBackupSvc",
    [string]$ExistingAccountUsername,
    [int]$Port = 8483
)

$ErrorActionPreference = "Stop"
$ServiceName = "JMBackup"

$currentPrincipal = New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())
if (-not $currentPrincipal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    throw "Este script debe ejecutarse en una consola de PowerShell elevada (Administrador)."
}

$exePath = Join-Path $InstallPath "JMBackup.Api.exe"
if (-not (Test-Path $exePath)) {
    throw "No se encontró '$exePath'. Publicá la aplicación antes de instalar el servicio:`n" +
        "  dotnet publish src/JMBackup.Api -c Release -r win-x64 --self-contained " +
        "-p:PublishSingleFile=true -o publish"
}

function New-RandomServicePassword {
    # La cuenta de servicio nunca inicia sesión interactiva: esta contraseña solo
    # existe en memoria durante esta ejecución, para pasarla a New-Service
    # -Credential. No se muestra ni se guarda en ningún lado.
    $lower = 'abcdefghijklmnopqrstuvwxyz'
    $upper = $lower.ToUpper()
    $digits = '0123456789'
    $special = '!@#$%^&*-_=+'
    $all = $lower + $upper + $digits + $special

    # Se usa Create()+GetBytes() en vez de Fill(): Fill() es estático y solo existe
    # desde .NET moderno — Windows PowerShell 5.1 corre sobre .NET Framework, que no
    # lo tiene. GetBytes(byte[]) existe en ambos.
    $bytes = [byte[]]::new(32)
    $rng = [Security.Cryptography.RandomNumberGenerator]::Create()
    try { $rng.GetBytes($bytes) } finally { $rng.Dispose() }
    $chars = for ($i = 0; $i -lt $bytes.Length; $i++) { $all[$bytes[$i] % $all.Length] }

    # Garantiza al menos un carácter de cada categoría, para cumplir cualquier
    # política de complejidad local sin depender de la distribución aleatoria.
    $chars[0] = $lower[(Get-Random -Maximum $lower.Length)]
    $chars[1] = $upper[(Get-Random -Maximum $upper.Length)]
    $chars[2] = $digits[(Get-Random -Maximum $digits.Length)]
    $chars[3] = $special[(Get-Random -Maximum $special.Length)]

    -join $chars
}

# 1. Cuenta que va a correr el servicio: la dedicada de siempre, o una que ya existe
#    si se pasó -ExistingAccountUsername.
if ($ExistingAccountUsername) {
    $accountName = $ExistingAccountUsername
    $credential = Get-Credential -UserName $accountName -Message "Contraseña de $accountName para el servicio $ServiceName"
    $sid = (New-Object Security.Principal.NTAccount($accountName)).Translate([Security.Principal.SecurityIdentifier]).Value
}
else {
    $accountName = ".\$ServiceAccountName"
    $securePassword = ConvertTo-SecureString -String (New-RandomServicePassword) -AsPlainText -Force
    $existingAccount = Get-LocalUser -Name $ServiceAccountName -ErrorAction SilentlyContinue
    if ($existingAccount) {
        Set-LocalUser -Name $ServiceAccountName -Password $securePassword
        Write-Host "Cuenta de servicio '$ServiceAccountName' ya existía: contraseña rotada."
    }
    else {
        New-LocalUser -Name $ServiceAccountName -Password $securePassword `
            -PasswordNeverExpires -UserMayNotChangePassword -AccountNeverExpires `
            -Description "Cuenta de servicio dedicada de JMBackup." | Out-Null
        Write-Host "Cuenta de servicio '$ServiceAccountName' creada."
    }

    $credential = New-Object Management.Automation.PSCredential($accountName, $securePassword)
    $sid = (Get-LocalUser -Name $ServiceAccountName).SID.Value
}

# 2. Derecho "Iniciar sesión como servicio" (SeServiceLogonRight), vía secedit:
#    no hay cmdlet nativo de PowerShell para editar derechos de usuario locales.
$seceditCfg = Join-Path $env:TEMP "jmbackup-secedit-$([Guid]::NewGuid()).inf"
$seceditDb = Join-Path $env:TEMP "jmbackup-secedit-$([Guid]::NewGuid()).sdb"
try {
    secedit /export /cfg $seceditCfg /areas USER_RIGHTS | Out-Null
    $lines = Get-Content $seceditCfg
    $rightLine = $lines | Where-Object { $_ -match '^SeServiceLogonRight\s*=' }

    if ($rightLine -and $rightLine -match [regex]::Escape("*$sid")) {
        Write-Host "'$accountName' ya tenía el derecho 'Iniciar sesión como servicio'."
    }
    else {
        if ($rightLine) {
            $lines = $lines -replace [regex]::Escape($rightLine), "$rightLine,*$sid"
        }
        else {
            $lines += "SeServiceLogonRight = *$sid"
        }
        $lines | Set-Content $seceditCfg
        secedit /configure /db $seceditDb /cfg $seceditCfg /areas USER_RIGHTS | Out-Null
        Write-Host "Derecho 'Iniciar sesión como servicio' otorgado a '$accountName'."
    }
}
finally {
    Remove-Item $seceditCfg, $seceditDb -ErrorAction SilentlyContinue
}

# 3. Registrar el servicio (reemplaza uno preexistente del mismo nombre)
$existingService = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
if ($existingService) {
    Write-Host "El servicio '$ServiceName' ya existe: se detiene y se vuelve a registrar."
    Stop-Service -Name $ServiceName -Force -ErrorAction SilentlyContinue
    & sc.exe delete $ServiceName | Out-Null
    Start-Sleep -Seconds 1
}

New-Service -Name $ServiceName `
    -BinaryPathName "`"$exePath`"" `
    -DisplayName "JMBackup" `
    -Description "Servicio de respaldo JMBackup (motor de copia, API HTTPS y planificación)." `
    -StartupType Automatic `
    -Credential $credential | Out-Null
Write-Host "Servicio '$ServiceName' registrado, apuntando a '$exePath'."

# 4. Regla de firewall entrante. Se limita a Dominio/Privada: exponer el respaldo a
#    una red Pública queda fuera del comportamiento "seguro por defecto" del
#    proyecto (ver docs/CLAUDE.md §6); el operador puede ampliarla si lo necesita.
$existingRule = Get-NetFirewallRule -DisplayName $ServiceName -ErrorAction SilentlyContinue
if ($existingRule) {
    Remove-NetFirewallRule -DisplayName $ServiceName
}
New-NetFirewallRule -DisplayName $ServiceName `
    -Direction Inbound -Action Allow -Protocol TCP -LocalPort $Port `
    -Profile Domain, Private | Out-Null
Write-Host "Regla de firewall creada para el puerto TCP $Port (perfiles Dominio/Privada)."

# 5. Rutas largas de NTFS (más de 260 caracteres), necesarias para el escaneo de
#    árboles de archivos profundos (ver app.manifest / longPathAware, fase 1).
$longPathsKey = "HKLM:\SYSTEM\CurrentControlSet\Control\FileSystem"
Set-ItemProperty -Path $longPathsKey -Name "LongPathsEnabled" -Value 1 -Type DWord
Write-Host "LongPathsEnabled activado."

# 6. Arrancar el servicio
Start-Service -Name $ServiceName
Write-Host "Servicio '$ServiceName' iniciado. Estado: $((Get-Service -Name $ServiceName).Status)"
