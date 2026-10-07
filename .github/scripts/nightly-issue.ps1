param(
    [string]$FailedJobs = '',
    [Parameter(Mandatory)][string]$RunUrl,
    [string]$Gh = 'gh',
    [switch]$DryRun
)

$ErrorActionPreference = 'Stop'
$label = 'nightly-failure'

$jobs = @($FailedJobs -split ',' | ForEach-Object { $_.Trim() } | Where-Object { $_ })
if ($jobs.Count -eq 0) {
    Write-Host 'nightly-issue: no failed job, nothing to report'
    exit 0
}

function Invoke-Gh {
    $output = & $Gh @args
    if ($LASTEXITCODE -ne 0) {
        Write-Host "nightly-issue: gh $($args -join ' ') failed"
        exit 1
    }
    return $output
}

$body = "Failed jobs: $($jobs -join ', ')`n`nRun: $RunUrl"
$open = "$(Invoke-Gh issue list --label $label --state open --limit 1 --json number --jq '.[0].number')".Trim()

if ($DryRun) {
    if ($open) { Write-Host "nightly-issue: would comment on #$open`n$body" } else { Write-Host "nightly-issue: would create an issue`n$body" }
    exit 0
}

Invoke-Gh label create $label --force --color D93F0B --description 'The scheduled nightly quality pipeline failed' | Out-Null

if ($open) {
    Invoke-Gh issue comment $open --body $body | Out-Null
    Write-Host "nightly-issue: commented on #$open"
} else {
    Invoke-Gh issue create --title 'Nightly quality pipeline failed' --label $label --body $body | Out-Null
    Write-Host 'nightly-issue: created the issue'
}
