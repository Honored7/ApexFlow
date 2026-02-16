# Hybrid Architecture (C# Core + Optional Python Sidecar)

## Decision
Use **C# as production runtime** and treat Python as an **optional analytics sidecar**.

## Why this is the right approach
1. cTrader execution and charting are natively C#; this gives best reliability and lowest latency.
2. Python can improve feature discovery (ML/research), but should not be a hard dependency for live execution.
3. A sidecar model allows innovation without increasing single-point-of-failure risk in trade-time logic.

## Go / No-Go Gate for Python Sidecar
Enable external Python signals only if all are true:
- Round-trip latency stays under configured threshold for at least 95% of requests.
- Sidecar availability is above configured uptime threshold during market sessions.
- Signal drift remains within accepted bounds against C# baseline on replay data.
- Fallback path is tested and recovers within one bar/tick cycle.

## Reliability Rules (Hard)
- Fail-closed for execution decisions: if sidecar fails, ignore external signal and continue with C# core.
- Timeouts are mandatory for external calls.
- Never block chart/event loop waiting indefinitely for sidecar response.
- Keep deterministic local score as source of truth.

## Phased Rollout
- Phase 1: C# only (current).
- Phase 2: External signal contract + disabled provider (compile-time/runtime ready).
- Phase 3: Python sidecar in paper/replay mode only.
- Phase 4: Limited live shadow mode (no execution influence).
- Phase 5: Weighted influence in live mode after statistical validation.

## Signal Blending
Final score = `LocalScore * (1 - ExternalWeight) + ExternalScore * ExternalWeight`.
When external data is stale/invalid, use LocalScore only.

## Suggested Sidecar Payload
- Symbol, timeframe, timestamp, OHLCV slice, local features.
- Response: external score, confidence, model version, event time.

## Security/Operations
- Localhost-only endpoint for sidecar.
- Version pinning and structured logs for post-trade audit.
- Circuit-breaker after consecutive failures.
