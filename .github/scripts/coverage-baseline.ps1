param(
    [Parameter(Mandatory)][string]$Backend,
    [Parameter(Mandatory)][string]$Wpf,
    [Parameter(Mandatory)][string]$Web,
    [string]$Path = 'coverage-baseline.json'
)

$ErrorActionPreference = 'Stop'

function Read-Figures([string]$summaryPath) {
    $summary = (Get-Content $summaryPath -Raw | ConvertFrom-Json).summary
    if ($null -eq $summary.linecoverage -or $null -eq $summary.branchcoverage) {
        throw "$summaryPath has no line or branch coverage"
    }

    return [ordered]@{
        line   = [math]::Round([double]$summary.linecoverage, 2)
        branch = [math]::Round([double]$summary.branchcoverage, 2)
    }
}

$baseline = [ordered]@{
    backend = Read-Figures $Backend
    wpf     = Read-Figures $Wpf
    web     = Read-Figures $Web
}

$baseline | ConvertTo-Json -Depth 3 | Out-File -Encoding utf8 $Path
