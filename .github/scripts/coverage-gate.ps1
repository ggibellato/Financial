param(
    [Parameter(Mandatory)][string]$Label,
    [Parameter(Mandatory)][string]$SummaryPath,
    [Parameter(Mandatory)][string]$BaselineKey,
    [string]$BaselinePath = 'coverage-baseline.json'
)

$ErrorActionPreference = 'Stop'
$lineFloor = 90
$tolerance = 0.5
$culture = [cultureinfo]::InvariantCulture

function Get-CoverageEmoji([double]$value) {
    if ($value -ge 90) { return '🟢' }
    elseif ($value -ge 85) { return '🟡' }
    elseif ($value -ge 80) { return '🟠' }
    else { return '🔴' }
}

function Add-StepSummary([string]$text) {
    if ($env:GITHUB_STEP_SUMMARY) { $text | Out-File -Append -Encoding utf8 $env:GITHUB_STEP_SUMMARY }
}

function Add-Output([string]$name, [string]$value) {
    if ($env:GITHUB_OUTPUT) { "$name=$value" | Out-File -Append -Encoding utf8 $env:GITHUB_OUTPUT }
}

function Format-Delta([double]$delta) {
    return $delta.ToString('+0.00;-0.00;+0.00', $culture)
}

$summary = Get-Content $SummaryPath -Raw | ConvertFrom-Json
$lineCoverage = $summary.summary.linecoverage
if ($null -eq $lineCoverage) {
    Write-Host "::error::Could not read summary.linecoverage from $SummaryPath"
    Add-StepSummary "🔴 Coverage gate: could not read line coverage from the report"
    exit 1
}

$line = [math]::Round([double]$lineCoverage, 2)
$emoji = Get-CoverageEmoji $line
$branch = $null
$branchEmoji = '⚪'
if ($null -ne $summary.summary.branchcoverage) {
    $branch = [math]::Round([double]$summary.summary.branchcoverage, 2)
    $branchEmoji = Get-CoverageEmoji $branch
}
$branchText = if ($null -ne $branch) { "$($branch.ToString($culture))%" } else { 'n/a' }

Add-StepSummary "$emoji $Label line coverage: $($line.ToString($culture))%, $branchEmoji branch coverage: $branchText"
Add-Output 'coverage' $line.ToString($culture)
Add-Output 'emoji' $emoji
Add-Output 'branch' $(if ($null -ne $branch) { $branch.ToString($culture) } else { '' })
Add-Output 'branch-emoji' $branchEmoji

$failures = @()

$baseline = $null
try {
    $document = Get-Content $BaselinePath -Raw -ErrorAction Stop | ConvertFrom-Json -ErrorAction Stop
    $baseline = $document.$BaselineKey
    if ($null -eq $baseline) {
        $failures += "coverage-baseline.json has no entry for '$BaselineKey'"
    }
    elseif ($null -eq $baseline.line -or $null -eq $baseline.branch) {
        $failures += "coverage-baseline.json missing or invalid: '$BaselineKey' needs numeric line and branch"
        $baseline = $null
    }
}
catch {
    $failures += "coverage-baseline.json missing or invalid: $($_.Exception.Message)"
}

if ($line -lt $lineFloor) {
    $failures += "$Label line coverage $($line.ToString($culture))% is below the ${lineFloor}% floor"
}

if ($null -eq $branch) {
    $failures += "$Label report has no branch coverage; the ratchet fails closed"
}

if ($null -ne $baseline) {
    $lineDelta = [math]::Round($line - [double]$baseline.line, 2)
    Add-Output 'line-delta' (Format-Delta $lineDelta)
    if ($lineDelta -lt -$tolerance) {
        $failures += "$Label line coverage $($line.ToString($culture))% is more than $tolerance below the baseline $(([double]$baseline.line).ToString($culture))%"
    }

    if ($null -ne $branch) {
        $branchDelta = [math]::Round($branch - [double]$baseline.branch, 2)
        Add-Output 'branch-delta' (Format-Delta $branchDelta)
        if ($branchDelta -lt -$tolerance) {
            $failures += "$Label branch coverage $($branch.ToString($culture))% is more than $tolerance below the baseline $(([double]$baseline.branch).ToString($culture))%"
        }
    }
}

foreach ($failure in $failures) {
    Write-Host "::error::$failure"
    Add-StepSummary "🔴 $failure"
}

if ($failures.Count -gt 0) { exit 1 }
