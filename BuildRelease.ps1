param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64"
)

$ErrorActionPreference = "Stop"

$projectRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectFile = Join-Path $projectRoot "ReiseArbeitszeitApp.csproj"

[xml]$project = Get-Content -Path $projectFile
$version = $project.Project.PropertyGroup.Version
if ([string]::IsNullOrWhiteSpace($version)) {
    throw "Keine Version in ReiseArbeitszeitApp.csproj gefunden."
}

$publishRoot = Join-Path $projectRoot "publish"
$outputFolder = Join-Path $publishRoot "$Runtime-v$version"
$zipPath = Join-Path $publishRoot "ReiseArbeitszeitApp-$version-$Runtime.zip"

dotnet publish $projectFile `
    -c $Configuration `
    -r $Runtime `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:PublishReadyToRun=false `
    -o $outputFolder

Get-ChildItem -Path $outputFolder -Filter "*.pdb" -ErrorAction SilentlyContinue | Remove-Item

if (Test-Path $zipPath) {
    Remove-Item $zipPath
}

Compress-Archive -Path (Join-Path $outputFolder "*") -DestinationPath $zipPath

Write-Host "Release erstellt:"
Write-Host "Ordner: $outputFolder"
Write-Host "ZIP:    $zipPath"
