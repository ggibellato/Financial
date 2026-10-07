param(
    [string]$PrdRoot = 'docs/prd',
    [string[]]$TestRoots = @('Tests', 'Financial.Web/src'),
    [string]$SummaryPath = $env:GITHUB_STEP_SUMMARY
)

$ErrorActionPreference = 'Stop'

$criterionLine = [regex]'^\s*- \[[ xX]\] (?<text>.*)$'
$criterionId = [regex]'^\*\*(?<id>P\d+-F\d+-[A-Za-z0-9-]+)\*\*'
$dotnetTag = [regex]'Trait\("AC",\s*"(?<id>[^"]+)"\)'
$webTag = [regex]'\[AC (?<id>[^\]\s]+)\]'

$prds = foreach ($file in Get-ChildItem -Path $PrdRoot -Recurse -File -Filter 'prd-*.md') {
    $ids = [System.Collections.Generic.List[string]]::new()
    $withoutId = 0
    $unreadable = $false
    try {
        foreach ($line in [IO.File]::ReadAllLines($file.FullName)) {
            $criterion = $criterionLine.Match($line)
            if (-not $criterion.Success) { continue }
            $match = $criterionId.Match($criterion.Groups['text'].Value)
            if ($match.Success) { $ids.Add($match.Groups['id'].Value) } else { $withoutId++ }
        }
    } catch {
        $unreadable = $true
    }
    [pscustomobject]@{ Name = $file.Directory.Name; Ids = $ids; WithoutId = $withoutId; Unreadable = $unreadable }
}

$tagged = [System.Collections.Generic.HashSet[string]]::new()
foreach ($root in $TestRoots) {
    $files = Get-ChildItem -Path $root -Recurse -File -Include '*.cs', '*.ts', '*.tsx' -ErrorAction SilentlyContinue |
        Where-Object { $_.FullName -notmatch '[\\/](bin|obj|node_modules)[\\/]' }
    foreach ($file in $files) {
        $pattern = if ($file.Extension -eq '.cs') { $dotnetTag } else { $webTag }
        foreach ($match in $pattern.Matches([IO.File]::ReadAllText($file.FullName))) {
            [void]$tagged.Add($match.Groups['id'].Value)
        }
    }
}

$withIds = @($prds | Where-Object { $_.Ids.Count -gt 0 })
$withoutIds = @($prds | Where-Object { $_.Ids.Count -eq 0 -and -not $_.Unreadable })
$unreadable = @($prds | Where-Object { $_.Unreadable })
$knownIds = [System.Collections.Generic.HashSet[string]]::new([string[]]@($withIds | ForEach-Object { $_.Ids }))
$orphans = @($tagged | Where-Object { -not $knownIds.Contains($_) } | Sort-Object)
$criteriaTotal = ($withIds | ForEach-Object { $_.Ids.Count } | Measure-Object -Sum).Sum
$untracedTotal = @($withIds | ForEach-Object { $_.Ids } | Where-Object { -not $tagged.Contains($_) }).Count

$lines = [System.Collections.Generic.List[string]]::new()
$lines.Add('### Acceptance-criteria traceability (advisory)')
$lines.Add('')
$lines.Add("$($withIds.Count) PRDs carry criterion ids: $untracedTotal of $criteriaTotal criteria have no test tagged with their id.")
foreach ($prd in $withIds) {
    $untraced = @($prd.Ids | Where-Object { -not $tagged.Contains($_) })
    if ($untraced.Count -eq 0) { continue }
    $lines.Add('')
    $lines.Add("**$($prd.Name)**: $($untraced.Count) of $($prd.Ids.Count) untraced")
    foreach ($id in $untraced) { $lines.Add("- ``$id``") }
}
if ($orphans.Count -gt 0) {
    $lines.Add('')
    $lines.Add("**Orphan tags** (no PRD has these ids): $($orphans.Count)")
    foreach ($id in $orphans) { $lines.Add("- ``$id``") }
}
if ($withoutIds.Count -gt 0 -or $unreadable.Count -gt 0) {
    $lines.Add('')
    $lines.Add('<details><summary>' + "$($withoutIds.Count) PRDs have no criterion ids and cannot be traced" + '</summary>')
    $lines.Add('')
    foreach ($prd in $withoutIds) { $lines.Add("- $($prd.Name): $($prd.WithoutId) criteria, no ids") }
    foreach ($prd in $unreadable) { $lines.Add("- $($prd.Name): unreadable") }
    $lines.Add('')
    $lines.Add('</details>')
}

if ($SummaryPath) { Add-Content $SummaryPath $lines }

Write-Host "ac-traceability: $untracedTotal of $criteriaTotal criteria untraced across $($withIds.Count) PRDs, $($orphans.Count) orphan tags, $($withoutIds.Count) PRDs without ids"
