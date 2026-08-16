# git branch -d only detects merges it can trace via commit ancestry, so it
# fails on squash-merged PRs (this repo's default merge strategy) even though
# GitHub already merged them. Ask GitHub for merged-PR status instead.

if (-not (Get-Command gh -ErrorAction SilentlyContinue)) {
    Write-Error "GitHub CLI ('gh') not found. Install it or delete branches manually."
    exit 1
}

$currentBranch = git rev-parse --abbrev-ref HEAD
git fetch --prune

foreach ($branch in (git branch --format="%(refname:short)")) {
    if ($branch -eq "main" -or $branch -eq $currentBranch) {
        continue
    }

    $pr = gh pr list --head $branch --state merged --json number,headRefOid --jq ".[0]" 2>$null | ConvertFrom-Json

    if (-not $pr) {
        Write-Host "Skipping branch '$branch': no merged PR found." -ForegroundColor Yellow
        continue
    }

    $localSha = git rev-parse $branch

    if ($localSha -ne $pr.headRefOid) {
        Write-Host "Skipping branch '$branch': local commit differs from merged PR #$($pr.number) (branch has unmerged changes)." -ForegroundColor Yellow
        continue
    }

    Write-Host "Deleting branch '$branch' (merged via PR #$($pr.number))..."
    git branch -D $branch
}