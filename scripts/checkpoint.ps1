param(
    [string]$Message = "checkpoint"
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path .git)) {
    throw "Not a git repository. Run scripts/setup_git_recovery.ps1 first."
}

$timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
$commitMessage = "checkpoint: $Message [$timestamp]"

$status = git status --porcelain
if (-not $status) {
    Write-Output "No changes to checkpoint."
    exit 0
}

git add .
git commit -m $commitMessage
Write-Output "Checkpoint committed: $commitMessage"
