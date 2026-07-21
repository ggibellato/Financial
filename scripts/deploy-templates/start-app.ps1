# Launches the Financial WPF desktop app from this deploy folder.
$ErrorActionPreference = 'Stop'

$appDir = Join-Path $PSScriptRoot 'Financial.App'
$exePath = Join-Path $appDir 'Financial.Presentation.App.exe'

if (-not (Test-Path $exePath)) {
    Write-Error "Financial.Presentation.App.exe not found at '$exePath'. Run scripts/deploy.ps1 first."
    exit 1
}

Start-Process -FilePath $exePath -WorkingDirectory $appDir
Write-Host "Financial desktop app started."
