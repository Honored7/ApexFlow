param(
    [string]$MainBranch = "main",
    [string]$DevBranch = "dev"
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path .git)) {
    git init | Out-Null
}

# Ensure identity exists
$hasName = git config --get user.name
$hasEmail = git config --get user.email

if (-not $hasName) {
    git config user.name "ApexFlow Local"
}

if (-not $hasEmail) {
    git config user.email "apexflow@local"
}

# Initial commit if needed
$status = git status --porcelain
if ($status) {
    git add .
    git commit -m "chore: initial import" | Out-Null
}

$currentBranch = (git branch --show-current).Trim()
if (-not $currentBranch) {
    git checkout -b $MainBranch | Out-Null
}
elseif ($currentBranch -ne $MainBranch) {
    git branch -M $MainBranch | Out-Null
}

$branches = git branch --format='%(refname:short)'
if ($branches -notcontains $DevBranch) {
    git checkout -b $DevBranch | Out-Null
    git checkout $MainBranch | Out-Null
}

# Install simple post-commit checkpoint note hook
$hookPath = Join-Path (Join-Path $PWD ".git") "hooks\post-commit"
$hookContent = @"
#!/bin/sh
echo "[post-commit] checkpoint created at $(date)" >> .git/checkpoints.log
"@
Set-Content -Path $hookPath -Value $hookContent -Encoding ascii

Write-Output "Git recovery setup complete. Branches: $MainBranch, $DevBranch"
