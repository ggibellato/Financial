<#
.SYNOPSIS
    Publishes the current local state of Financial.App (WPF) and Financial.Web (API + SPA)
    to the local `deploy/` folder. Manual, local-only tooling - not part of CI/CD.

.DESCRIPTION
    Re-run any time to refresh the deployed copies with whatever is currently on disk.
    Log level (Debug) and the Google Drive data source are fixed via the checked-in
    appsettings.Production.json files, so nothing needs to be reconfigured between runs.
    GoogleDrive:CredentialsPath is machine-specific and NOT committed to source control -
    it's read from scripts/deploy.local.json (gitignored, created from
    scripts/deploy.local.example.json on first run) and stamped into the deployed
    appsettings.Production.json files after each publish.
#>
[CmdletBinding()]
param(
    [string]$Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$deployRoot = Join-Path $repoRoot 'deploy'
$appDeployDir = Join-Path $deployRoot 'Financial.App'
$webDeployDir = Join-Path $deployRoot 'Financial.Web'
$templatesDir = Join-Path $PSScriptRoot 'deploy-templates'
$localSettingsPath = Join-Path $PSScriptRoot 'deploy.local.json'

Write-Host "== Financial local deploy ==" -ForegroundColor Cyan
Write-Host "Repo root:   $repoRoot"
Write-Host "Deploy root: $deployRoot"

# 1. Load (or seed) the machine-local settings that are never committed to source control.
if (-not (Test-Path $localSettingsPath)) {
    Copy-Item -Path (Join-Path $PSScriptRoot 'deploy.local.example.json') -Destination $localSettingsPath
    Write-Warning "Created $localSettingsPath - edit GoogleDriveCredentialsPath before starting the apps."
}
$localSettings = Get-Content $localSettingsPath -Raw | ConvertFrom-Json

# 2. Stop any running deployed instances so publish doesn't hit locked files.
Write-Host "`n-- Stopping any running deployed instances --"
Get-Process -Name 'Financial.Presentation.App', 'Financial.Api' -ErrorAction SilentlyContinue |
    Where-Object { $_.Path -and $_.Path.StartsWith($deployRoot, [System.StringComparison]::OrdinalIgnoreCase) } |
    ForEach-Object {
        Write-Host "Stopping $($_.ProcessName) (PID $($_.Id))"
        Stop-Process -Id $_.Id -Force
    }

# 3. Clean + recreate the two app output folders.
foreach ($dir in @($appDeployDir, $webDeployDir)) {
    if (Test-Path $dir) {
        Remove-Item -Path $dir -Recurse -Force
    }
    New-Item -ItemType Directory -Path $dir -Force | Out-Null
}

# 4. Publish the WPF desktop app (framework-dependent - build machine == run machine).
Write-Host "`n-- Publishing Financial.App (WPF) --"
dotnet publish (Join-Path $repoRoot 'Financial.App\Financial.App.csproj') -c $Configuration -o $appDeployDir
if ($LASTEXITCODE -ne 0) { throw 'Publish of Financial.App failed.' }

# 5. Publish the API host.
Write-Host "`n-- Publishing Financial.Api --"
dotnet publish (Join-Path $repoRoot 'Financial.Api\Financial.Api.csproj') -c $Configuration -o $webDeployDir
if ($LASTEXITCODE -ne 0) { throw 'Publish of Financial.Api failed.' }

# 6. Deploy appsettings.Production.json ourselves - it's excluded from `dotnet publish`
#    output (CopyToPublishDirectory=Never, same treatment as appsettings.Development.json)
#    so this script is the only thing that puts it in the deploy folder, then stamps in
#    the machine-local Google Drive credentials path.
foreach ($pair in @(
        @{ Source = (Join-Path $repoRoot 'Financial.App\appsettings.Production.json'); Target = (Join-Path $appDeployDir 'appsettings.Production.json') },
        @{ Source = (Join-Path $repoRoot 'Financial.Api\appsettings.Production.json'); Target = (Join-Path $webDeployDir 'appsettings.Production.json') }
    )) {
    $settings = Get-Content $pair.Source -Raw | ConvertFrom-Json
    $settings.GoogleDrive.CredentialsPath = $localSettings.GoogleDriveCredentialsPath
    $settings | ConvertTo-Json -Depth 10 | Set-Content -Path $pair.Target -Encoding utf8
}

# 7. Build the React SPA (same API_BASE_URL convention as the Docker image) and drop it into wwwroot.
Write-Host "`n-- Building Financial.Web (SPA) --"
$webSrcDir = Join-Path $repoRoot 'Financial.Web'
Push-Location $webSrcDir
try {
    'API_BASE_URL=/api/v1/financial' | Set-Content -Path (Join-Path $webSrcDir '.env') -Encoding utf8
    npm install
    if ($LASTEXITCODE -ne 0) { throw 'npm install failed.' }
    npm run build
    if ($LASTEXITCODE -ne 0) { throw 'npm run build failed.' }
}
finally {
    Pop-Location
}

$wwwrootDir = Join-Path $webDeployDir 'wwwroot'
New-Item -ItemType Directory -Path $wwwrootDir -Force | Out-Null
Copy-Item -Path (Join-Path $webSrcDir 'dist\*') -Destination $wwwrootDir -Recurse -Force

# 8. Refresh the launcher scripts.
Copy-Item -Path (Join-Path $templatesDir '*.ps1') -Destination $deployRoot -Force

# 9. Record what was deployed.
Push-Location $repoRoot
try {
    $branch = (git branch --show-current 2>$null)
    $commit = (git rev-parse --short HEAD 2>$null)
}
finally {
    Pop-Location
}
@"
Deployed: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')
Branch:   $branch
Commit:   $commit
"@ | Set-Content -Path (Join-Path $deployRoot 'deploy-info.txt') -Encoding utf8

# 10. Summary.
Write-Host "`n== Deploy complete ==" -ForegroundColor Green
Write-Host "Financial.App -> $appDeployDir"
Write-Host "Financial.Web -> $webDeployDir"

$credentialsPath = $localSettings.GoogleDriveCredentialsPath
if ([string]::IsNullOrWhiteSpace($credentialsPath) -or -not (Test-Path $credentialsPath)) {
    Write-Warning "Google Drive credentials not found at '$credentialsPath'. Edit GoogleDriveCredentialsPath in $localSettingsPath, then re-run this script."
}

Write-Host "Start everything with: $deployRoot\start-all.ps1"
