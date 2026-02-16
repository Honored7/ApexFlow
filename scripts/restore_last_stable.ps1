$ErrorActionPreference = "Stop"

if (-not (Test-Path .git)) {
    throw "Not a git repository."
}

$latestTag = git tag --sort=-creatordate | Select-Object -First 1

if (-not $latestTag) {
    throw "No tags found. Create a stable tag first, e.g. git tag -a v0.1.0 -m \"stable\""
}

Write-Output "Restoring to tag: $latestTag"
git reset --hard $latestTag
Write-Output "Restore complete."
