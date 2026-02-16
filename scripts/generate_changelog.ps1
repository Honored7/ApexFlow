param(
    [Parameter(Mandatory = $true)]
    [string]$Version,
    [string]$OutputPath = "CHANGELOG.md",
    [string]$FromTag = "",
    [string]$ToRef = "HEAD"
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path .git)) {
    throw "Not a git repository."
}

if ([string]::IsNullOrWhiteSpace($FromTag)) {
    $tags = git tag --sort=-creatordate
    if ($LASTEXITCODE -ne 0) { throw "Unable to list tags." }

    $parsedTags = @($tags | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
    if ($parsedTags.Count -gt 0) {
        $FromTag = $parsedTags[0].Trim()
    }
}

$range = if ([string]::IsNullOrWhiteSpace($FromTag)) { $ToRef } else { "$FromTag..$ToRef" }
$commitLines = git log $range --no-merges --pretty=format:"- %s (%h)"
if ($LASTEXITCODE -ne 0) { throw "Unable to read git log for range $range" }

$entryDate = Get-Date -Format "yyyy-MM-dd"
$header = "## v$Version - $entryDate"

$lines = @()
$lines += $header
$lines += ""
if ($commitLines -and $commitLines.Count -gt 0) {
    $lines += $commitLines
} else {
    $lines += "- No user-facing changes in this release"
}
$lines += ""

if (Test-Path $OutputPath) {
    $existing = Get-Content $OutputPath
    $insertIndex = 0
    if ($existing.Count -gt 0 -and $existing[0] -like "# Changelog*") {
        $insertIndex = 2
    }

    $updated = @()
    if ($insertIndex -gt 0) {
        $updated += $existing[0]
        $updated += $existing[1]
    }
    $updated += $lines
    if ($insertIndex -gt 0) {
        $updated += $existing | Select-Object -Skip 2
    } else {
        $updated += $existing
    }

    Set-Content -Path $OutputPath -Value $updated -Encoding UTF8
} else {
    $initial = @("# Changelog", "", "All notable changes to this project are documented in this file.", "")
    $initial += $lines
    Set-Content -Path $OutputPath -Value $initial -Encoding UTF8
}

Write-Output "Changelog entry generated for v$Version using range: $range"
