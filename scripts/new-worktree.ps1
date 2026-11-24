[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$Topic,
    [Parameter(Mandatory)][string]$Branch,
    [int]$ApiPort = 5191,
    [int]$WebPort = 5176,
    [switch]$SkipNpmInstall
)

$ErrorActionPreference = 'Stop'

$localFiles = @(
    '.env',
    'Financial.Web/.env',
    '.claude/settings.local.json',
    'data/data-investment.json',
    'data/data-fx-rates.json',
    'data/google-calendar-credentials.json',
    'data/dev'
)

$mainRoot = (git -C $PSScriptRoot worktree list --porcelain | Select-Object -First 1) -replace '^worktree ', ''
$worktree = Join-Path (Split-Path -Parent $mainRoot) "financial-wt-$Topic"

if (Test-Path $worktree) { throw "$worktree already exists." }

foreach ($port in $ApiPort, $WebPort) {
    if (Get-NetTCPConnection -LocalPort $port -State Listen -ErrorAction SilentlyContinue) {
        Write-Warning "Port $port is already in use; pass a different -ApiPort or -WebPort to run the app from this worktree."
    }
}

git -C $mainRoot fetch origin main
if ($LASTEXITCODE) { throw 'git fetch failed.' }
git -C $mainRoot worktree add --no-track $worktree -b $Branch origin/main
if ($LASTEXITCODE) { throw 'git worktree add failed.' }

foreach ($relativePath in $localFiles) {
    $source = Join-Path $mainRoot $relativePath
    if (-not (Test-Path $source)) {
        Write-Warning "Skipped $relativePath (not in $mainRoot)."
        continue
    }
    $destinationFolder = Split-Path -Parent (Join-Path $worktree $relativePath)
    New-Item -ItemType Directory -Force -Path $destinationFolder | Out-Null
    Copy-Item -Path $source -Destination $destinationFolder -Recurse -Force
    Write-Host "Copied $relativePath"
}

$webEnv = Join-Path $worktree 'Financial.Web/.env'
$webEnvLines = @(if (Test-Path $webEnv) { Get-Content $webEnv | Where-Object { $_ -notmatch '^\s*API_PROXY_TARGET=' } })
Set-Content -Path $webEnv -Value ($webEnvLines + "API_PROXY_TARGET=http://localhost:$ApiPort")

if (-not $SkipNpmInstall) {
    npm --prefix (Join-Path $worktree 'Financial.Web') ci --no-audit --no-fund
    if ($LASTEXITCODE) { throw 'npm ci failed.' }
}

Write-Host ''
Write-Host "Worktree ready: $worktree ($Branch)" -ForegroundColor Green
Write-Host 'Run from the worktree folder:'
Write-Host "  API: dotnet run --project Financial.Api --launch-profile http --urls http://localhost:$ApiPort"
Write-Host "  Web: npm --prefix Financial.Web run dev -- --port $WebPort --strictPort"
Write-Host "Then open http://localhost:$WebPort"
