# Core Strategies and Setups

## Core Strategy (Execution Bot)

The cBot is a **multi-symbol continuation strategy**:

1. Build symbol-level context (MA trend, swing/BOS, normalized regime).
2. Require continuation permission from regime engine.
3. Require trend alignment (fast vs slow MA).
4. Apply signal lifecycle cooldown.
5. Rank all valid candidates and execute best (or all, based on setting).

## Regime v2 Logic

Regime uses:

- momentum
- BOS up/down
- HTF fast/slow relationship
- volatility normalization (rolling range)
- session activity normalization (rolling tick-volume ratio)
- hysteresis (enter/exit chop bands)

Outputs:

- `Regime`
- `AllowLongContinuation`
- `AllowShortContinuation`
- `TrendStrength`

## Entry Filters (Execution)

A symbol candidate is eligible only if all pass:

- Session filter (Asia/London/NY toggles)
- Spread absolute cap (`Max Spread (pips)`)
- Spread relative cap (`Max Spread/Avg Ratio`)
- Regime type gate (Uptrend/Downtrend; Pullback optional)
- Min regime strength
- MA alignment
- Cooldown/lifecycle gate

## Risk & Safety Stack

- Dynamic position sizing (`Risk Per Trade (%)`) or fixed lot size
- Adaptive ATR stop-loss/take-profit (optional)
- Max positions per symbol
- Max concurrent active symbols
- Max daily trades
- Max gross exposure (lots)
- Max open risk (%)
- Max daily loss lock (optional forced close)
- Entry deviation cap + retries
- Non-retriable error early exit
- Performance guard pause loop prevention (only evaluates on new closed trades)

## Performance Guard

Triggers pause when recent closed-trade quality degrades:

- rolling window win-rate below threshold
- rolling window drawdown above threshold

When triggered:

- entries are paused for configured bar count
- guard resumes automatically after cooldown

## Indicator Setups

### Structural/SMC Setup

- Swing/BOS structure
- FVG and continuation markers
- Session volume profile nodes (POC/HVN/LVN)
- Strict trigger and confidence scoring

### Execution Bridge Setup

Recommended initial setup:

- `Enable Auto Execution = false`
- `Max Concurrent Symbols = 1`
- `Max Trades / Day = 2–3`
- `Risk Per Trade (%) = 0.15–0.25`
- `Allow Pullback Entries = false`

Then enable execution after diagnostics validate behavior.
