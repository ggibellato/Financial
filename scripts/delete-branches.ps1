[CmdletBinding(SupportsShouldProcess)]
param()

# Cleans up local branches - and the worktrees holding them - once their PR is merged.
#
# git branch -d only detects merges it can trace via commit ancestry, so it
# fails on squash-merged PRs (this repo's default merge strategy) even though
# GitHub already merged them. Ask GitHub for merged-PR status instead.
#
# Worktrees pass that same merged-PR gate, plus one more that branches don't need:
# a worktree is a directory, and it can hold work that exists nowhere else -
# uncommitted edits, or files never added. `git worktree remove` refuses those
# unless forced, and this script never forces. A dirty worktree is skipped and
# reported, and its branch is left in place with it.
#
# Caveat worth knowing: removing a worktree deletes its directory, including
# git-ignored files inside it (a local .env, a data/ folder). Those are invisible
# to the clean check, because `git status` is what defines "clean" here and it
# ignores them by design. Worktrees in this repo are throwaway copies, so that is
# the intended trade - but don't keep the only copy of anything inside one.
#
# Run with -WhatIf to see what would be removed without touching anything.

if (-not (Get-Command gh -ErrorAction SilentlyContinue)) {
    Write-Error "GitHub CLI ('gh') not found. Install it or delete branches manually."
    exit 1
}

function ConvertTo-ComparablePath {
    param([string]$Path)
    if (-not $Path) { return "" }
    return $Path.Replace('\', '/').TrimEnd('/')
}

function Get-Worktrees {
    $entries = @()
    $current = $null

    foreach ($line in (git worktree list --porcelain)) {
        if ($line -match '^worktree (.+)$') {
            if ($current) { $entries += $current }
            $current = [pscustomobject]@{
                Path       = $Matches[1]
                Head       = $null
                Branch     = $null
                Detached   = $false
                Locked     = $false
                LockReason = $null
            }
            continue
        }

        if (-not $current) { continue }

        switch -regex ($line) {
            '^HEAD (.+)$'              { $current.Head = $Matches[1] }
            '^branch refs/heads/(.+)$' { $current.Branch = $Matches[1] }
            '^detached$'               { $current.Detached = $true }
            '^locked ?(.*)$'           { $current.Locked = $true; $current.LockReason = $Matches[1] }
        }
    }

    if ($current) { $entries += $current }
    return , $entries
}

# The gate both branches and worktrees must pass: GitHub says a PR from this
# branch was merged, and the local tip is exactly what got merged - nothing
# committed on top of it. Returns the PR, or $null after explaining why not.
function Get-MergedPr {
    param([string]$Branch, [string]$Label)

    $pr = gh pr list --head $Branch --state merged --json number,headRefOid --jq ".[0]" 2>$null | ConvertFrom-Json

    if (-not $pr) {
        Write-Host "Skipping ${Label}: no merged PR found." -ForegroundColor Yellow
        return $null
    }

    $localSha = git rev-parse $Branch

    if ($localSha -ne $pr.headRefOid) {
        Write-Host "Skipping ${Label}: local commit differs from merged PR #$($pr.number) (branch has unmerged changes)." -ForegroundColor Yellow
        return $null
    }

    return $pr
}

$currentBranch = git rev-parse --abbrev-ref HEAD
$currentRoot = ConvertTo-ComparablePath (git rev-parse --show-toplevel)

git fetch --prune
git worktree prune   # drop admin entries for directories that are already gone

$worktrees = Get-Worktrees
# git worktree list always reports the main worktree first; it holds the real
# repository and must never be removed.
$mainWorktreePath = ConvertTo-ComparablePath $worktrees[0].Path

# --- worktrees first: a branch checked out in a worktree cannot be deleted ---
$handledByWorktreeLoop = @{}

foreach ($worktree in $worktrees) {
    $path = ConvertTo-ComparablePath $worktree.Path

    if ($path -eq $mainWorktreePath) { continue }

    if ($path -eq $currentRoot) {
        Write-Host "Skipping worktree '$path': it is the working directory this script is running in." -ForegroundColor Yellow
        continue
    }

    if ($worktree.Locked) {
        $reason = if ($worktree.LockReason) { " ($($worktree.LockReason))" } else { "" }
        Write-Host "Skipping worktree '$path': locked$reason." -ForegroundColor Yellow
        continue
    }

    if ($worktree.Detached -or -not $worktree.Branch) {
        Write-Host "Skipping worktree '$path': detached HEAD, so there is no branch whose merge status can be checked." -ForegroundColor Yellow
        continue
    }

    $branch = $worktree.Branch
    if ($branch -eq "main" -or $branch -eq $currentBranch) { continue }

    $pr = Get-MergedPr -Branch $branch -Label "worktree '$path'"
    if (-not $pr) { continue }

    # A worktree can sit on a stale checkout of an otherwise-merged branch.
    if ($worktree.Head -ne $pr.headRefOid) {
        Write-Host "Skipping worktree '$path': its checkout is not at the merged tip of '$branch'." -ForegroundColor Yellow
        continue
    }

    # The same rule `git worktree remove` applies, checked up front so a refusal
    # reads as a message rather than a git error - and so the branch survives too.
    $dirty = @(git -C $worktree.Path status --porcelain)
    if ($dirty.Count -gt 0) {
        Write-Host "Skipping worktree '$path': $($dirty.Count) uncommitted or untracked file(s) present. Commit, stash or delete them first." -ForegroundColor Yellow
        continue
    }

    # Recorded before ShouldProcess so that -WhatIf doesn't then report this same
    # branch as "still checked out" in the loop below - under -WhatIf the worktree
    # is still there, but the preview should read as one decision, not two.
    $handledByWorktreeLoop[$branch] = $true

    if ($PSCmdlet.ShouldProcess($path, "Remove worktree and delete branch '$branch' (merged via PR #$($pr.number))")) {
        Write-Host "Removing worktree '$path' (merged via PR #$($pr.number))..."
        git worktree remove $worktree.Path

        if ($LASTEXITCODE -ne 0) {
            Write-Host "Failed to remove worktree '$path'; leaving branch '$branch' in place." -ForegroundColor Red
            continue
        }

        Write-Host "Deleting branch '$branch' (merged via PR #$($pr.number))..."
        git branch -D $branch
    }
}

# A branch still held by a worktree can't be deleted; say so rather than letting
# git fail. Re-read the list, because the loop above removed some.
$heldByWorktree = @{}
foreach ($worktree in (Get-Worktrees)) {
    if ($worktree.Branch) { $heldByWorktree[$worktree.Branch] = ConvertTo-ComparablePath $worktree.Path }
}

# --- then the branches with no worktree of their own ---
foreach ($branch in (git branch --format="%(refname:short)")) {
    if ($branch -eq "main" -or $branch -eq $currentBranch) {
        continue
    }

    if ($handledByWorktreeLoop.ContainsKey($branch)) {
        continue
    }

    if ($heldByWorktree.ContainsKey($branch)) {
        Write-Host "Skipping branch '$branch': still checked out in worktree '$($heldByWorktree[$branch])'." -ForegroundColor Yellow
        continue
    }

    $pr = Get-MergedPr -Branch $branch -Label "branch '$branch'"
    if (-not $pr) { continue }

    if ($PSCmdlet.ShouldProcess($branch, "Delete branch (merged via PR #$($pr.number))")) {
        Write-Host "Deleting branch '$branch' (merged via PR #$($pr.number))..."
        git branch -D $branch
    }
}
