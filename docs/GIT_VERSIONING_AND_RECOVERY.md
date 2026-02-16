# Git Versioning and Auto-Recovery

## Branching Model

- `main`: stable baseline
- `dev`: active work
- `hotfix/*`: urgent fixes
- `experiment/*`: strategy experiments

## Versioning Rules

- Semantic tags: `vMAJOR.MINOR.PATCH`
  - `PATCH`: bug fix/no behavior intent change
  - `MINOR`: new feature/parameter/module
  - `MAJOR`: breaking behavior or architecture change

## Auto-Recovery Approach

This workspace includes scripts for frequent checkpoints and fast rollback:

- `scripts/setup_git_recovery.ps1`: initialize hooks + baseline branches
- `scripts/checkpoint.ps1`: auto-commit local checkpoint with timestamp
- `scripts/restore_last_stable.ps1`: reset to latest tagged stable release

## Recommended Workflow

1. Initialize once:
   - `pwsh -ExecutionPolicy Bypass -File .\scripts\setup_git_recovery.ps1`
2. Create checkpoints during tuning:
   - `pwsh -ExecutionPolicy Bypass -File .\scripts\checkpoint.ps1 -Message "tuning atr xau"`
3. Tag stable milestones:
   - `git tag -a v0.1.0 -m "stable baseline"`
4. Recover if needed:
   - `pwsh -ExecutionPolicy Bypass -File .\scripts\restore_last_stable.ps1`

## Suggested Backup Cadence

- Before every parameter-set change
- Before enabling auto execution
- At end of each trading week

## GitHub Push

After local init and commit:

1. Create empty GitHub repo.
2. Add remote:
   - `git remote add origin <REPO_URL>`
3. Push:
   - `git push -u origin main`
   - `git push --tags`
