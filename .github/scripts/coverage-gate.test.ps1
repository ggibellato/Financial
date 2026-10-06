$ErrorActionPreference = 'Stop'

$here = Split-Path -Parent $MyInvocation.MyCommand.Path
$gate = Join-Path $here 'coverage-gate.ps1'
$writer = Join-Path $here 'coverage-baseline.ps1'
$work = Join-Path ([IO.Path]::GetTempPath()) "coverage-gate-test-$([guid]::NewGuid().ToString('N'))"
New-Item -ItemType Directory -Path $work | Out-Null

$failures = 0
$cases = 0

function Write-Summary([string]$name, $line, $branch) {
    $summary = [ordered]@{ linecoverage = $line }
    if ($null -ne $branch) { $summary.branchcoverage = $branch }
    @{ summary = $summary } | ConvertTo-Json | Out-File -Encoding utf8 (Join-Path $work $name)
}

function Write-Baseline([string]$content) {
    $content | Out-File -Encoding utf8 (Join-Path $work 'baseline.json')
}

function Invoke-Gate([string]$baselineFile = 'baseline.json') {
    $env:GITHUB_OUTPUT = Join-Path $work 'output.txt'
    $env:GITHUB_STEP_SUMMARY = Join-Path $work 'step-summary.md'
    Remove-Item $env:GITHUB_OUTPUT, $env:GITHUB_STEP_SUMMARY -ErrorAction SilentlyContinue
    $text = & pwsh -NoProfile -File $gate -Label Backend -SummaryPath (Join-Path $work 'Summary.json') `
        -BaselineKey backend -BaselinePath (Join-Path $work $baselineFile) 2>&1 | Out-String
    $outputs = if (Test-Path $env:GITHUB_OUTPUT) { Get-Content $env:GITHUB_OUTPUT -Raw } else { '' }
    return [pscustomobject]@{ Exit = $LASTEXITCODE; Text = $text; Outputs = $outputs }
}

function Check([string]$name, [bool]$condition, $result) {
    $script:cases++
    if (-not $condition) {
        $script:failures++
        Write-Host "FAIL ${name}: exit=$($result.Exit) text=$($result.Text.Trim()) outputs=$($result.Outputs.Trim())"
    }
}

$standardBaseline = '{ "backend": { "line": 95.0, "branch": 90.0 }, "wpf": { "line": 94.0, "branch": 80.0 } }'

Write-Baseline $standardBaseline
Write-Summary 'Summary.json' 95.0 90.0
$result = Invoke-Gate
Check 'at baseline passes with zero deltas' ($result.Exit -eq 0 -and $result.Outputs -match 'line-delta=\+0\.00' -and $result.Outputs -match 'branch-delta=\+0\.00') $result

foreach ($row in @(
        @{ Metric = 'line'; Delta = 0.4; Pass = $true },
        @{ Metric = 'line'; Delta = 0.6; Pass = $false },
        @{ Metric = 'branch'; Delta = 0.4; Pass = $true },
        @{ Metric = 'branch'; Delta = 0.6; Pass = $false },
        @{ Metric = 'branch'; Delta = 0.5; Pass = $true })) {
    $line = if ($row.Metric -eq 'line') { 95.0 - $row.Delta } else { 95.0 }
    $branch = if ($row.Metric -eq 'branch') { 90.0 - $row.Delta } else { 90.0 }
    Write-Summary 'Summary.json' $line $branch
    $result = Invoke-Gate
    $expectedExit = if ($row.Pass) { 0 } else { 1 }
    $messageOk = $row.Pass -or $result.Text -match "$($row.Metric) coverage .* is more than 0\.5 below the baseline"
    Check "$($row.Metric) down $($row.Delta)" ($result.Exit -eq $expectedExit -and $messageOk) $result
}

Write-Baseline '{ "backend": { "line": 89.5, "branch": 80.0 } }'
Write-Summary 'Summary.json' 89.9 80.0
$result = Invoke-Gate
Check 'line below the 90 floor fails even with a low baseline' ($result.Exit -eq 1 -and $result.Text -match 'below the 90% floor') $result

Write-Baseline $standardBaseline
Write-Summary 'Summary.json' 97.2 93.1
$result = Invoke-Gate
Check 'improvement passes with positive deltas and all outputs' (
    $result.Exit -eq 0 -and $result.Outputs -match 'coverage=97\.2' -and $result.Outputs -match 'branch=93\.1' -and
    $result.Outputs -match 'line-delta=\+2\.20' -and $result.Outputs -match 'branch-delta=\+3\.10' -and $result.Outputs -match 'emoji=') $result

Write-Summary 'Summary.json' 95.0 90.0
$result = Invoke-Gate 'does-not-exist.json'
Check 'baseline file missing fails closed' ($result.Exit -eq 1 -and $result.Text -match 'coverage-baseline\.json missing or invalid') $result

Write-Baseline '{ not json'
$result = Invoke-Gate
Check 'baseline unparsable fails closed' ($result.Exit -eq 1 -and $result.Text -match 'coverage-baseline\.json missing or invalid') $result

Write-Baseline '{ "wpf": { "line": 94.0, "branch": 80.0 } }'
$result = Invoke-Gate
Check 'baseline key missing fails closed and names the key' ($result.Exit -eq 1 -and $result.Text -match "no entry for 'backend'") $result

Write-Baseline $standardBaseline
Write-Summary 'Summary.json' 95.0 $null
$result = Invoke-Gate
Check 'branch figure missing fails closed' ($result.Exit -eq 1 -and $result.Text -match 'no branch coverage; the ratchet fails closed') $result

foreach ($name in 'backend', 'wpf', 'web') {
    Write-Summary "$name-summary.json" 95.123 90.987
}
& pwsh -NoProfile -File $writer -Backend (Join-Path $work 'backend-summary.json') -Wpf (Join-Path $work 'wpf-summary.json') `
    -Web (Join-Path $work 'web-summary.json') -Path (Join-Path $work 'written.json')
$written = Get-Content (Join-Path $work 'written.json') -Raw | ConvertFrom-Json
$script:cases++
if (-not ($written.backend.line -eq 95.12 -and $written.backend.branch -eq 90.99 -and $written.web.line -eq 95.12 -and $written.wpf.branch -eq 90.99)) {
    $script:failures++
    Write-Host "FAIL baseline writer: $($written | ConvertTo-Json -Compress)"
}

Remove-Item $work -Recurse -Force
Write-Host "coverage-gate.test.ps1: $cases cases, $failures failures"
if ($failures -gt 0) { exit 1 }
