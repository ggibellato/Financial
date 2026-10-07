param(
    [Parameter(Mandatory)][string]$NeedsJson,
    [Parameter(Mandatory)][string]$RunUrl,
    [string]$Gh = 'gh'
)

$ErrorActionPreference = 'Stop'
$label = 'nightly-failure'

$jobs = @(($NeedsJson | ConvertFrom-Json).PSObject.Properties |
    Where-Object { $_.Value.result -notin 'success', 'skipped' } |
    ForEach-Object { $_.Name })
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

if ($open) {
    Invoke-Gh issue comment $open --body $body | Out-Null
    Write-Host "nightly-issue: commented on #$open"
} else {
    Invoke-Gh label create $label --force --color D93F0B --description 'The scheduled nightly quality pipeline failed' | Out-Null
    Invoke-Gh issue create --title 'Nightly quality pipeline failed' --label $label --body $body | Out-Null
    Write-Host 'nightly-issue: created the issue'
}
