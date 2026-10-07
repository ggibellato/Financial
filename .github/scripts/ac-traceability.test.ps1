$ErrorActionPreference = 'Stop'

$here = Split-Path -Parent $MyInvocation.MyCommand.Path
$script = Join-Path $here 'ac-traceability.ps1'
$work = Join-Path ([IO.Path]::GetTempPath()) "ac-traceability-test-$([guid]::NewGuid())"
$failures = 0
$cases = 0

function New-File([string]$relative, [string]$content) {
    $path = Join-Path $work $relative
    New-Item -ItemType Directory -Path (Split-Path -Parent $path) -Force | Out-Null
    Set-Content -Path $path -Value $content
}

function Invoke-Report {
    $summary = Join-Path $work "$([guid]::NewGuid()).md"
    $global:LASTEXITCODE = 0
    $text = & $script -PrdRoot (Join-Path $work 'prd') -TestRoots @((Join-Path $work 'Tests'), (Join-Path $work 'web')) -SummaryPath $summary 6>&1 2>&1 | Out-String
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

New-File 'prd/P90-prd-alpha/prd-alpha.md' @'
# Alpha
- [ ] **P90-F01-alpha-01** unticked, traced by a Trait
- [x] **P90-F01-alpha-02** ticked, traced by a web title
- [ ] **P90-F01-alpha-03** untraced
- [ ] not an id'd criterion
'@
New-File 'prd/P91-prd-beta/prd-beta.md' @'
# Beta
- [ ] first criterion without an id
- [x] second criterion without an id
'@
New-File 'Tests/Acceptance/AlphaTests.cs' @'
[Trait("AC", "P90-F01-alpha-01")]
public void Works() { }
[Trait("AC", "P99-F01-ghost-01")]
public void Ghost() { }
'@
New-File 'Tests/bin/Debug/Stale.cs' '[Trait("AC", "P90-F01-alpha-03")]'
New-File 'web/components/Alpha.test.tsx' "it('[AC P90-F01-alpha-02] shows the value', () => {})"

$result = Invoke-Report
Check 'an id with a Trait tag is traced' ($result.Summary -notmatch 'P90-F01-alpha-01') $result
Check 'an id with a web test title is traced' ($result.Summary -notmatch 'P90-F01-alpha-02') $result
Check 'an id with no tag is listed under its PRD' ($result.Summary -match 'P90-prd-alpha\*\*: 1 of 3 untraced' -and $result.Summary -match 'P90-F01-alpha-03') $result
Check 'tags under bin are ignored' ($result.Summary -match 'P90-F01-alpha-03') $result
Check 'a PRD without ids yields one line with its criteria count' ($result.Summary -match 'P91-prd-beta: 2 criteria, no ids' -and $result.Summary -match '1 PRDs have no criterion ids') $result
Check 'a tag naming an unknown id is an orphan' ($result.Summary -match 'Orphan tags[^\n]*: 1' -and $result.Summary -match 'P99-F01-ghost-01') $result
Check 'the totals count ticked and unticked criteria' ($result.Summary -match '1 PRDs carry criterion ids: 1 of 3 criteria') $result
Check 'the run exits 0 when criteria are untraced' ($result.Exit -eq 0) $result

Remove-Item -Recurse -Force $work
Write-Host "ac-traceability.test.ps1: $cases cases, $failures failures"
if ($failures -gt 0) { exit 1 }
