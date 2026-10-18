param(
    [Parameter(Mandatory)][ValidateSet('Baseline', 'Migrate', 'After', 'Compare', 'All')][string]$Step,
    [string]$LiveCopy,
    [string]$WorkFolder,
    [int]$BeforePort = 5291,
    [int]$AfterPort = 5292,
    [string]$BaselineTag = 'p60-baseline',
    [switch]$KeepRunning,
    [string]$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
)

$ErrorActionPreference = 'Stop'
Import-Module (Join-Path $PSScriptRoot 'Parity.psm1') -Force

$parityRoot = Join-Path ([IO.Path]::GetTempPath()) 'financial-parity'
$startedProcesses = [System.Collections.Generic.List[System.Diagnostics.Process]]::new()
$worktreePath = $null
$succeeded = $false
$exitCode = 1

function Read-RunInfo([string]$Folder) {
    $path = Join-Path $Folder 'run.json'
    if (Test-Path -LiteralPath $path) { return Get-Content -LiteralPath $path -Raw | ConvertFrom-Json -AsHashtable }
    [ordered]@{}
}

function Save-RunInfo([string]$Folder, $RunInfo) {
    $RunInfo | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $Folder 'run.json') -Encoding utf8
}

function Resolve-WorkFolder {
    if ($WorkFolder) {
        New-Item -ItemType Directory -Path $WorkFolder -Force | Out-Null
        return (Resolve-Path $WorkFolder).Path
    }
    if ($Step -in 'All', 'Baseline') {
        $folder = Join-Path $parityRoot (Get-Date -Format 'yyyyMMdd-HHmmss')
        New-Item -ItemType Directory -Path $folder -Force | Out-Null
        return $folder
    }
    $latest = Get-ChildItem -LiteralPath $parityRoot -Directory -ErrorAction SilentlyContinue | Sort-Object Name | Select-Object -Last 1
    if (-not $latest) { throw "No work folder found under $parityRoot. Run -Step Baseline or -Step All first, or pass -WorkFolder." }
    $latest.FullName
}

function Invoke-Logged([string]$LogPath, [string]$FailureMessage, [scriptblock]$Command) {
    & $Command *>> $LogPath
    if ($LASTEXITCODE -ne 0) { throw $FailureMessage }
}

function Publish-ParityApi([string]$Side, [string]$SourceRoot, [string]$SideFolder) {
    $appFolder = Join-Path $SideFolder 'app'
    $log = Join-Path $SideFolder 'build.log'
    $failure = "$Side build failed; see $log"
    Invoke-Logged $log $failure { dotnet publish (Join-Path $SourceRoot 'Financial.Api/Financial.Api.csproj') --configuration Release --output $appFolder }
    if ($KeepRunning) {
        $webFolder = Join-Path $SourceRoot 'Financial.Web'
        $env:API_BASE_URL = '/api/v1/financial'
        Push-Location $webFolder
        try {
            if (-not (Test-Path -LiteralPath (Join-Path $webFolder 'node_modules'))) { Invoke-Logged $log $failure { npm ci } }
            Invoke-Logged $log $failure { npm run build }
        }
        finally { Pop-Location }
        $wwwroot = Join-Path $appFolder 'wwwroot'
        New-Item -ItemType Directory -Path $wwwroot -Force | Out-Null
        Copy-Item -Path (Join-Path $webFolder 'dist/*') -Destination $wwwroot -Recurse -Force
    }
    $appFolder
}

function Start-ParityApi([string]$Side, [string]$SideFolder, [string]$AppFolder, [int]$Port) {
    Copy-Item -LiteralPath (Join-Path $RepoRoot 'data/data-investment.example.json') -Destination (Join-Path $SideFolder 'data-investment.json') -Force
    Copy-Item -LiteralPath (Join-Path $RepoRoot 'data/data-fx-rates.example.json') -Destination (Join-Path $SideFolder 'data-fx-rates.json') -Force
    $log = Join-Path $SideFolder 'api.log'
    $environment = @{
        ASPNETCORE_ENVIRONMENT          = 'Parity'
        ASPNETCORE_URLS                 = "http://localhost:$Port"
        CashFlow__Repository__Provider  = 'LocalJson'
        CashFlow__DataJsonFile          = Join-Path $SideFolder 'data-cashflow.json'
        Investment__Repository__Provider = 'LocalJson'
        Investment__DataJsonFile        = Join-Path $SideFolder 'data-investment.json'
        FxRates__Repository__Provider   = 'LocalJson'
        FxRates__DataJsonFile           = Join-Path $SideFolder 'data-fx-rates.json'
        Observability__Enabled          = 'false'
    }
    $process = Start-Process -FilePath 'dotnet' -ArgumentList 'Financial.Api.dll' -WorkingDirectory $AppFolder -Environment $environment `
        -RedirectStandardOutput $log -RedirectStandardError (Join-Path $SideFolder 'api.err.log') -NoNewWindow -PassThru
    $startedProcesses.Add($process)

    $baseUrl = "http://localhost:$Port/api/v1/financial"
    $deadline = (Get-Date).AddSeconds(60)
    while ((Get-Date) -lt $deadline -and -not $process.HasExited) {
        try {
            $health = Invoke-RestMethod -Uri "$baseUrl/health" -TimeoutSec 5
            if ($health.contexts.cashFlow.provider -eq 'LocalJson' -and $health.contexts.investment.provider -eq 'LocalJson') { return $baseUrl }
            break
        }
        catch { Start-Sleep -Milliseconds 500 }
    }
    throw "$Side API on port $Port did not become ready (or is not LocalJson); see $log"
}

function Stop-ParityApi([System.Diagnostics.Process]$Process) {
    if (-not $Process.HasExited) { Stop-Process -Id $Process.Id -Force }
    $Process.WaitForExit()
    [void]$startedProcesses.Remove($Process)
}

function Export-ParitySide([string]$Side, [string]$Folder, [string]$SourceRoot, [int]$Port, $RunInfo) {
    $sideFolder = Join-Path $Folder $Side.ToLowerInvariant()
    $dataFile = Join-Path $sideFolder 'data-cashflow.json'
    $appFolder = Publish-ParityApi $Side $SourceRoot $sideFolder
    if ($script:worktreePath) {
        git -C $RepoRoot worktree remove --force $script:worktreePath *>> (Join-Path $sideFolder 'build.log')
        $script:worktreePath = $null
    }
    $baseUrl = Start-ParityApi $Side $sideFolder $appFolder $Port
    $hashBeforeRead = Get-ParitySha256 $dataFile
    $fetch = { param($path) ConvertFrom-ParityJson (Invoke-WebRequest -Uri "$baseUrl/$path" -TimeoutSec 60).Content }.GetNewClosure()
    $report = Get-ParityReport -Fetch $fetch -First $RunInfo.range.first -Last $RunInfo.range.last -Side $Side.ToLowerInvariant()
    Set-Content -LiteralPath (Join-Path $Folder "$($Side.ToLowerInvariant())-report.json") -Value (ConvertTo-ParityReportJson $report) -NoNewline -Encoding utf8
    $RunInfo[$Side.ToLowerInvariant()] = [ordered]@{
        copyChangedDuringRead = (Get-ParitySha256 $dataFile) -ne $hashBeforeRead
        figures               = $report.Count
        url                   = "http://localhost:$Port"
    }
    if (-not $KeepRunning) { Stop-ParityApi $startedProcesses[$startedProcesses.Count - 1] }
}

function Invoke-BaselineStep([string]$Folder, $RunInfo) {
    Write-Host "[1/4] Baseline: building $BaselineTag..."
    $sideFolder = Join-Path $Folder 'before'
    New-Item -ItemType Directory -Path $sideFolder -Force | Out-Null
    Copy-Item -LiteralPath $RunInfo.liveCopy -Destination (Join-Path $sideFolder 'data-cashflow.json') -Force
    $script:worktreePath = Join-Path $Folder 'baseline-src'
    Invoke-Logged (Join-Path $sideFolder 'build.log') "Before build failed; see $(Join-Path $sideFolder 'build.log')" {
        git -C $RepoRoot worktree add --detach $script:worktreePath $BaselineTag
    }
    $RunInfo.baseline = [ordered]@{ tag = $BaselineTag; commit = (git -C $RepoRoot rev-parse "$BaselineTag^{commit}") }
    Export-ParitySide 'Before' $Folder $script:worktreePath $BeforePort $RunInfo
}

function Invoke-MigrateStep([string]$Folder, $RunInfo) {
    Write-Host '[2/4] Migrate...'
    $sideFolder = Join-Path $Folder 'after'
    New-Item -ItemType Directory -Path $sideFolder -Force | Out-Null
    Copy-Item -LiteralPath $RunInfo.liveCopy -Destination (Join-Path $sideFolder 'data-cashflow.json') -Force
}

function Invoke-AfterStep([string]$Folder, $RunInfo) {
    Write-Host '[3/4] After: building the working tree...'
    if (-not (Test-Path -LiteralPath (Join-Path $Folder 'after/data-cashflow.json'))) { throw "No migrated copy in $Folder. Run -Step Migrate first." }
    $RunInfo.afterBuild = [ordered]@{
        commit = (git -C $RepoRoot rev-parse HEAD)
        dirty  = [bool](git -C $RepoRoot status --porcelain)
    }
    Export-ParitySide 'After' $Folder $RepoRoot $AfterPort $RunInfo
}

function Invoke-CompareStep([string]$Folder, $RunInfo) {
    Write-Host '[4/4] Compare...'
    $before = Read-ParityReport (Join-Path $Folder 'before-report.json')
    $after = Read-ParityReport (Join-Path $Folder 'after-report.json')
    $differences = Compare-ParityReport $before $after (Read-ParityFigureMap (Join-Path $PSScriptRoot 'figure-map.json'))
    $lines = @($differences | ForEach-Object { Format-ParityDifference $_ })
    $lines | ForEach-Object { Write-Host $_ }
    if ($lines.Count -gt 0) { Set-Content -LiteralPath (Join-Path $Folder 'differences.txt') -Value $lines -Encoding utf8 }
    $RunInfo.figures = $before.Count
    $RunInfo.differences = $lines.Count
    $summary = Format-ParitySummary $lines.Count $before.Count (Get-ParityMonthCount $before)
    Write-Host $summary -ForegroundColor $(if ($lines.Count -eq 0) { 'Green' } else { 'Red' })
    $lines.Count -eq 0
}

try {
    if ($Step -ne 'Compare') {
        if (-not $LiveCopy) { throw "-LiveCopy is required for -Step $Step." }
        $liveCopyPath = Assert-ParityLiveCopy $LiveCopy $RepoRoot
        $ports = switch ($Step) { 'All' { $BeforePort, $AfterPort } 'Baseline' { $BeforePort } 'After' { $AfterPort } default { @() } }
        if (@($ports).Count -gt 0) { Assert-ParityPorts @($ports) }
        if ($Step -in 'All', 'Baseline') {
            git -C $RepoRoot rev-parse --verify --quiet "$BaselineTag^{commit}" *> $null
            if ($LASTEXITCODE -ne 0) { throw "Tag $BaselineTag not found. Create it on main before the first run." }
        }
    }

    $folder = Resolve-WorkFolder
    Write-Host "Work folder: $folder"
    $runInfo = Read-RunInfo $folder

    if ($Step -ne 'Compare') {
        if ($Step -in 'All', 'Baseline' -or -not $runInfo.Contains('range')) {
            $runInfo.liveCopy = $liveCopyPath
            $runInfo.liveCopySha256Start = Get-ParitySha256 $liveCopyPath
            $range = Get-ParityMonthRange $liveCopyPath (Get-Date)
            $runInfo.range = [ordered]@{ first = $range.First; last = $range.Last; currentMonth = $range.CurrentMonth }
            Save-RunInfo $folder $runInfo
        }
        elseif ($runInfo.liveCopy -ne $liveCopyPath) {
            throw "This work folder was started with $($runInfo.liveCopy); pass the same -LiveCopy or start a new run."
        }
    }

    $passed = $true
    if ($Step -in 'All', 'Baseline') { Invoke-BaselineStep $folder $runInfo }
    if ($Step -in 'All', 'Migrate') { Invoke-MigrateStep $folder $runInfo }
    if ($Step -in 'All', 'After') { Invoke-AfterStep $folder $runInfo }
    if ($Step -ne 'Compare') {
        Assert-ParityHashUnchanged $runInfo.liveCopy $runInfo.liveCopySha256Start
        $runInfo.liveCopySha256End = Get-ParitySha256 $runInfo.liveCopy
    }
    if ($Step -in 'All', 'Compare') { $passed = Invoke-CompareStep $folder $runInfo }
    if ($runInfo.Count -gt 0) { Save-RunInfo $folder $runInfo }

    if ($KeepRunning -and $startedProcesses.Count -gt 0) {
        foreach ($side in 'before', 'after') {
            if ($runInfo.Contains($side) -and $runInfo[$side].Contains('url')) { Write-Host "$side`: $($runInfo[$side].url)" }
        }
        Write-Host "Stop them with: Stop-Process -Id $(($startedProcesses | ForEach-Object Id) -join ',')"
    }
    $succeeded = $true
    $exitCode = if ($passed) { 0 } else { 1 }
}
catch {
    Write-Host $_.Exception.Message -ForegroundColor Red
    $exitCode = 1
}
finally {
    if (-not ($succeeded -and $KeepRunning)) {
        foreach ($process in @($startedProcesses)) { Stop-ParityApi $process }
    }
    if ($worktreePath) {
        git -C $RepoRoot worktree remove --force $worktreePath *> $null
        if (Test-Path -LiteralPath $worktreePath) { Remove-Item -LiteralPath $worktreePath -Recurse -Force -ErrorAction SilentlyContinue }
        git -C $RepoRoot worktree prune *> $null
    }
}
exit $exitCode
