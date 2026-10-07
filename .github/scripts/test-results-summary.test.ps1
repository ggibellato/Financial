$ErrorActionPreference = 'Stop'

$here = Split-Path -Parent $MyInvocation.MyCommand.Path
$script = Join-Path $here 'test-results-summary.ps1'
$work = Join-Path ([IO.Path]::GetTempPath()) "test-results-summary-test-$([guid]::NewGuid())"
New-Item -ItemType Directory -Path $work | Out-Null
$failures = 0
$cases = 0

function New-Trx([hashtable[]]$results) {
    $items = foreach ($result in $results) {
        $error = if ($result.Message) { "<Output><ErrorInfo><Message>$($result.Message)</Message></ErrorInfo></Output>" } else { '' }
        "<UnitTestResult testName=`"$($result.Name)`" outcome=`"$($result.Outcome)`">$error</UnitTestResult>"
    }
    $path = Join-Path $work "$([guid]::NewGuid()).trx"
    "<?xml version=`"1.0`" encoding=`"UTF-8`"?><TestRun xmlns=`"http://microsoft.com/schemas/VisualStudio/TeamTest/2010`"><Results>$($items -join '')</Results></TestRun>" | Set-Content $path
    return $path
}

function Invoke-Summary([string]$trx) {
    $summary = Join-Path $work "$([guid]::NewGuid()).md"
    $global:LASTEXITCODE = 0
    $text = & $script -Trx $trx -Name 'Live' -SummaryPath $summary 6>&1 2>&1 | Out-String
    return [pscustomobject]@{
        Exit    = $LASTEXITCODE
        Text    = $text.Trim()
        Summary = if (Test-Path $summary) { Get-Content $summary -Raw } else { '' }
    }
}

function Check([string]$name, [bool]$condition, $result) {
    $script:cases++
    if (-not $condition) {
        $script:failures++
        Write-Host "FAIL ${name}: exit=$($result.Exit) text=$($result.Text) summary=$($result.Summary)"
    }
}

$result = Invoke-Summary (New-Trx @(@{ Name = 'A'; Outcome = 'Passed' }, @{ Name = 'B'; Outcome = 'Passed' }))
Check 'a passing run lists no failure' ($result.Exit -eq 0 -and $result.Summary -match '\| Live \| 2 \| 0 \| 0 \|' -and $result.Summary -notmatch '^- ' ) $result

$result = Invoke-Summary (New-Trx @(
        @{ Name = 'Ns.Good'; Outcome = 'Passed' },
        @{ Name = 'Ns.Bad'; Outcome = 'Failed'; Message = 'Expected 5 but found 6.' },
        @{ Name = 'Ns.Worse'; Outcome = 'Failed'; Message = 'Not found' }))
Check 'failures are counted and listed with their message' ($result.Summary -match '\| Live \| 1 \| 2 \| 0 \|' -and $result.Summary -match '- `Ns\.Bad`: Expected 5 but found 6\.' -and $result.Summary -match '- `Ns\.Worse`: Not found') $result

$result = Invoke-Summary (New-Trx @(@{ Name = 'A'; Outcome = 'Passed' }, @{ Name = 'B'; Outcome = 'NotExecuted' }))
Check 'skipped tests are counted separately' ($result.Summary -match '\| Live \| 1 \| 0 \| 1 \|') $result

$result = Invoke-Summary (New-Trx @(@{ Name = 'A'; Outcome = 'Failed'; Message = "first line`nsecond line" }))
Check 'only the first message line is listed' ($result.Summary -match 'first line' -and $result.Summary -notmatch 'second line') $result

$result = Invoke-Summary (Join-Path $work 'missing.trx')
Check 'a missing trx fails naming the path' ($result.Exit -eq 1 -and $result.Text -match 'missing\.trx') $result

$bad = Join-Path $work 'bad.trx'
'not xml' | Set-Content $bad
$result = Invoke-Summary $bad
Check 'an unparsable trx fails' ($result.Exit -eq 1 -and $result.Text -match 'not a trx file') $result

$result = Invoke-Summary (New-Trx @(@{ Name = 'A'; Outcome = 'Failed' }))
Check 'a run with failures still exits 0' ($result.Exit -eq 0) $result

$many = Join-Path $work 'many'
New-Item -ItemType Directory -Path $many | Out-Null
Copy-Item (New-Trx @(@{ Name = 'A'; Outcome = 'Passed' }, @{ Name = 'B'; Outcome = 'Failed'; Message = 'boom' })) (Join-Path $many 'one.trx')
Copy-Item (New-Trx @()) (Join-Path $many 'empty.trx')
Copy-Item (New-Trx @(@{ Name = 'C'; Outcome = 'Passed' })) (Join-Path $many 'two.trx')
$result = Invoke-Summary (Join-Path $many '*.trx')
Check 'every trx matching a wildcard is combined' ($result.Summary -match '\| Live \| 2 \| 1 \| 0 \|' -and $result.Summary -match '- `B`: boom') $result

Remove-Item -Recurse -Force $work
Write-Host "test-results-summary.test.ps1: $cases cases, $failures failures"
if ($failures -gt 0) { exit 1 }
