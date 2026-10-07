param(
    [ValidateSet('CashFlow', 'Investment', 'Both')][string]$Target = 'Both',
    [string]$Mutate = '',
    [switch]$NoOpen
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot

$targets = @{
    CashFlow   = @{ Tests = 'Tests/Financial.CashFlow.Domain.Tests';   Project = 'Financial.CashFlow.Domain.csproj' }
    Investment = @{ Tests = 'Tests/Financial.Investment.Domain.Tests'; Project = 'Financial.Investment.Domain.csproj' }
}

$selected = if ($Target -eq 'Both') { @('CashFlow', 'Investment') } else { @($Target) }

Push-Location $root
try {
    dotnet tool restore | Out-Null
    if ($LASTEXITCODE -ne 0) { throw 'dotnet tool restore failed' }
} finally {
    Pop-Location
}

foreach ($name in $selected) {
    $testsDir = Join-Path $root $targets[$name].Tests
    $arguments = @('stryker', '--config-file', '../../.config/stryker-config.json', '--project', $targets[$name].Project)
    if ($Mutate) { $arguments += @('--mutate', $Mutate) }

    Write-Host "== Stryker.NET: $name.Domain"
    Push-Location $testsDir
    try {
        $started = Get-Date
        & dotnet @arguments
        if ($LASTEXITCODE -ne 0) { throw "Stryker failed for $name.Domain (exit $LASTEXITCODE)" }

        $reports = Get-ChildItem -Path (Join-Path $testsDir 'StrykerOutput') -Directory |
            Where-Object { $_.LastWriteTime -ge $started } |
            Sort-Object LastWriteTime -Descending
        $json = Join-Path $reports[0].FullName 'reports/mutation-report.json'
        $html = Join-Path $reports[0].FullName 'reports/mutation-report.html'
    } finally {
        Pop-Location
    }

    & pwsh -NoProfile -File (Join-Path $root '.github/scripts/mutation-score.ps1') -Report $json -Name "$name.Domain" -SummaryPath '' -OutputPath ''
    Write-Host "Report: $html"
    if (-not $NoOpen) { Start-Process $html }
}
