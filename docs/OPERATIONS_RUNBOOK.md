# Operations Runbook

## Build and Deploy

1. Build:
   - `dotnet build .\ApexFlowSMC.slnx -v:minimal`
2. Verify cTrader artifacts:
   - Indicator: `ApexFlowSmartMoneyIndicator.algo`
   - Robot: `ApexFlowExecutionBot.algo`

## Recommended Rollout Sequence

### Stage 1 — Dry Validation

- Robot: `Enable Auto Execution = false`
- Validate symbol scanner, session filter, spread controls, and candidate ranking in logs.

### Stage 2 — Micro Risk

- `Risk Per Trade (%) = 0.15–0.25`
- `Max Concurrent Symbols = 1`
- `Max Trades / Day = 2–3`
- `Allow Pullback Entries = false`
- Keep performance guard enabled.

### Stage 3 — Controlled Expansion

- Add symbols one by one.
- Increase concurrency only after stable expectancy.
- Re-tune ATR multipliers per symbol if needed.

## Monitoring Checklist (Daily)

- Trade count vs `Max Trades / Day`
- Session-specific behavior (Asia/London/NY)
- Spread blocks and retry frequency
- Performance guard trigger frequency
- Net PnL and rolling drawdown

## Incident Playbook

### Continuous Guard Triggers

Actions:

1. Confirm recent trades are new (avoid stale-history loop).
2. Disable pullback entries.
3. Reduce risk per trade and max trades/day.
4. Restrict symbols to strongest 1–2.

### Fast Drawdown

Actions:

1. Turn `Enable Auto Execution = false`.
2. Export trade log and identify worst symbol/session.
3. Re-run with stricter spread/session filters.

### Broker/Execution Errors

Actions:

1. Check non-retriable error counts.
2. Reduce retries and tighten entry deviation.
3. Verify symbol market hours and spreads.

## Backtest/Forward-Test Discipline

- Use out-of-sample periods by symbol.
- Avoid changing many knobs at once.
- Track changes with git tags and changelog notes.
