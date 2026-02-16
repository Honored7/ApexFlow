# System Architecture

## Overview

The system is split into two layers:

1. **Analytics Layer (`ApexFlowSMC`)**
   - Produces market context and signal quality diagnostics.
   - Focuses on structure, profile nodes, order-flow proxy, regime, confidence, and outcomes.

2. **Execution Layer (`ApexFlowExecutor`)**
   - Scans symbols and executes only when strict risk and quality gates pass.
   - Handles sizing, spread/slippage controls, portfolio limits, and performance circuit breaker.

## High-Level Data Flow

1. Bars/ticks/depth arrive from cTrader.
2. Regime and signal quality are computed (normalized momentum, HTF alignment, lifecycle filters).
3. Scanner builds per-symbol trade candidates.
4. Candidates are ranked by regime strength.
5. Execution applies risk/safety gates.
6. Orders are placed with adaptive SL/TP.
7. Performance guard monitors closed-trade quality and can pause entries.

## Design Principles

- **Fail-safe defaults**: auto execution off by default; hard limits on spread/risk/exposure.
- **Deterministic core**: local C# logic remains primary source of truth.
- **Modular engines**: separate concern boundaries for profile, reliability, regime, lifecycle, outcomes.
- **Cross-symbol scalability**: independent per-symbol context state in scanner.

## Projects and Responsibilities

### `ApexFlowSMC` (Indicator)

- `ApexFlowSMCIndicator.cs`
  - Composite indicator runtime and rendering.
- Engines:
  - `RegimeStateEngine.cs`
  - `SessionVolumeProfile.cs`
  - `SignalReliabilityEngine.cs`
  - `LevelScoringEngine.cs`
  - `SignalLifecycleEngine.cs`
  - `SignalOutcomeTracker.cs`
- Hybrid contracts:
  - `HybridContracts.cs`
  - `HybridBlend.cs`
  - `HYBRID_ARCHITECTURE.md`

### `ApexFlowExecutor` (cBot)

- `ApexFlowExecutionBot.cs`
  - Multi-symbol evaluation + ranked candidate execution.
  - Safety/risk controls and performance guard.
  - Adaptive ATR-based exits.

## cTrader Packaging/Deployment

- Custom post-build targets deploy named artifacts to cTrader Sources roots.
- Legacy `CBOT_1` alias artifacts are explicitly cleaned.
- Active artifacts:
  - `ApexFlowSmartMoneyIndicator.algo`
  - `ApexFlowExecutionBot.algo`
