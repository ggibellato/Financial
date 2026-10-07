$ErrorActionPreference = 'Stop'

$here = Split-Path -Parent $MyInvocation.MyCommand.Path
$script = Join-Path $here 'nightly-issue.ps1'
$work = Join-Path ([IO.Path]::GetTempPath()) "nightly-issue-test-$([guid]::NewGuid())"
New-Item -ItemType Directory -Path $work | Out-Null
$failures = 0
$cases = 0

$fake = Join-Path $work 'fake-gh.ps1'
Set-Content $fake @'
Add-Content $env:FAKE_GH_LOG (($args -join ' ') -replace '\r?\n', ' / ')
if ($env:FAKE_GH_FAIL -and ($args -join ' ') -match $env:FAKE_GH_FAIL) { exit 1 }
if ($args[0] -eq 'issue' -and $args[1] -eq 'list') { Write-Output $env:FAKE_OPEN_ISSUE }
exit 0
'@

function Invoke-Issue([hashtable]$needs, [string]$openIssue = '', [string]$failOn = '') {
    $env:FAKE_GH_LOG = Join-Path $work "$([guid]::NewGuid()).log"
    $env:FAKE_OPEN_ISSUE = $openIssue
    $env:FAKE_GH_FAIL = $failOn
    $global:LASTEXITCODE = 0
    $needsJson = $needs.GetEnumerator() | ForEach-Object -Begin { $map = [ordered]@{} } -Process { $map[$_.Key] = @{ result = $_.Value } } -End { $map | ConvertTo-Json -Compress }
    $text = & $script -NeedsJson $needsJson -RunUrl 'https://example.test/run/1' -Gh $fake 6>&1 2>&1 | Out-String
    return [pscustomobject]@{
        Exit  = $LASTEXITCODE
        Text  = $text.Trim()
        Calls = if (Test-Path $env:FAKE_GH_LOG) { @(Get-Content $env:FAKE_GH_LOG) } else { @() }
    }
}

function Check([string]$name, [bool]$condition, $result) {
    $script:cases++
    if (-not $condition) {
        $script:failures++
        Write-Host "FAIL ${name}: exit=$($result.Exit) text=$($result.Text) calls=$($result.Calls -join ' | ')"
    }
}

$result = Invoke-Issue @{ 'stryker-js' = 'success'; 'e2e-web' = 'skipped' }
Check 'no failed job does nothing' ($result.Exit -eq 0 -and $result.Calls.Count -eq 0) $result

$result = Invoke-Issue @{ 'stryker-js' = 'failure'; 'e2e-web' = 'failure'; 'e2e-wpf' = 'success' }
Check 'no open issue creates one with the label, the failed jobs and the run url' (
    $result.Exit -eq 0 -and
    @($result.Calls | Where-Object { $_ -match '^issue create .*--label nightly-failure' -and $_ -match 'stryker-js' -and $_ -match 'e2e-web' -and $_ -notmatch 'e2e-wpf' -and $_ -match 'https://example.test/run/1' }).Count -eq 1 -and
    @($result.Calls | Where-Object { $_ -match '^issue comment' }).Count -eq 0) $result
Check 'the label is created once with force before the issue' (@($result.Calls | Where-Object { $_ -match '^label create nightly-failure --force' }).Count -eq 1) $result

$result = Invoke-Issue @{ 'e2e-wpf' = 'cancelled' }
Check 'a cancelled job, which is how a timeout ends, counts as failed' (@($result.Calls | Where-Object { $_ -match '^issue create .*e2e-wpf' }).Count -eq 1) $result

$result = Invoke-Issue @{ 'e2e-wpf' = 'failure' } '42'
Check 'an open issue gets a comment and no second issue or label call' (
    @($result.Calls | Where-Object { $_ -match '^issue comment 42 .*e2e-wpf' }).Count -eq 1 -and
    @($result.Calls | Where-Object { $_ -match '^(issue create|label create)' }).Count -eq 0) $result

$result = Invoke-Issue @{ 'e2e-wpf' = 'failure' } '' '^issue create'
Check 'a failing gh call exits 1' ($result.Exit -eq 1 -and $result.Text -match 'failed') $result

Remove-Item -Recurse -Force $work
Remove-Item Env:\FAKE_GH_LOG, Env:\FAKE_OPEN_ISSUE, Env:\FAKE_GH_FAIL -ErrorAction SilentlyContinue
Write-Host "nightly-issue.test.ps1: $cases cases, $failures failures"
if ($failures -gt 0) { exit 1 }
