param(
    [Parameter(Mandatory = $true)]
    [string]$Version,
    [string]$TargetBranch = "main",
    [switch]$Push,
    [switch]$SkipChangelogCommit
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path .git)) {
    throw "Not a git repository."
}

$status = git status --porcelain
if ($status) {
    throw "Working tree is not clean. Commit or stash changes first."
}

pwsh -ExecutionPolicy Bypass -File .\scripts\generate_changelog.ps1 -Version $Version

if (-not $SkipChangelogCommit) {
    git add CHANGELOG.md
    git commit -m "docs(changelog): release v$Version"
}

$tagName = "v$Version"
$existingTag = git tag --list $tagName
if ($existingTag) {
    throw "Tag $tagName already exists."
}

git tag -a $tagName -m "release $tagName"

if ($Push) {
    git push origin $TargetBranch
    git push origin $tagName
}

Write-Output "Release prepared: $tagName"
if ($Push) {
    Write-Output "Pushed branch '$TargetBranch' and tag '$tagName'."
}
