param(
    [Parameter(Mandatory)][string]$Report,
    [string]$Tests = '',
    [int]$MaxMutantsPerFile = 15
)

$ErrorActionPreference = 'Stop'
$root = (Split-Path -Parent $PSScriptRoot).TrimEnd('\', '/') + [IO.Path]::DirectorySeparatorChar

if (-not (Test-Path $Report)) { throw "mutation-test-value: report not found: $Report" }
$json = Get-Content $Report -Raw | ConvertFrom-Json -AsHashtable

$testNames = @{}
if ($json.testFiles) {
    foreach ($file in $json.testFiles.GetEnumerator()) {
        foreach ($test in $file.Value.tests) { $testNames[[string]$test.id] = $test.name }
    }
}
function Get-TestName([string]$id) { if ($testNames.ContainsKey($id)) { $testNames[$id] } else { $id } }
function Test-InScope([string]$name) { -not $Tests -or $name -match $Tests }

$undetected = [System.Collections.Generic.List[object]]::new()
$killsByTest = @{}
$coveringTests = [System.Collections.Generic.HashSet[string]]::new()
$sharedKills = 0
$killedMutants = 0
$timedOut = 0

foreach ($file in $json.files.GetEnumerator()) {
    foreach ($mutant in $file.Value.mutants) {
        $key = "$($file.Key)#$($mutant.id)"
        foreach ($id in @($mutant.coveredBy)) { if ($id) { [void]$coveringTests.Add((Get-TestName $id)) } }
        if ($mutant.status -in 'Survived', 'NoCoverage') {
            $undetected.Add([pscustomobject]@{
                File        = $file.Key.Replace($root, '')
                Line        = $mutant.location.start.line
                Status      = $mutant.status
                Mutator     = $mutant.mutatorName
                Replacement = $mutant.replacement
            })
        }
        if ($mutant.status -eq 'Timeout') { $timedOut++ }
        if ($mutant.status -eq 'Killed') {
            $killers = @($mutant.killedBy | Where-Object { $_ } | ForEach-Object { Get-TestName $_ } | Select-Object -Unique)
            $killedMutants++
            if ($killers.Count -gt 1) { $sharedKills++ }
            foreach ($name in $killers) {
                if (-not $killsByTest.ContainsKey($name)) { $killsByTest[$name] = [System.Collections.Generic.HashSet[string]]::new() }
                [void]$killsByTest[$name].Add($key)
            }
        }
    }
}

$tested = $killedMutants + $timedOut + $undetected.Count
if ($tested -gt 0 -and $timedOut / $tested -gt 0.1) {
    Write-Host ''
    Write-Host "WARNING: $timedOut of $tested mutants timed out and count as detected. A timeout is usually an overloaded machine, not a test catching the change: re-run with fewer processes (StrykerJS --concurrency 2, Stryker.NET --concurrency 2) before trusting the score."
}

Write-Host ''
Write-Host "== Undetected mutants ($($undetected.Count)) by file"
foreach ($group in $undetected | Group-Object File | Sort-Object Count -Descending) {
    Write-Host ''
    Write-Host "$($group.Name)  ($($group.Count))"
    foreach ($m in $group.Group | Sort-Object Line | Select-Object -First $MaxMutantsPerFile) {
        $replacement = if ($m.Replacement) { ($m.Replacement -replace '\s+', ' ') } else { '' }
        if ($replacement.Length -gt 70) { $replacement = $replacement.Substring(0, 70) + '...' }
        Write-Host ("  L{0,-5} {1,-10} {2,-28} {3}" -f $m.Line, $m.Status, $m.Mutator, $replacement)
    }
    if ($group.Count -gt $MaxMutantsPerFile) { Write-Host "  ... $($group.Count - $MaxMutantsPerFile) more" }
}

$bailed = $killedMutants -gt 0 -and $sharedKills -eq 0
$bailNote = '  not available: every mutant lists one killer, so the run stopped at the first failing test (re-run with -DisableBail / disableBail)'
$scopeNote = if ($Tests) { " (tests matching '$Tests')" } else { '' }

Write-Host ''
Write-Host "== Tests that run the mutated code but kill nothing$scopeNote"
$idle = @()
if ($bailed) {
    Write-Host $bailNote
} else {
    $idle = @($coveringTests | Where-Object { -not $killsByTest.ContainsKey($_) -and (Test-InScope $_) } | Sort-Object)
    if ($idle.Count -eq 0) { Write-Host '  none' } else { $idle | ForEach-Object { Write-Host "  $_" } }
}

Write-Host ''
Write-Host "== Tests whose every kill is also made by one other test$scopeNote"
$redundant = 0
if ($bailed) {
    Write-Host $bailNote
} else {
    $names = @($killsByTest.Keys | Sort-Object)
    foreach ($candidate in $names | Where-Object { Test-InScope $_ }) {
        $set = $killsByTest[$candidate]
        $cover = $names |
            Where-Object { $_ -ne $candidate -and $killsByTest[$_].Count -ge $set.Count -and $set.IsSubsetOf($killsByTest[$_]) } |
            Where-Object { $killsByTest[$_].Count -gt $set.Count -or [string]::CompareOrdinal($_, $candidate) -lt 0 } |
            Sort-Object { $killsByTest[$_].Count } |
            Select-Object -First 1
        if (-not $cover) { continue }
        $relation = if ($killsByTest[$cover].Count -eq $set.Count) { 'same kills as' } else { 'kills a subset of' }
        Write-Host "  $candidate  ($($set.Count) kills)"
        Write-Host "      $relation $cover  ($($killsByTest[$cover].Count) kills)"
        $redundant++
    }
    if ($redundant -eq 0) { Write-Host '  none' }
}

Write-Host ''
Write-Host "mutation-test-value: $($undetected.Count) undetected, $($idle.Count) idle, $redundant redundant candidates$scopeNote, $sharedKills of $killedMutants kills shared"
