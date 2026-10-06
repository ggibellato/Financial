param(
    [Parameter(Mandatory)][string]$Label,
    [Parameter(Mandatory)][string]$CoberturaPath,
    [string]$MergeBase = '',
    [string]$Head = 'HEAD',
    [string]$PathPrefix = '',
    [switch]$Blocking
)

$ErrorActionPreference = 'Stop'
$lineThreshold = 80
$branchThreshold = 70
$scope = @(
    '^Financial\.[^/]+\.(Domain|Application)/',
    '^Financial\.Api/',
    '^Financial\.App/ViewModels/',
    '^Financial\.Web/src/(hooks|utils|components|pages)/'
)
$sourceExtensions = '\.(cs|ts|tsx)$'

function Add-Output([string]$name, [string]$value) {
    if ($env:GITHUB_OUTPUT) { "$name=$value" | Out-File -Append -Encoding utf8 $env:GITHUB_OUTPUT }
}

function Add-StepSummary([string]$text) {
    if ($env:GITHUB_STEP_SUMMARY) { $text | Out-File -Append -Encoding utf8 $env:GITHUB_STEP_SUMMARY }
}

function Format-Ratio([int]$covered, [int]$total) {
    if ($total -eq 0) { return 'n/a' }
    $percent = [math]::Round(100.0 * $covered / $total, 1)
    return "$($percent.ToString([cultureinfo]::InvariantCulture))% ($covered/$total)"
}

function Complete([string]$line, [string]$branch, [bool]$fail, [string]$reason) {
    Add-Output 'diff-line' $line
    Add-Output 'diff-branch' $branch
    Add-Output 'diff-below' $(if ($reason) { 'true' } else { 'false' })
    Add-StepSummary "$Label diff coverage: line $line, branch $branch"
    if ($reason) { Add-StepSummary $reason }
    if ($fail) {
        Write-Host "::error::$reason"
        exit 1
    }
    exit 0
}

if (-not $MergeBase -or -not (Test-Path $CoberturaPath)) { Complete 'not computed' 'not computed' $false '' }

$diff = & git diff -U0 --no-color $MergeBase $Head 2>$null
if ($LASTEXITCODE -ne 0) { Complete 'not computed' 'not computed' $false '' }

$changed = @{}
$file = $null
foreach ($row in $diff) {
    if ($row -match '^\+\+\+ b/(.+)$') {
        $file = $Matches[1]
        $inScope = ($file -match $sourceExtensions) -and ($scope | Where-Object { $file -match $_ })
        if ($inScope) { $changed[$file] = [System.Collections.Generic.HashSet[int]]::new() } else { $file = $null }
    }
    elseif ($file -and $row -match '^@@ -\S+ \+(\d+)(?:,(\d+))? @@') {
        $start = [int]$Matches[1]
        $count = if ($Matches[2] -ne $null -and $Matches[2] -ne '') { [int]$Matches[2] } else { 1 }
        for ($n = $start; $n -lt $start + $count; $n++) { [void]$changed[$file].Add($n) }
    }
}

if ($changed.Count -eq 0) { Complete 'n/a' 'n/a' $false '' }

[xml]$report = Get-Content $CoberturaPath -Raw
$sources = @($report.coverage.sources.source | Where-Object { $_ } | ForEach-Object { ($_ -replace '\\', '/').TrimEnd('/') + '/' })

$lineHits = @{}
$branchHits = @{}
foreach ($class in $report.SelectNodes('//class')) {
    $path = $class.filename -replace '\\', '/'
    foreach ($source in $sources) {
        if ($path.StartsWith($source, [StringComparison]::OrdinalIgnoreCase)) { $path = $path.Substring($source.Length); break }
    }
    $path = "$PathPrefix$path"
    if (-not $changed.ContainsKey($path)) { continue }
    foreach ($line in $class.SelectNodes('lines/line')) {
        $number = [int]$line.number
        if (-not $changed[$path].Contains($number)) { continue }
        $key = "${path}:$number"
        $hit = [int]$line.hits -gt 0
        $lineHits[$key] = $lineHits[$key] -or $hit
        if ($line.'condition-coverage' -match '\((\d+)/(\d+)\)') {
            $covered = [int]$Matches[1]
            $total = [int]$Matches[2]
            if (-not $branchHits.ContainsKey($key) -or $branchHits[$key].Covered -lt $covered) {
                $branchHits[$key] = @{ Covered = $covered; Total = $total }
            }
        }
    }
}

$lineTotal = $lineHits.Count
$lineCovered = @($lineHits.Values | Where-Object { $_ }).Count
$branchTotal = 0
$branchCovered = 0
foreach ($entry in $branchHits.Values) { $branchTotal += $entry.Total; $branchCovered += $entry.Covered }

$failures = @()
if ($lineTotal -gt 0 -and $lineCovered * 100 -lt $lineThreshold * $lineTotal) {
    $failures += "$Label diff line coverage $(Format-Ratio $lineCovered $lineTotal) is below the ${lineThreshold}% threshold"
}
if ($branchTotal -gt 0 -and $branchCovered * 100 -lt $branchThreshold * $branchTotal) {
    $failures += "$Label diff branch coverage $(Format-Ratio $branchCovered $branchTotal) is below the ${branchThreshold}% threshold"
}

$reason = $failures -join '; '
Complete (Format-Ratio $lineCovered $lineTotal) (Format-Ratio $branchCovered $branchTotal) ($Blocking.IsPresent -and $failures.Count -gt 0) $reason
