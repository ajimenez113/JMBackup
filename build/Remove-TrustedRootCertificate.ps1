<#
.SYNOPSIS
    Quita del almacén Trusted Root el certificado de JMBackup instalado por
    `Install-TrustedRootCertificate.ps1`, si lo hay.

.DESCRIPTION
    Se usa en la desinstalación. Lee la huella digital guardada por
    `Install-TrustedRootCertificate.ps1` y borra únicamente ese certificado del
    almacén — nunca borra por nombre ni por emisor, para no arriesgarse a tocar otro
    certificado que alguien haya agregado después.

    Si el archivo marcador no existe (porque nunca se instaló el certificado, o
    porque el instalador se ejecutó antes de que este script existiera), no hace
    nada: no es un error, es el estado esperado en una instalación que nunca confió
    en el certificado.

    Requiere ejecutarse elevado (Administrador).

.PARAMETER MarkerPath
    Ruta al archivo con la huella digital guardada. Por defecto,
    `%ProgramData%\JMBackup\trusted-root-thumbprint.txt`.

.EXAMPLE
    .\Remove-TrustedRootCertificate.ps1
#>
[CmdletBinding()]
param(
    [string]$MarkerPath = (Join-Path $env:ProgramData "JMBackup\trusted-root-thumbprint.txt")
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path $MarkerPath)) {
    Write-Host "Sin certificado de JMBackup instalado en Trusted Root (nada que quitar)."
    return
}

$currentPrincipal = New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())
if (-not $currentPrincipal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    throw "Este script debe ejecutarse en una consola de PowerShell elevada (Administrador)."
}

$thumbprint = (Get-Content $MarkerPath -Raw).Trim()
$certPath = "Cert:\LocalMachine\Root\$thumbprint"

if (Test-Path $certPath) {
    Remove-Item $certPath -Force
    Write-Host "Certificado de JMBackup ($thumbprint) quitado de Entidades de certificación raíz de confianza."
}
else {
    Write-Host "El certificado de JMBackup ($thumbprint) ya no estaba en el almacén."
}

Remove-Item $MarkerPath -ErrorAction SilentlyContinue
