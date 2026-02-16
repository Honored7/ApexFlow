<#
.SYNOPSIS
    Copies latest ApexFlow backtest results into the repo and pushes to GitHub.
.DESCRIPTION
    Run this after a cTrader backtest completes. It copies the latest CSV and
    JSON result files from ~/Documents/ApexFlow/Results/ into the repo's
    results/ folder, commits them, and pushes to origin/main.
.EXAMPLE
    .\scripts\push-results.ps1
    .\scripts\push-results.ps1 -Count 2 -Message "EURUSD M15 backtest"
#>

param(
    [int]$Count = 1,
    [string]$Message = ""
)

$ErrorActionPreference = 'Stop'

$resultsSource = Join-Path $env:USERPROFILE "Documents\ApexFlow\Results"
$repoDir = Split-Path -Parent $PSScriptRoot
$resultsTarget = Join-Path $repoDir "results"

# Validate source exists
if (-not (Test-Path $resultsSource)) {
    Write-Host "[!] No results found at $resultsSource" -ForegroundColor Yellow
    Write-Host "    Run a backtest first — the bot exports results on stop."
    exit 1
}

# Create target directory
if (-not (Test-Path $resultsTarget)) {
    New-Item -ItemType Directory -Path $resultsTarget -Force | Out-Null
    Write-Host "[+] Created $resultsTarget"
}

# Copy latest JSON summaries (always keep these in git — small & useful)
$jsonFiles = Get-ChildItem $resultsSource -Filter "summary_*.json" |
    Sort-Object LastWriteTime -Descending |
    Select-Object -First $Count

# Copy latest CSV trade logs
$csvFiles = Get-ChildItem $resultsSource -Filter "trades_*.csv" |
    Sort-Object LastWriteTime -Descending |
    Select-Object -First $Count

$copied = @()

foreach ($file in @($jsonFiles) + @($csvFiles)) {
    if ($null -ne $file) {
        Copy-Item $file.FullName -Destination $resultsTarget -Force
        $copied += $file.Name
        Write-Host "[+] Copied $($file.Name)"
    }
}

if ($copied.Count -eq 0) {
    Write-Host "[!] No result files found to copy." -ForegroundColor Yellow
    exit 1
}

# Git operations
Push-Location $repoDir
try {
    git add results/

    $commitMsg = if ($Message) {
        "results: $Message"
    } else {
        "results: backtest $(Get-Date -Format 'yyyy-MM-dd HH:mm')"
    }

    git commit -m $commitMsg
    git push origin main

    Write-Host ""
    Write-Host "[OK] $($copied.Count) result file(s) pushed to GitHub." -ForegroundColor Green
    Write-Host "     $($jsonFiles.Count) summary JSON + $($csvFiles.Count) trade CSV"
} finally {
    Pop-Location
}
