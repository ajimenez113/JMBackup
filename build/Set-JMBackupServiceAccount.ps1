<#
.SYNOPSIS
    Cambia la cuenta con la que corre el servicio JMBackup a una cuenta de usuario ya
    existente, en vez de la cuenta dedicada que crea Install-JMBackupService.ps1.

.DESCRIPTION
    Útil cuando los respaldos son sobre todo hacia servidores de red: con la cuenta de
    usuario propia, el servicio ya tiene la identidad de red que usás normalmente al
    iniciar sesión, sin necesitar guardar una credencial separada por cada tarea.

    La contraseña se pide de forma interactiva y segura (no queda en el historial de
    PowerShell ni en ningún archivo). Requiere ejecutarse elevado (Administrador).

.PARAMETER Username
    Cuenta que va a correr el servicio, en formato DOMINIO\usuario o EQUIPO\usuario.

.EXAMPLE
    .\Set-JMBackupServiceAccount.ps1 -Username "GESTIONADORA\ajimenez"
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$Username
)

$ErrorActionPreference = "Stop"
$ServiceName = "JMBackup"

$currentPrincipal = New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())
if (-not $currentPrincipal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    throw "Este script debe ejecutarse en una consola de PowerShell elevada (Administrador)."
}

if (-not (Get-Service -Name $ServiceName -ErrorAction SilentlyContinue)) {
    throw "El servicio '$ServiceName' no está instalado. Instalalo primero con Install-JMBackupService.ps1."
}

$credential = Get-Credential -UserName $Username -Message "Contraseña de $Username para el servicio $ServiceName"

# 1. Derecho "Iniciar sesión como servicio" (SeServiceLogonRight), vía secedit — igual
#    que Install-JMBackupService.ps1, porque no hay cmdlet nativo para editar derechos
#    de usuario locales.
$sid = (New-Object Security.Principal.NTAccount($Username)).Translate([Security.Principal.SecurityIdentifier]).Value
$seceditCfg = Join-Path $env:TEMP "jmbackup-secedit-$([Guid]::NewGuid()).inf"
$seceditDb = Join-Path $env:TEMP "jmbackup-secedit-$([Guid]::NewGuid()).sdb"
try {
    secedit /export /cfg $seceditCfg /areas USER_RIGHTS | Out-Null
    $lines = Get-Content $seceditCfg
    $rightLine = $lines | Where-Object { $_ -match '^SeServiceLogonRight\s*=' }

    if ($rightLine -and $rightLine -match [regex]::Escape("*$sid")) {
        Write-Host "'$Username' ya tenía el derecho 'Iniciar sesión como servicio'."
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
        Write-Host "Derecho 'Iniciar sesión como servicio' otorgado a '$Username'."
    }
}
finally {
    Remove-Item $seceditCfg, $seceditDb -ErrorAction SilentlyContinue
}

# 2. Cambiar la cuenta del servicio ya registrado. sc.exe exige exactamente un
#    espacio entre "clave=" y el valor, y ninguno entre la clave y el "=".
Stop-Service -Name $ServiceName -Force -ErrorAction SilentlyContinue

$plainPassword = $credential.GetNetworkCredential().Password
& sc.exe config $ServiceName obj= $Username password= $plainPassword | Out-Null

# 3. Arrancar de nuevo con la nueva identidad
Start-Service -Name $ServiceName
Write-Host "Servicio '$ServiceName' ahora corre como '$Username'. Estado: $((Get-Service -Name $ServiceName).Status)"
