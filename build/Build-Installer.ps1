<#
.SYNOPSIS
    Publica JMBackup y compila el instalador de Inno Setup.

.DESCRIPTION
    Envuelve los dos `dotnet publish` documentados en el README (servicio y
    escritorio, autocontenidos, archivo único), toma el número de versión de
    `Directory.Build.props` (la misma que ya usan todos los proyectos .NET, para no
    tener el número en dos lugares) y compila `JMBackup.iss` con ISCC pasándole esa
    versión.

    Requiere tener Inno Setup 6 instalado (https://jrsoftware.org/isinfo.php) — no es
    un paquete NuGet ni npm, así que no está en `Directory.Packages.props`; es una
    herramienta de compilación externa, igual que el propio .NET SDK.

.PARAMETER SkipPublish
    Omite los `dotnet publish` y usa lo que ya esté en `publish\`/`publish\desktop\`
    (más rápido si solo cambiaste el script del instalador).

.PARAMETER CertificateThumbprint
    Huella digital de un certificado de firma de código ya instalado en un almacén
    de certificados. Si se pasa (o `-CertificatePath`), firma los dos ejecutables
    publicados antes de compilar el instalador, y el instalador ya compilado después
    (RNF-06, ver `Sign-Artifacts.ps1`). Sin ninguno de los dos, el instalador sale sin
    firmar — sin error, ver `Sign-Artifacts.ps1`.

.PARAMETER CertificatePath
    Alternativa a `-CertificateThumbprint`: ruta a un archivo .pfx. Requiere
    `-CertificatePassword`.

.PARAMETER CertificatePassword
    Contraseña del .pfx indicado en `-CertificatePath`.

.EXAMPLE
    .\Build-Installer.ps1

.EXAMPLE
    .\Build-Installer.ps1 -CertificateThumbprint "ABCDEF0123456789..."
#>
[CmdletBinding()]
param(
    [switch]$SkipPublish,
    [string]$CertificateThumbprint,
    [string]$CertificatePath,
    [Security.SecureString]$CertificatePassword
)

$ErrorActionPreference = "Stop"
$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")

function Get-ProjectVersion {
    $propsPath = Join-Path $repoRoot "Directory.Build.props"
    [xml]$props = Get-Content $propsPath
    $version = $props.Project.PropertyGroup.Version | Where-Object { $_ } | Select-Object -First 1
    if (-not $version) {
        throw "No se encontró <Version> en '$propsPath'."
    }
    return $version.Trim()
}

function Find-Iscc {
    $onPath = Get-Command "iscc.exe" -ErrorAction SilentlyContinue
    if ($onPath) {
        return $onPath.Source
    }

    $wellKnownPaths = @(
        "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe",
        "$env:ProgramFiles\Inno Setup 6\ISCC.exe"
    )
    foreach ($candidate in $wellKnownPaths) {
        if (Test-Path $candidate) {
            return $candidate
        }
    }

    throw "No se encontró ISCC.exe (Inno Setup 6). Instalalo desde " +
        "https://jrsoftware.org/isinfo.php o agregalo al PATH."
}

function Invoke-Signing {
    param([string[]]$Files)

    $signScript = Join-Path $PSScriptRoot "Sign-Artifacts.ps1"
    $signArgs = @{ FilesToSign = $Files }
    if ($CertificateThumbprint) {
        $signArgs.CertificateThumbprint = $CertificateThumbprint
    }
    elseif ($CertificatePath) {
        $signArgs.CertificatePath = $CertificatePath
        $signArgs.CertificatePassword = $CertificatePassword
    }
    & $signScript @signArgs
}

if (-not $SkipPublish) {
    Write-Host "Publicando JMBackup.Api (servicio)..."
    dotnet publish (Join-Path $repoRoot "src\JMBackup.Api") -c Release -r win-x64 --self-contained `
        -p:PublishSingleFile=true -o (Join-Path $repoRoot "publish")
    if ($LASTEXITCODE -ne 0) { throw "Falló 'dotnet publish' de JMBackup.Api." }

    Write-Host "Publicando JMBackup.Desktop..."
    dotnet publish (Join-Path $repoRoot "src\JMBackup.Desktop") -c Release -r win-x64 --self-contained `
        -p:PublishSingleFile=true -o (Join-Path $repoRoot "publish\desktop")
    if ($LASTEXITCODE -ne 0) { throw "Falló 'dotnet publish' de JMBackup.Desktop." }
}

Invoke-Signing -Files @(
    (Join-Path $repoRoot "publish\JMBackup.Api.exe"),
    (Join-Path $repoRoot "publish\desktop\JMBackup.Desktop.exe")
)

$version = Get-ProjectVersion
$iscc = Find-Iscc
$issPath = Join-Path $PSScriptRoot "JMBackup.iss"

Write-Host "Compilando el instalador (versión $version)..."
& $iscc $issPath "/DMyAppVersion=$version"
if ($LASTEXITCODE -ne 0) { throw "Falló la compilación del instalador con ISCC." }

$installerPath = Join-Path $PSScriptRoot "dist\JMBackup-Setup-$version.exe"
Invoke-Signing -Files @($installerPath)

Write-Host "Instalador generado en '$(Join-Path $PSScriptRoot "dist")'."
