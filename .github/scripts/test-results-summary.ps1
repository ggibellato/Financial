param(
    [Parameter(Mandatory)][string]$Trx,
    [Parameter(Mandatory)][string]$Name,
    [string]$SummaryPath = $env:GITHUB_STEP_SUMMARY
)

$ErrorActionPreference = 'Stop'

$files = @(Get-ChildItem -Path $Trx -File -ErrorAction SilentlyContinue)
if ($files.Count -eq 0) {
    Write-Host "test-results-summary: trx not found: $Trx"
    exit 1
}

$results = @()
foreach ($file in $files) {
    try {
        $results += @(([xml](Get-Content $file.FullName -Raw)).TestRun.Results.UnitTestResult)
    } catch {
        Write-Host "test-results-summary: not a trx file: $($file.FullName) ($($_.Exception.Message))"
        exit 1
    }
}

$passed = @($results | Where-Object { $_.outcome -eq 'Passed' }).Count
$failed = @($results | Where-Object { $_.outcome -eq 'Failed' })
$skipped = @($results | Where-Object { $_.outcome -eq 'NotExecuted' }).Count

if ($SummaryPath) {
    if (-not (Test-Path $SummaryPath) -or (Get-Content $SummaryPath -Raw) -notmatch '\| Test run \|') {
        Add-Content $SummaryPath '| Test run | Passed | Failed | Skipped |'
        Add-Content $SummaryPath '|---|--:|--:|--:|'
    }
    Add-Content $SummaryPath "| $Name | $passed | $($failed.Count) | $skipped |"

    foreach ($result in $failed) {
        $message = "$($result.Output.ErrorInfo.Message)".Trim() -split "`r?`n" | Select-Object -First 1
        Add-Content $SummaryPath ''
        Add-Content $SummaryPath "- ``$($result.testName)``: $message"
    }
}

Write-Host "test-results-summary: $Name $passed passed, $($failed.Count) failed, $skipped skipped"
