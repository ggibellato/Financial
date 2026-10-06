$ErrorActionPreference = 'Stop'

$here = Split-Path -Parent $MyInvocation.MyCommand.Path
$script = Join-Path $here 'coverage-exclusions.ps1'
$work = Join-Path ([IO.Path]::GetTempPath()) "coverage-exclusions-test-$([guid]::NewGuid().ToString('N'))"
$repo = Join-Path $work 'repo'
New-Item -ItemType Directory -Path (Join-Path $repo 'Financial.Web') | Out-Null

$failures = 0
$cases = 0
$gitOptions = @('-c', 'user.email=t@t', '-c', 'user.name=t', '-c', 'commit.gpgsign=false')

function Invoke-Git { & git -C $repo @gitOptions @args | Out-Null }

function Write-Config([string[]]$coverlet, [string[]]$vite) {
    "<RunSettings><Exclude>$($coverlet -join ',')</Exclude></RunSettings>" | Out-File -Encoding utf8 (Join-Path $repo 'coverlet.runsettings')
    $quoted = ($vite | ForEach-Object { "'$_'" }) -join ', '
    "export default { test: { exclude: ['tests/e2e/**'], coverage: { provider: 'v8', exclude: [...defaults, $quoted] } } }" |
        Out-File -Encoding utf8 (Join-Path $repo 'Financial.Web/vite.config.ts')
}

function Commit-Config([string[]]$coverlet, [string[]]$vite) {
    Write-Config $coverlet $vite
    Invoke-Git add -A
    Invoke-Git commit -q -m change
    return (& git -C $repo rev-parse HEAD).Trim()
}

function Invoke-Script([string]$mergeBase) {
    $env:GITHUB_OUTPUT = Join-Path $work 'output.txt'
    Remove-Item $env:GITHUB_OUTPUT -ErrorAction SilentlyContinue
    Push-Location $repo
    try { $text = & pwsh -NoProfile -File $script -MergeBase $mergeBase -Head HEAD 2>&1 | Out-String } finally { Pop-Location }
    $outputs = if (Test-Path $env:GITHUB_OUTPUT) { (Get-Content $env:GITHUB_OUTPUT -Raw).Trim() } else { '' }
    return [pscustomobject]@{ Exit = $LASTEXITCODE; Text = $text; Outputs = $outputs }
}

function Check([string]$name, [bool]$condition, $result) {
    $script:cases++
    if (-not $condition) {
        $script:failures++
        Write-Host "FAIL ${name}: exit=$($result.Exit) text=$($result.Text.Trim()) outputs=$($result.Outputs)"
    }
}

Invoke-Git init -q
$base = Commit-Config @('[A]*', '[B]*') @('src/main.tsx', 'src/test/**')

$head = Commit-Config @('[A]*', '[B]*') @('src/main.tsx', 'src/test/**')
$result = Invoke-Script $base
Check 'unchanged files are silent' ($result.Exit -eq 0 -and $result.Outputs -eq 'exclusions=') $result

$head = Commit-Config @('[A]*', '[B]*', '[C]Foo.*') @('src/main.tsx', 'src/test/**')
$result = Invoke-Script $base
Check 'added coverlet pattern is listed' ($result.Exit -eq 0 -and $result.Outputs -eq 'exclusions=[C]Foo.*') $result

$head = Commit-Config @('[A]*') @('src/main.tsx', 'src/test/**')
$result = Invoke-Script $base
Check 'removed coverlet pattern is silent' ($result.Outputs -eq 'exclusions=') $result

$head = Commit-Config @('[A]*', '[B]*') @('src/main.tsx', 'src/test/**', 'src/legacy/**')
$result = Invoke-Script $base
Check 'added vite exclude is listed' ($result.Outputs -eq 'exclusions=src/legacy/**') $result

$head = Commit-Config @('[A]*', '[B]*', '[C]Foo.*') @('src/main.tsx', 'src/test/**', 'src/legacy/**')
$result = Invoke-Script $base
Check 'both files combined' ($result.Outputs -eq 'exclusions=[C]Foo.*, src/legacy/**') $result

$result = Invoke-Script ''
Check 'empty merge base is silent' ($result.Exit -eq 0 -and $result.Outputs -eq 'exclusions=') $result

Remove-Item $work -Recurse -Force
Write-Host "coverage-exclusions.test.ps1: $cases cases, $failures failures"
if ($failures -gt 0) { exit 1 }
