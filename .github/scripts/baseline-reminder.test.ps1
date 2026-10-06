$ErrorActionPreference = 'Stop'

$here = Split-Path -Parent $MyInvocation.MyCommand.Path
$script = Join-Path $here 'baseline-reminder.ps1'
$failures = 0
$cases = 0

function Invoke-Reminder([string]$backend, [string]$wpf, [string]$web) {
    $text = & pwsh -NoProfile -File $script -Backend $backend -Wpf $wpf -Web $web -RunUrl 'http://run' 2>&1 | Out-String
    return [pscustomobject]@{ Exit = $LASTEXITCODE; Text = $text.Trim() }
}

function Check([string]$name, [bool]$condition, $result) {
    $script:cases++
    if (-not $condition) {
        $script:failures++
        Write-Host "FAIL ${name}: exit=$($result.Exit) text=$($result.Text)"
    }
}

$result = Invoke-Reminder '+0.00|+0.00' '-0.30|+0.20' '+0.50|+0.50'
Check 'no figure above 0.5 is silent (exactly 0.5 included)' ($result.Exit -eq 0 -and $result.Text -eq '') $result

$result = Invoke-Reminder '+0.00|+0.00' '+0.60|+0.10' '+0.00|+0.00'
Check 'one rising line is reported with its label only' ($result.Text -match '- WPF: line \+0\.60' -and $result.Text -notmatch '- Backend:' -and $result.Text -notmatch '- Web:' -and $result.Text -match 'http://run') $result

$result = Invoke-Reminder '+0.00|+1.20' '+2.00|+0.70' ''
Check 'line and branch rises are both listed' ($result.Text -match '- Backend: branch \+1\.20' -and $result.Text -match '- WPF: line \+2\.00, branch \+0\.70') $result

$result = Invoke-Reminder '|' '-1.00|-2.00' ''
Check 'empty deltas (skipped job) and drops are silent' ($result.Exit -eq 0 -and $result.Text -eq '') $result

Write-Host "baseline-reminder.test.ps1: $cases cases, $failures failures"
if ($failures -gt 0) { exit 1 }
