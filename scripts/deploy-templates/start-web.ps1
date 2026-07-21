# Launches the Financial web server (API + SPA) from this deploy folder.
$ErrorActionPreference = 'Stop'

$webDir = Join-Path $PSScriptRoot 'Financial.Web'
$exePath = Join-Path $webDir 'Financial.Api.exe'

if (-not (Test-Path $exePath)) {
    Write-Error "Financial.Api.exe not found at '$exePath'. Run scripts/deploy.ps1 first."
    exit 1
}

$env:ASPNETCORE_URLS = 'http://localhost:8080'
Start-Process -FilePath $exePath -WorkingDirectory $webDir
Write-Host "Financial web app starting at http://localhost:8080"
