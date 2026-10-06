param(
    [string]$MergeBase = '',
    [string]$Head = 'HEAD'
)

$ErrorActionPreference = 'Stop'

function Add-Output([string]$name, [string]$value) {
    if ($env:GITHUB_OUTPUT) { "$name=$value" | Out-File -Append -Encoding utf8 $env:GITHUB_OUTPUT }
}

function Get-FileAt([string]$revision, [string]$path) {
    $content = & git show "${revision}:$path" 2>$null
    if ($LASTEXITCODE -ne 0) { return '' }
    return ($content -join "`n")
}

function Get-CoverletExcludes([string]$text) {
    if ($text -notmatch '(?s)<Exclude>(.*?)</Exclude>') { return @() }
    return @($Matches[1] -split ',' | ForEach-Object { $_.Trim() } | Where-Object { $_ })
}

function Get-ViteExcludes([string]$text) {
    if ($text -notmatch '(?s)coverage:\s*\{.*?exclude:\s*\[(.*?)\]') { return @() }
    return @([regex]::Matches($Matches[1], "'([^']+)'") | ForEach-Object { $_.Groups[1].Value })
}

$sources = @(
    @{ Path = 'coverlet.runsettings'; Parse = ${function:Get-CoverletExcludes} },
    @{ Path = 'Financial.Web/vite.config.ts'; Parse = ${function:Get-ViteExcludes} }
)

$added = @()
if ($MergeBase) {
    foreach ($source in $sources) {
        $before = @(& $source.Parse (Get-FileAt $MergeBase $source.Path))
        $after = @(& $source.Parse (Get-FileAt $Head $source.Path))
        $added += @($after | Where-Object { $_ -notin $before })
    }
}

$list = $added -join ', '
Add-Output 'exclusions' $list
if ($list) { Write-Host "Coverage exclusions added: $list" }
