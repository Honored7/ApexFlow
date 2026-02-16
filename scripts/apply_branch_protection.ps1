param(
    [Parameter(Mandatory = $true)]
    [string]$GitHubToken,

    [Parameter(Mandatory = $true)]
    [string]$Owner,

    [Parameter(Mandatory = $true)]
    [string]$Repo,

    [string[]]$Branches = @("main", "dev"),

    [string[]]$RequiredContexts = @("build")
)

$ErrorActionPreference = "Stop"

function Set-BranchProtection {
    param(
        [string]$Branch
    )

    $uri = "https://api.github.com/repos/$Owner/$Repo/branches/$Branch/protection"

    $body = @{
        required_status_checks = @{
            strict = $true
            contexts = $RequiredContexts
        }
        enforce_admins = $true
        required_pull_request_reviews = @{
            dismissal_restrictions = @{}
            dismiss_stale_reviews = $true
            require_code_owner_reviews = $false
            required_approving_review_count = 1
            require_last_push_approval = $false
        }
        restrictions = $null
        required_linear_history = $false
        allow_force_pushes = $false
        allow_deletions = $false
        block_creations = $false
        required_conversation_resolution = $true
        lock_branch = $false
        allow_fork_syncing = $true
    } | ConvertTo-Json -Depth 10

    $headers = @{
        Authorization = "Bearer $GitHubToken"
        Accept        = "application/vnd.github+json"
        "X-GitHub-Api-Version" = "2022-11-28"
        "User-Agent"  = "ApexFlow-BranchProtection-Script"
    }

    Invoke-RestMethod -Method Put -Uri $uri -Headers $headers -ContentType "application/json" -Body $body | Out-Null
    Write-Output "Branch protection applied: $Branch"
}

foreach ($branch in $Branches) {
    Set-BranchProtection -Branch $branch
}

Write-Output "Done. Protected branches: $($Branches -join ', ')"
