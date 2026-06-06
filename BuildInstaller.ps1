param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64"
)

$ErrorActionPreference = "Stop"

$projectRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectFile = Join-Path $projectRoot "ReiseArbeitszeitApp.csproj"
$releaseScript = Join-Path $projectRoot "BuildRelease.ps1"
$installerScript = Join-Path $projectRoot "Installer.iss"

[xml]$project = Get-Content -Path $projectFile
$version = $project.Project.PropertyGroup.Version
if ([string]::IsNullOrWhiteSpace($version)) {
    throw "Keine Version in ReiseArbeitszeitApp.csproj gefunden."
}

& $releaseScript -Configuration $Configuration -Runtime $Runtime

$isccCandidates = @(
    (Join-Path ${env:ProgramFiles(x86)} "Inno Setup 6\ISCC.exe"),
    (Join-Path $env:ProgramFiles "Inno Setup 6\ISCC.exe"),
    (Join-Path $env:LOCALAPPDATA "Programs\Inno Setup 6\ISCC.exe")
)

$iscc = $isccCandidates | Where-Object { $_ -and (Test-Path $_) } | Select-Object -First 1
if (-not $iscc) {
    throw "Inno Setup 6 wurde nicht gefunden. Bitte Inno Setup installieren und das Skript erneut starten."
}

$publishDir = Join-Path $projectRoot "publish\$Runtime-v$version"
$installerOutputDir = Join-Path $projectRoot "publish\installer"
New-Item -ItemType Directory -Path $installerOutputDir -Force | Out-Null

& $iscc `
    "/DMyAppVersion=$version" `
    "/DPublishDir=$publishDir" `
    "/DInstallerOutputDir=$installerOutputDir" `
    $installerScript

if ($LASTEXITCODE -ne 0) {
    throw "Der Installer konnte nicht erstellt werden. ISCC-Exitcode: $LASTEXITCODE"
}

$setupPath = Join-Path $installerOutputDir "ReiseArbeitszeitApp-Setup-$version.exe"
Write-Host "Installer erstellt:"
Write-Host $setupPath
