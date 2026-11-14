param(
    [ValidateSet('CashFlow', 'Investment', 'Both',
        'CashFlow.Domain', 'Investment.Domain',
        'CashFlow.Application', 'Investment.Application',
        'CashFlow.Infrastructure', 'Investment.Infrastructure',
        'Shared.Abstractions', 'Shared.Infrastructure', 'Api')]
    [string]$Target = 'Both',
    [string]$Mutate = '',
    [string]$Tests = '',
    [switch]$DisableBail,
    [switch]$NoOpen
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot

$aliases = @{
    CashFlow   = @('CashFlow.Domain')
    Investment = @('Investment.Domain')
    Both       = @('CashFlow.Domain', 'Investment.Domain')
}
$selected = if ($aliases.ContainsKey($Target)) { $aliases[$Target] } else { @($Target) }

Push-Location $root
try {
    dotnet tool restore | Out-Null
    if ($LASTEXITCODE -ne 0) { throw 'dotnet tool restore failed' }
} finally {
    Pop-Location
}

foreach ($name in $selected) {
    $testsDir = Join-Path $root "Tests/Financial.$name.Tests"
    $outputDir = Join-Path $root "TestResults/stryker/$name/$(Get-Date -Format 'yyyyMMdd-HHmmss')"
    $arguments = @('stryker', '--config-file', '../../.config/stryker-config.json', '--project', "Financial.$name.csproj", '--output', $outputDir, '--break-at', '0')
    if ($Mutate) { $arguments += @('--mutate', $Mutate) }
    if ($DisableBail) { $arguments += '--disable-bail' }

    Write-Host "== Stryker.NET: $name"
    Push-Location $testsDir
    try {
        & dotnet @arguments
        if ($LASTEXITCODE -ne 0) { throw "Stryker failed for $name (exit $LASTEXITCODE)" }
    } finally {
        Pop-Location
    }

    $json = Join-Path $outputDir 'reports/mutation-report.json'
    $html = Join-Path $outputDir 'reports/mutation-report.html'
    & pwsh -NoProfile -File (Join-Path $root '.github/scripts/mutation-score.ps1') -Report $json -Name $name -SummaryPath '' -OutputPath ''
    & pwsh -NoProfile -File (Join-Path $PSScriptRoot 'mutation-test-value.ps1') -Report $json -Tests $Tests
    Write-Host "Report: $html"
    if (-not $NoOpen) { Start-Process $html }
}
