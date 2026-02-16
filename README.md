# ApexFlow SMC + Execution System

This workspace contains two production-oriented cTrader components:

- `ApexFlowSMC` (indicator): market structure, regime, profile nodes, confidence gating, analytics.
- `ApexFlowExecutor` (cBot): multi-symbol scanner and safety-first execution/risk layer.

## Projects

- `ApexFlowSMC/ApexFlowSMCIndicator.cs`
- `ApexFlowExecutor/ApexFlowExecutionBot.cs`
- `ApexFlowSMC.slnx`

## Documentation Index

- `docs/ARCHITECTURE.md`
- `docs/STRATEGY_AND_SETUPS.md`
- `docs/MODULE_REFERENCE.md`
- `docs/OPERATIONS_RUNBOOK.md`
- `docs/GIT_VERSIONING_AND_RECOVERY.md`
- `docs/BRANCH_PROTECTION.md`

## Quick Start

1. Build solution:
   - `dotnet build .\ApexFlowSMC.slnx -v:minimal`
2. In cTrader, load:
   - Indicator: `ApexFlowSmartMoneyIndicator`
   - Robot: `ApexFlowExecutionBot`
3. Safe startup for robot:
   - `Enable Auto Execution = false`
   - Validate diagnostics and symbol scan behavior first
   - Then enable auto execution with conservative risk profile
