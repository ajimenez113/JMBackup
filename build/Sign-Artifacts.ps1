<#
.SYNOPSIS
    Firma con Authenticode los ejecutables publicados y el instalador, si hay un
    certificado de firma de código configurado (RNF-06).

.DESCRIPTION
    Sin certificado, este script no hace nada más que avisarlo por consola: no es un
    error, es el estado esperado mientras el proyecto no tenga uno propio (ver
    ADR-028 y la guía de instalación, que documenta la advertencia de SmartScreen que
    ve quien instale un paquete sin firmar).

    Con certificado, firma cada archivo que se le pase con SignTool
    (`signtool sign /fd sha256 /tr <servidor de sellado> /td sha256`), usando sellado
    de tiempo para que la firma siga siendo válida después de que el certificado
    venza.

    El certificado se puede indicar de dos formas (una sola, no las dos):
    - `-CertificateThumbprint`: un certificado ya importado en el almacén de
      certificados del usuario o del equipo (`Cert:\CurrentUser\My` o
      `Cert:\LocalMachine\My`) — la forma recomendada para no tener el .pfx ni su
      contraseña sueltos en el disco de la máquina que compila.
    - `-CertificatePath` + `-CertificatePassword`: un archivo .pfx directo.

    SignTool es parte del Windows SDK, no un paquete NuGet — igual que ISCC.exe para
    Inno Setup, es una herramienta de compilación externa.

.PARAMETER FilesToSign
    Rutas de los archivos a firmar (ejecutables, el instalador compilado).

.PARAMETER CertificateThumbprint
    Huella digital del certificado ya instalado en un almacén de certificados.

.PARAMETER CertificatePath
    Ruta a un archivo .pfx, alternativa a `-CertificateThumbprint`.

.PARAMETER CertificatePassword
    Contraseña del .pfx indicado en `-CertificatePath`, como `SecureString`.

.PARAMETER TimestampServer
    Servidor de sellado de tiempo RFC 3161. Por defecto, el de DigiCert.

.EXAMPLE
    .\Sign-Artifacts.ps1 -FilesToSign "publish\JMBackup.Api.exe" -CertificateThumbprint "ABCD..."

.EXAMPLE
    .\Sign-Artifacts.ps1 -FilesToSign "publish\JMBackup.Api.exe", "publish\desktop\JMBackup.Desktop.exe"
    # Sin certificado configurado: no firma nada, solo lo informa.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string[]]$FilesToSign,

    [string]$CertificateThumbprint,
    [string]$CertificatePath,
    [Security.SecureString]$CertificatePassword,
    [string]$TimestampServer = "http://timestamp.digicert.com"
)

$ErrorActionPreference = "Stop"

function Find-SignTool {
    $onPath = Get-Command "signtool.exe" -ErrorAction SilentlyContinue
    if ($onPath) {
        return $onPath.Source
    }

    # El Windows SDK instala varias versiones lado a lado, cada una en su propia
    # carpeta (ej. 10.0.22621.0); se toma la más nueva que exista.
    $sdkRoot = "${env:ProgramFiles(x86)}\Windows Kits\10\bin"
    if (Test-Path $sdkRoot) {
        $candidate = Get-ChildItem $sdkRoot -Directory |
            Sort-Object Name -Descending |
            ForEach-Object { Join-Path $_.FullName "x64\signtool.exe" } |
            Where-Object { Test-Path $_ } |
            Select-Object -First 1
        if ($candidate) {
            return $candidate
        }
    }

    return $null
}

if (-not $CertificateThumbprint -and -not $CertificatePath) {
    Write-Host "Sin certificado de firma de código configurado: los artefactos quedan sin firmar (RNF-06)."
    Write-Host "La guía de instalación documenta la advertencia de SmartScreen que esto produce."
    return
}
if ($CertificateThumbprint -and $CertificatePath) {
    throw "Pasá -CertificateThumbprint o -CertificatePath, no los dos a la vez."
}

$signTool = Find-SignTool
if (-not $signTool) {
    throw "No se encontró signtool.exe. Instalá el Windows SDK " +
        "(https://developer.microsoft.com/windows/downloads/windows-sdk/) o agregalo al PATH."
}

$signArgs = @("sign", "/fd", "sha256", "/tr", $TimestampServer, "/td", "sha256")
if ($CertificateThumbprint) {
    $signArgs += @("/sha1", $CertificateThumbprint)
}
else {
    if (-not (Test-Path $CertificatePath)) {
        throw "No se encontró el certificado '$CertificatePath'."
    }
    if (-not $CertificatePassword) {
        throw "Falta -CertificatePassword para el archivo .pfx indicado."
    }
    $plainPassword = [Runtime.InteropServices.Marshal]::PtrToStringUni(
        [Runtime.InteropServices.Marshal]::SecureStringToGlobalAllocUnicode($CertificatePassword))
    $signArgs += @("/f", $CertificatePath, "/p", $plainPassword)
}

foreach ($file in $FilesToSign) {
    if (-not (Test-Path $file)) {
        throw "No se encontró '$file' para firmar."
    }

    Write-Host "Firmando '$file'..."
    & $signTool @signArgs $file
    if ($LASTEXITCODE -ne 0) {
        throw "signtool falló firmando '$file' (código $LASTEXITCODE)."
    }
}

Write-Host "Firma de código completa para $($FilesToSign.Count) archivo(s)."
