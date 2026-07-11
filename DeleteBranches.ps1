foreach ($branch in (git branch --format="%(refname:short)")) {
    if ($branch -ne "main") {
        $commits = git log main..$branch --oneline

        if (-not $commits) {
            Write-Host "Deleting branch '$branch'..."
            git branch -d $branch
        }
        else {
            Write-Host "Keeping branch '$branch' - unmerged commits exist"
        }
    }
}