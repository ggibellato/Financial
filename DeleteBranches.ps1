foreach ($branch in (git branch --format="%(refname:short)")) {
    if ($branch -ne "main") {
        Write-Host "Deleting branch '$branch'..."
        git branch -d $branch
    }
}