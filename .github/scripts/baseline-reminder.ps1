param(
    [string]$Backend = '',
    [string]$Wpf = '',
    [string]$Web = '',
    [string]$RunUrl = ''
)

$ErrorActionPreference = 'Stop'
$tolerance = 0.5
$culture = [cultureinfo]::InvariantCulture

$stale = @()
foreach ($entry in @(@('Backend', $Backend), @('WPF', $Wpf), @('Web', $Web))) {
    $label = $entry[0]
    $lineDelta, $branchDelta = $entry[1] -split '\|'
    $rises = @()
    foreach ($metric in @(@('line', $lineDelta), @('branch', $branchDelta))) {
        $value = 0.0
        if ([double]::TryParse($metric[1], [Globalization.NumberStyles]::Float, $culture, [ref]$value) -and $value -gt $tolerance) {
            $rises += "$($metric[0]) $($metric[1])"
        }
    }
    if ($rises.Count -gt 0) { $stale += "- $label`: $($rises -join ', ')" }
}

if ($stale.Count -eq 0) { exit 0 }

@(
    '### Coverage baseline is stale'
    ''
    "Coverage rose by more than $tolerance points against ``coverage-baseline.json`` (points above the baseline):"
    ''
    $stale
    ''
    'Until the baseline is raised, a later PR can lose that gain and still pass the ratchet. To raise it, download the three `coverage-report-*` artifacts of this run' + $(if ($RunUrl) { " ($RunUrl)" } else { '' }) + ', run `pwsh .github/scripts/coverage-baseline.ps1 -Backend <Summary.json> -Wpf <Summary.json> -Web <Summary.json>`, and commit the file in its own PR.'
) -join "`n"
