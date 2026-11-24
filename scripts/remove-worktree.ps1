[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$Topic,
    [switch]$KeepBranch
)

$ErrorActionPreference = 'Stop'

$mainRoot = (git -C $PSScriptRoot worktree list --porcelain | Select-Object -First 1) -replace '^worktree ', ''
$worktree = Join-Path (Split-Path -Parent $mainRoot) "financial-wt-$Topic"

if (-not (Test-Path $worktree)) { throw "$worktree does not exist." }

$branch = git -C $worktree branch --show-current

git -C $mainRoot worktree remove $worktree
if ($LASTEXITCODE) { throw "git worktree remove failed; commit or discard the changes in $worktree first." }
Write-Host "Removed $worktree"

if (-not $KeepBranch -and $branch) {
    git -C $mainRoot fetch origin
    git -C $mainRoot merge-base --is-ancestor $branch origin/main
    if ($LASTEXITCODE) {
        Write-Warning "Kept branch ${branch}: it is not merged into origin/main. Delete it with 'git branch -D $branch' if that is intended."
    }
    else {
        git -C $mainRoot branch -D $branch
    }
}
