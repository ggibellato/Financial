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
$succeeded = $false
$exitCode = 1
$readinessTimeoutSeconds = 60
$readinessPollMilliseconds = 500
$runDate = (Get-Date).ToString('yyyy-MM-dd')

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
        $callerApiBaseUrl = $env:API_BASE_URL
        $env:API_BASE_URL = '/api/v1/financial'
        Push-Location $webFolder
        try {
            if (-not (Test-Path -LiteralPath (Join-Path $webFolder 'node_modules'))) { Invoke-Logged $log $failure { npm ci } }
            Invoke-Logged $log $failure { npm run build }
        }
        finally {
            Pop-Location
            $env:API_BASE_URL = $callerApiBaseUrl
        }
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
        ASPNETCORE_ENVIRONMENT           = 'Parity'
        ASPNETCORE_URLS                  = "http://localhost:$Port"
        CashFlow__Repository__Provider   = 'LocalJson'
        CashFlow__DataJsonFile           = Join-Path $SideFolder 'data-cashflow.json'
        Investment__Repository__Provider = 'LocalJson'
        Investment__DataJsonFile         = Join-Path $SideFolder 'data-investment.json'
        FxRates__Repository__Provider    = 'LocalJson'
        FxRates__DataJsonFile            = Join-Path $SideFolder 'data-fx-rates.json'
        Observability__Enabled           = 'false'
    }
    $process = Start-Process -FilePath 'dotnet' -ArgumentList 'Financial.Api.dll' -WorkingDirectory $AppFolder -Environment $environment `
        -RedirectStandardOutput $log -RedirectStandardError (Join-Path $SideFolder 'api.err.log') -NoNewWindow -PassThru
    $startedProcesses.Add($process)

    $baseUrl = "http://localhost:$Port/api/v1/financial"
    $deadline = (Get-Date).AddSeconds($readinessTimeoutSeconds)
    while ((Get-Date) -lt $deadline -and -not $process.HasExited) {
        try {
            $health = Invoke-RestMethod -Uri "$baseUrl/health" -TimeoutSec 5
            if ($health.contexts.cashFlow.provider -eq 'LocalJson' -and $health.contexts.investment.provider -eq 'LocalJson') {
                return [pscustomobject]@{ Process = $process; BaseUrl = $baseUrl }
            }
            break
        }
        catch { Start-Sleep -Milliseconds $readinessPollMilliseconds }
    }
    throw "$Side API on port $Port did not become ready (or is not LocalJson); see $log"
}

function Stop-ParityApi([System.Diagnostics.Process]$Process) {
    if (-not $Process.HasExited) { Stop-Process -Id $Process.Id -Force }
    $Process.WaitForExit()
    [void]$startedProcesses.Remove($Process)
}

function New-ParitySideFolder([string]$Folder, [string]$SideKey) {
    $sideFolder = Join-Path $Folder $SideKey
    New-Item -ItemType Directory -Path $sideFolder -Force | Out-Null
    $sideFolder
}

function New-ParitySideCopy([string]$Folder, [string]$SideKey, [string]$LiveCopy) {
    $sideFolder = New-ParitySideFolder $Folder $SideKey
    Copy-Item -LiteralPath $LiveCopy -Destination (Join-Path $sideFolder 'data-cashflow.json') -Force
    $sideFolder
}

function Export-ParitySide([string]$Side, [string]$Folder, [string]$AppFolder, [int]$Port, $RunInfo) {
    $sideKey = $Side.ToLowerInvariant()
    $sideFolder = Join-Path $Folder $sideKey
    $dataFile = Join-Path $sideFolder 'data-cashflow.json'
    $api = Start-ParityApi $Side $sideFolder $AppFolder $Port
    $client = [System.Net.Http.HttpClient]::new()
    try {
        $hashBeforeRead = Get-ParitySha256 $dataFile
        $baseUrl = $api.BaseUrl
        $fetch = { param($path) ConvertFrom-ParityJson $client.GetStringAsync("$baseUrl/$path").GetAwaiter().GetResult() }.GetNewClosure()
        $report = Get-ParityReport -Fetch $fetch -First $RunInfo.range.first -Last $RunInfo.range.last -Side $sideKey -IncludeSummary:($sideKey -eq 'after')
    }
    finally { $client.Dispose() }
    Set-Content -LiteralPath (Join-Path $Folder "$sideKey-report.json") -Value (ConvertTo-ParityReportJson $report) -NoNewline -Encoding utf8
    $RunInfo[$sideKey] = [ordered]@{
        copyChangedDuringRead = (Get-ParitySha256 $dataFile) -ne $hashBeforeRead
        figures               = $report.Count
    }
    if ($KeepRunning) { Write-Host "$Side running at http://localhost:$Port" }
    else { Stop-ParityApi $api.Process }
}

function Invoke-BaselineStep([string]$Folder, $RunInfo) {
    Write-Host "[1/4] Baseline: building $BaselineTag..."
    $sideFolder = New-ParitySideCopy $Folder 'before' $RunInfo.liveCopy
    $log = Join-Path $sideFolder 'build.log'
    $worktree = Join-Path $Folder 'baseline-src'
    try {
        Invoke-Logged $log "Before build failed; see $log" { git -C $RepoRoot worktree add --detach $worktree $BaselineTag }
        $appFolder = Publish-ParityApi 'Before' $worktree $sideFolder
    }
    finally {
        git -C $RepoRoot worktree remove --force $worktree *>> $log
        if (Test-Path -LiteralPath $worktree) { Remove-Item -LiteralPath $worktree -Recurse -Force -ErrorAction SilentlyContinue }
        git -C $RepoRoot worktree prune *>> $log
    }
    $RunInfo.baseline = [ordered]@{ tag = $BaselineTag; commit = (git -C $RepoRoot rev-parse "$BaselineTag^{commit}") }
    Export-ParitySide 'Before' $Folder $appFolder $BeforePort $RunInfo
}

function Invoke-MigrateStep([string]$Folder, $RunInfo) {
    Write-Host '[2/4] Migrate...'
    $sideFolder = New-ParitySideFolder $Folder 'after'
    $log = Join-Path $sideFolder 'migrate.log'
    $tool = Join-Path $RepoRoot 'Tools/CashFlowSpreadsheetImport/CashFlowSpreadsheetImport.csproj'
    $output = Join-Path $sideFolder 'data-cashflow.json'
    Invoke-Logged $log "Migrate failed; see $log" { dotnet run --project $tool -c Release -- migrate --input $RunInfo.liveCopy --output $output }
}

function Invoke-AfterStep([string]$Folder, $RunInfo) {
    Write-Host '[3/4] After: building the working tree...'
    $sideFolder = Join-Path $Folder 'after'
    if (-not (Test-Path -LiteralPath (Join-Path $sideFolder 'data-cashflow.json'))) { throw "No migrated copy in $Folder. Run -Step Migrate first." }
    if ($RunInfo.runDate -ne $runDate) { throw "The before report was taken on $($RunInfo.runDate) and the API reads today's date; rerun -Step All." }
    $RunInfo.afterBuild = [ordered]@{
        commit = (git -C $RepoRoot rev-parse HEAD)
        dirty  = [bool](git -C $RepoRoot status --porcelain)
    }
    $appFolder = Publish-ParityApi 'After' $RepoRoot $sideFolder
    Export-ParitySide 'After' $Folder $appFolder $AfterPort $RunInfo
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
        $gitCommonDir = if (Get-Command git -ErrorAction SilentlyContinue) { git -C $RepoRoot rev-parse --path-format=absolute --git-common-dir 2> $null }
        $repoRoots = @($RepoRoot) + @(if ($gitCommonDir) { Split-Path -Parent $gitCommonDir })
        $liveCopyPath = Assert-ParityLiveCopy $LiveCopy $repoRoots
        Assert-ParityPorts @(switch ($Step) { 'All' { $BeforePort, $AfterPort } 'Baseline' { $BeforePort } 'After' { $AfterPort } })
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
            $runInfo.runDate = $runDate
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
    Save-RunInfo $folder $runInfo

    if ($KeepRunning -and $startedProcesses.Count -gt 0) {
        Write-Host "Stop them with: Stop-Process -Id $(($startedProcesses | ForEach-Object Id) -join ',')"
    }
    $succeeded = $true
    $exitCode = if ($passed) { 0 } else { 1 }
}
catch {
    Write-Host $_.Exception.Message -ForegroundColor Red
}
finally {
    if (-not ($succeeded -and $KeepRunning)) {
        foreach ($process in @($startedProcesses)) { Stop-ParityApi $process }
    }
}
exit $exitCode
