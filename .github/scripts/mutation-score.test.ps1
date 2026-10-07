$ErrorActionPreference = 'Stop'

$here = Split-Path -Parent $MyInvocation.MyCommand.Path
$script = Join-Path $here 'mutation-score.ps1'
$work = Join-Path ([IO.Path]::GetTempPath()) "mutation-score-test-$([guid]::NewGuid())"
New-Item -ItemType Directory -Path $work | Out-Null
$failures = 0
$cases = 0

function New-Report([hashtable]$counts) {
    $mutants = @()
    foreach ($status in $counts.Keys) {
        1..$counts[$status] | ForEach-Object { $mutants += @{ status = $status } }
    }
    $path = Join-Path $work "$([guid]::NewGuid()).json"
    @{ schemaVersion = '2'; files = @{ 'a.cs' = @{ mutants = $mutants } } } | ConvertTo-Json -Depth 6 | Set-Content $path
    return $path
}

function Invoke-Score([string]$report) {
    $summary = Join-Path $work "$([guid]::NewGuid()).md"
    $output = Join-Path $work "$([guid]::NewGuid()).out"
    $text = & pwsh -NoProfile -File $script -Report $report -Name 'Target' -SummaryPath $summary -OutputPath $output 2>&1 | Out-String
    return [pscustomobject]@{
        Exit    = $LASTEXITCODE
        Text    = $text.Trim()
        Summary = if (Test-Path $summary) { Get-Content $summary -Raw } else { '' }
        Output  = if (Test-Path $output) { (Get-Content $output -Raw).Trim() } else { '' }
    }
}

function Check([string]$name, [bool]$condition, $result) {
    $script:cases++
    if (-not $condition) {
        $script:failures++
        Write-Host "FAIL ${name}: exit=$($result.Exit) text=$($result.Text) output=$($result.Output)"
    }
}

$result = Invoke-Score (New-Report @{ Killed = 6; Timeout = 2; Survived = 2 })
Check 'killed and timeout both count as detected' ($result.Exit -eq 0 -and $result.Output -eq 'score=80.0') $result

$result = Invoke-Score (New-Report @{ Killed = 4; Survived = 1; NoCoverage = 5 })
Check 'no coverage counts as undetected' ($result.Output -eq 'score=40.0') $result

$result = Invoke-Score (New-Report @{ Killed = 3; Survived = 1; Ignored = 5; CompileError = 5; RuntimeError = 2 })
Check 'ignored, compile and runtime errors are excluded' ($result.Output -eq 'score=75.0') $result

$result = Invoke-Score (New-Report @{ Killed = 1; Survived = 2 })
Check 'the score is rounded to one decimal' ($result.Output -eq 'score=33.3') $result

$result = Invoke-Score (New-Report @{ Ignored = 4; CompileError = 1 })
Check 'a report with no testable mutants fails' ($result.Exit -eq 1 -and $result.Text -match 'no testable mutants') $result

$result = Invoke-Score (Join-Path $work 'missing.json')
Check 'a missing report fails naming the path' ($result.Exit -eq 1 -and $result.Text -match 'missing\.json') $result

$bad = Join-Path $work 'bad.json'
'not json' | Set-Content $bad
$result = Invoke-Score $bad
Check 'an unparsable report fails' ($result.Exit -eq 1 -and $result.Text -match 'not a mutation report') $result

$result = Invoke-Score (New-Report @{ Killed = 1; Survived = 1 })
Check 'the summary carries the header and one row' ($result.Summary -match '\| Mutation target \|' -and $result.Summary -match '\| Target \| 50\.0% \| 1 \| 0 \| 1 \| 0 \|') $result

$twice = Join-Path $work 'twice.md'
$report = New-Report @{ Killed = 1 }
& pwsh -NoProfile -File $script -Report $report -Name 'One' -SummaryPath $twice -OutputPath (Join-Path $work 'x.out') | Out-Null
& pwsh -NoProfile -File $script -Report $report -Name 'Two' -SummaryPath $twice -OutputPath (Join-Path $work 'x.out') | Out-Null
$headers = ([regex]::Matches((Get-Content $twice -Raw), '\| Mutation target \|')).Count
Check 'a second row reuses the header' ($headers -eq 1) ([pscustomobject]@{ Exit = 0; Text = ''; Output = "headers=$headers" })

Remove-Item -Recurse -Force $work
Write-Host "mutation-score.test.ps1: $cases cases, $failures failures"
if ($failures -gt 0) { exit 1 }
