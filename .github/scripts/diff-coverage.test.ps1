$ErrorActionPreference = 'Stop'

$here = Split-Path -Parent $MyInvocation.MyCommand.Path
$script = Join-Path $here 'diff-coverage.ps1'
$work = Join-Path ([IO.Path]::GetTempPath()) "diff-coverage-test-$([guid]::NewGuid().ToString('N'))"
$repo = Join-Path $work 'repo'
New-Item -ItemType Directory -Path $repo | Out-Null

$failures = 0
$cases = 0
$git = @('-c', 'user.email=t@t', '-c', 'user.name=t', '-c', 'commit.gpgsign=false')

function Invoke-Git { & git -C $repo @git @args | Out-Null }

function Write-Source([string]$relative, [int]$lines) {
    $full = Join-Path $repo $relative
    New-Item -ItemType Directory -Force -Path (Split-Path $full) | Out-Null
    (1..$lines | ForEach-Object { "line $_" }) | Out-File -Encoding utf8 $full
}

function Write-Cobertura([string]$source, [string]$filename, [string]$linesXml) {
    @"
<coverage><sources><source>$source</source></sources><packages><package><classes>
<class name="c" filename="$filename"><methods/><lines>$linesXml</lines></class>
</classes></package></packages></coverage>
"@ | Out-File -Encoding utf8 (Join-Path $work 'Cobertura.xml')
}

function Invoke-Script([string]$mergeBase, [string]$prefix = '', [switch]$Blocking) {
    $env:GITHUB_OUTPUT = Join-Path $work 'output.txt'
    $env:GITHUB_STEP_SUMMARY = Join-Path $work 'step-summary.md'
    Remove-Item $env:GITHUB_OUTPUT, $env:GITHUB_STEP_SUMMARY -ErrorAction SilentlyContinue
    $arguments = @('-NoProfile', '-File', $script, '-Label', 'Backend', '-CoberturaPath', (Join-Path $work 'Cobertura.xml'), '-MergeBase', $mergeBase, '-Head', 'HEAD', '-PathPrefix', $prefix)
    if ($Blocking) { $arguments += '-Blocking' }
    Push-Location $repo
    try { $text = & pwsh @arguments 2>&1 | Out-String } finally { Pop-Location }
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

function Line([int]$number, [int]$hits, [string]$condition = '') {
    if ($condition) { return "<line number=`"$number`" hits=`"$hits`" branch=`"true`" condition-coverage=`"$condition`"/>" }
    return "<line number=`"$number`" hits=`"$hits`" branch=`"false`"/>"
}

Invoke-Git init -q
Write-Source 'Financial.Api/Controllers/Foo.cs' 5
Write-Source 'Financial.Api/Other/Skip.txt' 3
Write-Source 'Tests/Financial.Api.Tests/FooTests.cs' 3
Invoke-Git add -A
Invoke-Git commit -q -m base
$base = (& git -C $repo rev-parse HEAD).Trim()

Write-Source 'Financial.Api/Controllers/Foo.cs' 15
Write-Source 'Financial.Api/Other/Skip.txt' 8
Write-Source 'Tests/Financial.Api.Tests/FooTests.cs' 8
Invoke-Git add -A
Invoke-Git commit -q -m change

$linesAllCovered = (6..15 | ForEach-Object { Line $_ 3 }) -join ''
Write-Cobertura 'D:\a\r\r\' 'D:\a\r\r\Financial.Api\Controllers\Foo.cs' $linesAllCovered
$result = Invoke-Script $base
Check 'all changed lines covered, Windows paths mapped' ($result.Exit -eq 0 -and $result.Outputs -match 'diff-line=100% \(10/10\)' -and $result.Outputs -match 'diff-branch=n/a') $result

$oneUncovered = ((6..14 | ForEach-Object { Line $_ 3 }) + (Line 15 0)) -join ''
Write-Cobertura 'D:\a\r\r\' 'D:\a\r\r\Financial.Api\Controllers\Foo.cs' $oneUncovered
$result = Invoke-Script $base
Check 'one uncovered line gives 9/10' ($result.Exit -eq 0 -and $result.Outputs -match 'diff-line=90% \(9/10\)') $result

$branches = ((6..13 | ForEach-Object { Line $_ 3 }) + (Line 14 3 '50% (1/2)') + (Line 15 3 '100% (2/2)')) -join ''
Write-Cobertura 'D:\a\r\r\' 'D:\a\r\r\Financial.Api\Controllers\Foo.cs' $branches
$result = Invoke-Script $base
Check 'condition-coverage 50% (1/2) counts one of two branches' ($result.Outputs -match 'diff-branch=75% \(3/4\)') $result

$unchangedLinesOnly = (1..5 | ForEach-Object { Line $_ 0 }) -join ''
Write-Cobertura 'D:\a\r\r\' 'D:\a\r\r\Financial.Api\Controllers\Foo.cs' $unchangedLinesOnly
$result = Invoke-Script $base
Check 'report lines outside the diff are ignored' ($result.Outputs -match 'diff-line=n/a') $result

$result = Invoke-Script ''
Check 'empty merge base is not computed and does not block' ($result.Exit -eq 0 -and $result.Outputs -match 'diff-line=not computed') $result

Write-Cobertura 'D:\a\r\r\' 'D:\a\r\r\Financial.Api\Controllers\Foo.cs' $linesAllCovered
Remove-Item (Join-Path $work 'Cobertura.xml')
$result = Invoke-Script $base
Check 'missing report is not computed' ($result.Exit -eq 0 -and $result.Outputs -match 'diff-line=not computed') $result
Write-Cobertura 'D:\a\r\r\' 'D:\a\r\r\Financial.Api\Controllers\Foo.cs' $linesAllCovered
Write-Source 'Financial.Api/Other/Skip.txt' 9
Invoke-Git add -A
Invoke-Git commit -q -m docs-only
$docsBase = (& git -C $repo rev-parse HEAD~1).Trim()
Write-Source 'Tests/Financial.Api.Tests/FooTests.cs' 12
Invoke-Git add -A
Invoke-Git commit -q -m tests-only
$result = Invoke-Script $docsBase
Check 'out-of-scope and test-only diff is n/a' ($result.Exit -eq 0 -and $result.Outputs -match 'diff-line=n/a' -and $result.Outputs -match 'diff-branch=n/a') $result

foreach ($row in @(
        @{ Name = 'exactly 80% line and 70% branch pass'; LineHits = 8; BranchCovered = 7; Blocking = $true; Exit = 0; Below = 'false' },
        @{ Name = 'below 80% line fails when blocking'; LineHits = 7; BranchCovered = 7; Blocking = $true; Exit = 1; Below = 'true' },
        @{ Name = 'below 70% branch fails when blocking'; LineHits = 8; BranchCovered = 6; Blocking = $true; Exit = 1; Below = 'true' },
        @{ Name = 'below threshold is advisory without -Blocking'; LineHits = 7; BranchCovered = 6; Blocking = $false; Exit = 0; Below = 'true' })) {
    $rows = @()
    for ($n = 6; $n -le 15; $n++) {
        $hits = if ($n - 6 -lt $row.LineHits) { 1 } else { 0 }
        $rows += Line $n $hits
    }
    $rows[0] = Line 6 1 "$([math]::Round(100 * $row.BranchCovered / 10))% ($($row.BranchCovered)/10)"
    Write-Cobertura 'D:\a\r\r\' 'D:\a\r\r\Financial.Api\Controllers\Foo.cs' ($rows -join '')
    $result = Invoke-Script $base -Blocking:$row.Blocking
    Check $row.Name ($result.Exit -eq $row.Exit -and $result.Outputs -match "diff-below=$($row.Below)") $result
}

Write-Source 'Financial.Web/src/utils/money.ts' 6
Invoke-Git add -A
Invoke-Git commit -q -m web
$webBase = (& git -C $repo rev-parse HEAD~1).Trim()
Write-Cobertura '' 'src/utils/money.ts' ((1..6 | ForEach-Object { Line $_ 2 }) -join '')
$result = Invoke-Script $webBase 'Financial.Web/'
Check 'web report paths gain the Financial.Web prefix' ($result.Outputs -match 'diff-line=100% \(6/6\)') $result

Remove-Item $work -Recurse -Force
Write-Host "diff-coverage.test.ps1: $cases cases, $failures failures"
if ($failures -gt 0) { exit 1 }
