<#
.SYNOPSIS
    Instala la parte pública del certificado autofirmado de JMBackup en el almacén de
    entidades de certificación raíz de confianza del equipo (Trusted Root).

.DESCRIPTION
    JMBackup genera un certificado autofirmado para su propia interfaz HTTPS
    (`SelfSignedCertificateProvider`, en el primer arranque del servicio). Al no estar
    firmado por una entidad reconocida, el navegador y el WebView2 de la app de
    escritorio muestran una advertencia de conexión no segura la primera vez.

    Este script NO decide si conviene confiar en él — esa decisión, con su
    explicación en español, la muestra el instalador de Inno Setup antes de invocar
    este script. Acá solo se ejecuta la acción una vez que la persona ya aceptó.

    Confiar en este certificado significa que Windows va a aceptar como válida
    cualquier conexión HTTPS firmada con él. Como la clave privada nunca sale de este
    equipo (el .pfx se genera acá y no se comparte), el único que puede firmar algo
    con ella es este mismo JMBackup — pero es una decisión de confianza real, no un
    trámite cosmético, por eso el instalador la explica y la deja opcional.

    Requiere ejecutarse elevado (Administrador). Guarda la huella digital del
    certificado instalado en un archivo marcador junto al propio certificado, para que
    `Remove-TrustedRootCertificate.ps1` pueda quitar exactamente ese certificado en la
    desinstalación, sin tocar ningún otro certificado del almacén.

.PARAMETER CertificatePath
    Ruta al `jmbackup.pfx` generado por el servicio. Por defecto,
    `%ProgramData%\JMBackup\jmbackup.pfx`.

.PARAMETER TimeoutSeconds
    Cuánto esperar a que el archivo del certificado exista, por si este script corre
    justo después de arrancar el servicio por primera vez y la generación todavía no
    terminó.

.EXAMPLE
    .\Install-TrustedRootCertificate.ps1
#>
[CmdletBinding()]
param(
    [string]$CertificatePath = (Join-Path $env:ProgramData "JMBackup\jmbackup.pfx"),
    [int]$TimeoutSeconds = 15
)

$ErrorActionPreference = "Stop"

$currentPrincipal = New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())
if (-not $currentPrincipal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    throw "Este script debe ejecutarse en una consola de PowerShell elevada (Administrador)."
}

$deadline = (Get-Date).AddSeconds($TimeoutSeconds)
while (-not (Test-Path $CertificatePath) -and (Get-Date) -lt $deadline) {
    Start-Sleep -Milliseconds 500
}
if (-not (Test-Path $CertificatePath)) {
    throw "No se encontró '$CertificatePath' tras esperar $TimeoutSeconds segundos. " +
        "El servicio JMBackup tiene que haber arrancado al menos una vez para generar su certificado."
}

# Solo la parte pública: el .pfx tiene la clave privada, pero el almacén Root no la
# necesita (ni debe tenerla) — de ahí exportar a Cert (público) antes de importar.
$certificate = [Security.Cryptography.X509Certificates.X509CertificateLoader]::LoadPkcs12FromFile(
    $CertificatePath, $null, [Security.Cryptography.X509Certificates.X509KeyStorageFlags]::Exportable)

$tempCerPath = Join-Path $env:TEMP "jmbackup-$([Guid]::NewGuid()).cer"
try {
    $publicBytes = $certificate.Export([Security.Cryptography.X509Certificates.X509ContentType]::Cert)
    [IO.File]::WriteAllBytes($tempCerPath, $publicBytes)

    Import-Certificate -FilePath $tempCerPath -CertStoreLocation Cert:\LocalMachine\Root | Out-Null
    Write-Host "Certificado de JMBackup ($($certificate.Thumbprint)) instalado en Entidades de certificación raíz de confianza."

    $markerPath = Join-Path (Split-Path $CertificatePath -Parent) "trusted-root-thumbprint.txt"
    Set-Content -Path $markerPath -Value $certificate.Thumbprint -NoNewline
}
finally {
    Remove-Item $tempCerPath -ErrorAction SilentlyContinue
    $certificate.Dispose()
}
