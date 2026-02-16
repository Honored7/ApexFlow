# Module Reference

## `ApexFlowSMC`

### `RegimeStateEngine.cs`

- `Evaluate(...)`: classifies `Chop/Transition/Pullback/Uptrend/Downtrend` with normalization + hysteresis.
- `Clamp(...)`: bounded helper.

### `SessionVolumeProfile.cs`

- `SessionVolumeProfileEngine.Update(...)`: updates session bins.
- `BuildSnapshot(...)`: emits POC/HVN/LVN snapshot.
- `VolumeProfileSnapshot` helpers:
  - `VolumeRatioAtPrice(...)`
  - `IsNearAnyNode(...)`
  - `DistanceToPocInPips(...)`

### `SignalReliabilityEngine.cs`

- `EvaluateStrictMode(...)`: strict pass/fail context for buy/sell.
- `ComputeConfidence(...)`: composite confidence score.
- `FindNearestLevel(...)`: nearest node lookup.

### `LevelScoringEngine.cs`

- `Evaluate(...)`: support/resistance source scoring.
- distance/scoring helpers for nearest level and decay.

### `SignalLifecycleEngine.cs`

- `CanEmitBuy(...)`, `CanEmitSell(...)`
- `MarkBuy(...)`, `MarkSell(...)`

### `SignalOutcomeTracker.cs`

- `TrackSignal(...)`: enqueue new signal.
- `Update(...)`: resolve outcomes by horizon/move.
- `GetSnapshot()`: aggregate performance metrics.

### `HybridContracts.cs` and `HybridBlend.cs`

- `ExternalSignalRequest/Response`
- `IExternalSignalProvider` and `DisabledExternalSignalProvider`
- `HybridBlend.ComposeScore(...)`

### `ApexFlowSMCIndicator.cs`

Main runtime methods:

- `Calculate(...)`
- `AnalyzeStructure(...)`
- `AnalyzeOrderFlow(...)`
- `AnalyzePatterns(...)`
- `Render(...)`
- `UpdateRegimeNormalization(...)`
- `GetCurrentHtfMaValues(...)`
- preset application + parameter profile methods

## `ApexFlowExecutor`

### `ApexFlowExecutionBot.cs`

Signal and execution lifecycle:

- `OnStart()` initializes scanner contexts.
- `OnBar()` orchestrates guard checks, candidate ranking, execution.
- `EvaluateCandidate(...)` builds per-symbol signals.
- `TryExecute(...)` executes with full safety stack.

Risk/sizing methods:

- `ResolveStopsPips(...)` (ATR/static stop handling)
- `GetRequestedVolumeInUnits(...)` (dynamic or fixed sizing)
- `IsExposureWithinLimits(...)`
- `EstimatePositionRiskAmount(...)`

Operational guards:

- `IsSessionAllowed(...)`
- `UpdateSpreadProfile(...)`
- `IsSpreadTradeable(...)`
- `IsPerformanceGuardTriggered(...)`
- `IsDailyLossLimitReached(...)`

Scanner support:

- `ResolveTradeSymbols(...)`
- `CreateSymbolContext(...)`
- internal `SymbolContext` and `TradeCandidate` models
