param(
    [Parameter(Mandatory)][string]$Report,
    [Parameter(Mandatory)][string]$Name,
    [string]$SummaryPath = $env:GITHUB_STEP_SUMMARY,
    [string]$OutputPath = $env:GITHUB_OUTPUT
)

$ErrorActionPreference = 'Stop'
$culture = [cultureinfo]::InvariantCulture

if (-not (Test-Path $Report)) {
    Write-Host "mutation-score: report not found: $Report"
    exit 1
}

try {
    $json = Get-Content $Report -Raw | ConvertFrom-Json
    $statuses = @($json.files.PSObject.Properties | ForEach-Object { $_.Value.mutants } | ForEach-Object { $_.status })
} catch {
    Write-Host "mutation-score: report is not a mutation report: $Report ($($_.Exception.Message))"
    exit 1
}

function Count-Status([string[]]$names) {
    return @($statuses | Where-Object { $names -contains $_ }).Count
}

$killed = Count-Status 'Killed'
$timeout = Count-Status 'Timeout'
$survived = Count-Status 'Survived'
$noCoverage = Count-Status 'NoCoverage'
$detected = $killed + $timeout
$valid = $detected + $survived + $noCoverage

if ($valid -eq 0) {
    Write-Host "mutation-score: report has no testable mutants: $Report"
    exit 1
}

$score = [math]::Round(100.0 * $detected / $valid, 1)
$scoreText = $score.ToString('0.0', $culture)

if ($SummaryPath) {
    if (-not (Test-Path $SummaryPath) -or (Get-Content $SummaryPath -Raw) -notmatch '\| Mutation target \|') {
        Add-Content $SummaryPath '| Mutation target | Score | Killed | Timeout | Survived | No coverage |'
        Add-Content $SummaryPath '|---|--:|--:|--:|--:|--:|'
    }
    Add-Content $SummaryPath "| $Name | $scoreText% | $killed | $timeout | $survived | $noCoverage |"
}

if ($OutputPath) {
    Add-Content $OutputPath "score=$scoreText"
}

Write-Host "mutation-score: $Name $scoreText% ($detected of $valid mutants detected)"
