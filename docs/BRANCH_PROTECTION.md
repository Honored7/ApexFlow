# Branch Protection Policy

Repository: `Honored7/ApexFlow`

## Goal

Prevent unstable code from reaching protected branches and enforce CI/review discipline.

## Protected Branches

- `main`
- `dev`

## Recommended Rules

1. **Require a pull request before merging**
   - Minimum approvals: `1`
   - Dismiss stale approvals when new commits are pushed: `ON`
2. **Require status checks to pass before merging**
   - Required check context: `build` (from workflow `CI Build`)
3. **Require branches to be up to date before merging**: `ON`
4. **Require conversation resolution before merging**: `ON`
5. **Restrict force pushes**: `ON` (disabled)
6. **Restrict branch deletion**: `ON`

## Apply Automatically (API)

Use script:

- `scripts/apply_branch_protection.ps1`

Inputs:

- `GitHubToken` (PAT with `repo` scope)
- `Owner` (`Honored7`)
- `Repo` (`ApexFlow`)

Example:

```powershell
pwsh -ExecutionPolicy Bypass -File .\scripts\apply_branch_protection.ps1 \
  -GitHubToken "<PAT>" \
  -Owner "Honored7" \
  -Repo "ApexFlow"
```

## Verify

On GitHub:

- `Settings` → `Branches` → `Branch protection rules`
- Confirm rules exist for `main` and `dev`
- Confirm required check includes `build`
