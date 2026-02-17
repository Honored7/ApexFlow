# ApexFlow Smart Money Indicator — User Guide

> **Version:** 2.0 — Manual Trading Edition  
> **Platform:** cTrader (cAlgo)  
> **Overlay:** Yes (draws directly on the price chart)

---

## Table of Contents

1. [Overview](#overview)
2. [Installation](#installation)
3. [Modules & What They Show](#modules--what-they-show)
   - [Market Structure](#1-market-structure)
   - [Order Flow Bubbles](#2-order-flow-bubbles)
   - [Volume Profile](#3-volume-profile)
   - [Regime Detection](#4-regime-detection)
   - [Signal Arrows](#5-signal-arrows)
   - [Key Levels](#6-key-levels)
   - [Info Panel](#7-info-panel)
4. [Parameter Reference](#parameter-reference)
5. [How to Read the Chart](#how-to-read-the-chart)
6. [Recommended Workflows](#recommended-workflows)
7. [Tuning for Your Symbol](#tuning-for-your-symbol)
8. [FAQ & Troubleshooting](#faq--troubleshooting)

---

## Overview

ApexFlow SMC is a **complete Smart Money Concepts (SMC) + Order Flow** indicator for manual trading. It overlays institutional-grade analysis directly on your chart:

- **Where** are the institutional zones? → Order Blocks, FVG, Liquidity Sweeps
- **What** is the market doing? → BOS / ChoCH structure breaks
- **Who** is in control? → Order Flow Bubbles (aggressive buyers/sellers)
- **What** regime are we in? → ADX-based regime with direction
- **When** should I look for entries? → Confluence signal arrows

The indicator is **fully independent** from the ApexFlow trading bot. It shares the same analysis engines, so what you see on the chart matches what the bot would analyze internally.

---

## Installation

1. **Build** the `ApexFlowSMC` project (it auto-deploys the `.algo` file)
2. In cTrader, go to **Indicators → Custom** and find **ApexFlowSmartMoneyIndicator**
3. Drag it onto any chart
4. Adjust parameters in the settings panel (all features can be toggled on/off)

---

## Modules & What They Show

### 1. Market Structure

The core ICT (Inner Circle Trader) framework:

| Element | Visual | What It Means |
|---------|--------|---------------|
| **Order Block (OB)** | Green/Red rectangle | Last candle before an impulsive move. Institutional supply/demand zone. Price often returns here before continuing. Green = bullish demand, Red = bearish supply. |
| **Fair Value Gap (FVG)** | Blue/Orange rectangle | A 3-candle gap where price moved too fast, leaving an imbalance. Price tends to "fill" these gaps. Blue = bullish, Orange = bearish. |
| **Break of Structure (BOS)** | Green ▲ / Red ▼ arrow + label | A swing high/low has been broken in the prevailing trend direction. Confirms trend continuation. |
| **Change of Character (ChoCH)** | Gold ▲/▼ arrow + label | A swing high/low broken **against** the prevailing trend. Signals potential reversal. Stronger than BOS. |
| **Liquidity Sweep** | "Sweep High" / "Sweep Low" label + dotted line | Price briefly pierced a swing level and rejected — stop hunts. Often precedes reversals. |

**Key Parameters:**
- `Swing Lookback` — How many bars to look left/right to confirm a swing point. Higher = fewer, stronger swings.
- `Min Sweep Pips` — Minimum wick extension beyond the swing level to count as a sweep.
- `Max OB/FVG Zones` — How many zones to keep on screen (older ones auto-clean).

---

### 2. Order Flow Bubbles

Estimates institutional order flow from **candle anatomy + volume**. Since cTrader provides tick volume (not real volume), this works as a volume-weighted aggression proxy.

| Bubble | Symbol | Color | Meaning |
|--------|--------|-------|---------|
| **Aggressive Buy** | `•`, `◉`, `●`, `●●` | Green (below candle) | Big body, small upper wick, high volume = buyers pushing hard. Size scales with aggression score. |
| **Aggressive Sell** | `•`, `◉`, `●`, `●●` | Red (above candle) | Big body, small lower wick, high volume = sellers pushing hard. |
| **Absorption** | `◉ ABS` | Gold (at mid-candle) | Very high volume but tiny body = one side absorbing the other's orders. Often seen at turning points. |
| **Exhaustion** | `◉ EXH` | Orange (at top) / Green (at bottom) | Long wick against the move + high volume = the move is running out of steam. |

Each bubble also shows a **volume ratio** label (e.g., `2.1x`) indicating how many times above average the volume was.

**How to use:**
- Multiple green `●` bubbles stacking at lows → buyers accumulating, look for longs
- Red `●` at highs followed by `◉ ABS` → sellers being absorbed, trend may continue up
- `◉ EXH` at a swing high → exhaustion, potential reversal coming

**Key Parameters:**
- `Aggression Threshold` (default 1.2) — Combined score needed. Lower = more bubbles. Raise to 1.5-2.0 if too noisy.
- `Min Volume Mult` (default 1.2) — Minimum volume vs. average to show any bubble. Raise if chart is cluttered.
- `Bubble Lookback` (default 20) — How many bars to average volume over.

---

### 3. Volume Profile

Session-based volume distribution showing where the most trading occurred:

| Level | Visual | Meaning |
|-------|--------|---------|
| **POC** (Point of Control) | Yellow solid line + label | The price with the highest traded volume. Acts as a magnet for price. |
| **HVN** (High Volume Node) | Blue dotted lines | Other prices with heavy volume. Act as support/resistance — price tends to consolidate here. |
| **LVN** (Low Volume Node) | Faint red dotted lines | Prices with very little volume. Price moves fast through these — good breakout/rejection zones. |

- Profiles are calculated per **session** (London, New York, Asia) and update as the session builds.
- `Bin Size` controls resolution — smaller bins = more detail but more noise. For XAUUSD, 0.5 works well. For EURUSD, try 0.0005.

---

### 4. Regime Detection

A live regime label in the **top-right corner** showing the current market state:

| ADX Range | Regime | Color (Bull/Bear) | What To Do |
|-----------|--------|------------|------------|
| ≥ 30 | **STRONG TREND** | Lime / Red | Trend-follow aggressively, avoid mean reversion |
| 20–30 | **TREND** | Teal / Salmon | Trend-follow with pullback entries |
| 15–20 | **RANGE** | Orange | Mean reversion from extremes, fade breakouts |
| < 15 | **CHOPPY — NO TRADE** | Gray | Stay out or reduce size significantly |

The second line shows **direction** (▲ BULL / ▼ BEAR based on +DI vs -DI) and **RSI** value.

Optional overlays:
- **Donchian Channel** (default ON) — Blue dotted band showing the N-period breakout range
- **Bollinger Bands** (default OFF) — Orange dotted band for mean reversion levels

---

### 5. Signal Arrows

Multi-factor confluence arrows that fire when enough conditions align. These are **decision support**, not blind entry signals.

**9 Confluence Factors:**

| # | Factor | Max Score | Description |
|---|--------|-----------|-------------|
| 1 | BOS | +1.0 | Break of structure in signal direction |
| 2 | ChoCH | +2.0 | Change of character (strongest) |
| 3 | Order Block | +1.0 | Price is inside an unmitigated OB zone |
| 4 | FVG | +0.75 | Price is inside an unfilled FVG zone |
| 5 | Liquidity Sweep | +1.5 | Recent sweep (within 5 bars) |
| 6 | RSI Extreme | +0.5 | RSI < 30 (buy) or > 70 (sell) |
| 7 | Volume Profile POC | +0.3 | Price near POC (within 15 pips) |
| 8 | ADX Direction | +0.5 | ADX ≥ 20 and +DI/-DI alignment |
| 9 | Flow Bubbles | +0.5–1.0 | Recent aggressive flow or exhaustion |
| 10 | Candle Strength | +0.3 | Strong body ratio > 65% |

- **Green ▲ triangle** = Buy confluence (score shown as `3.5★`)
- **Red ▼ triangle** = Sell confluence
- Minimum score to show: `Min Confluence Score` (default 2.5)
- Cooldown between signals: `Signal Cooldown` (default 6 bars)
- **Sound alert** plays on live signal (toggle with `Sound Alert on Signal`)

---

### 6. Key Levels

| Level | Visual | Purpose |
|-------|--------|---------|
| **PDH** (Previous Day High) | Orange dashed line + label | Key institutional resistance. Often swept for liquidity. |
| **PDL** (Previous Day Low) | Blue dashed line + label | Key institutional support. Often swept for liquidity. |
| **London Open** | Faint blue vertical line | Session open — high-probability move zone |
| **NY Open** | Faint orange vertical line | Most liquid session open |
| **Asia Open** | Faint purple vertical line | Range-setting session |

---

### 7. Info Panel

Top-left information dashboard showing:
- Current regime, ADX, +DI/-DI, RSI
- Donchian and Bollinger band values
- Structure trend direction
- Active (unmitigated) OB and FVG counts
- Volume Profile session, POC price, distance to POC
- Order flow summary (last 20 bars): aggressive buys, sells, absorptions, exhaustions

---

## Parameter Reference

| Group | Parameter | Default | Description |
|-------|-----------|---------|-------------|
| **Market Structure** | Swing Lookback | 5 | Bars left/right for swing detection |
| | Show Order Blocks | true | Draw OB rectangles |
| | Show FVG Zones | true | Draw FVG rectangles |
| | Show BOS / ChoCH | true | Draw structure break arrows |
| | Show Liquidity Sweeps | true | Mark stop hunts |
| | Min Sweep Pips | 2.0 | Minimum wick extension for sweep |
| | Max OB Zones | 6 | Max rectangles on screen |
| | Max FVG Zones | 6 | Max rectangles on screen |
| **Order Flow** | Show Flow Bubbles | true | Enable bubble system |
| | Bubble Lookback | 20 | Volume averaging period |
| | Aggression Threshold | 1.2 | Score needed for aggression bubbles |
| | Min Volume Mult | 1.2 | Minimum volume vs. average |
| | Max Bubbles Visible | 40 | Cleanup limit |
| | Show Absorption | true | Detect absorption patterns |
| | Show Exhaustion | true | Detect exhaustion patterns |
| **Volume Profile** | Show Volume Profile | true | Enable POC/HVN/LVN |
| | Bin Size (price) | 0.5 | Price resolution per bin |
| | HVN Percentile | 0.75 | Threshold for high-volume nodes |
| | LVN Percentile | 0.25 | Threshold for low-volume nodes |
| | Profile London/NY/Asia | true | Which sessions to profile |
| **Regime** | Show Regime Label | true | Top-right regime display |
| | Show Donchian Channel | true | Blue breakout channel |
| | Show Bollinger Bands | false | Orange mean-reversion bands |
| | ADX/Donchian/Bollinger/RSI periods | 14/20/20/14 | Technical indicator periods |
| **Signals** | Show Signal Arrows | true | Enable confluence arrows |
| | Min Confluence Score | 2.5 | Minimum score to fire |
| | Signal Cooldown (bars) | 6 | Minimum bars between signals |
| **Key Levels** | Show Prev Day H/L | true | Previous day's high/low |
| | Show Session Kill Zones | true | Session open vertical lines |
| **Alerts** | Sound Alert on Signal | true | Play sound on live signal |
| **Display** | Show Info Panel | true | Top-left information panel |

---

## How to Read the Chart

### Bullish Setup Example
1. **Regime** shows TREND ▲ BULL (green)
2. Price sweeps a **PDL** or swing low → "Sweep Low" appears
3. A **ChoCH ▲** fires (gold arrow) — structure shifts bullish
4. Price pulls back into a **bullish OB** (green rectangle)
5. Green **aggressive buy bubbles** `●` appear at the OB
6. **Signal arrow** ▲ fires with score 3.5★
7. → **Enter long**, SL below OB, TP at PDH or next resistance

### Bearish Setup Example
1. **Regime** shows TREND ▼ BEAR (red)
2. Price sweeps a **PDH** → "Sweep High" appears
3. A **ChoCH ▼** fires — structure shifts bearish
4. Price rallies into a **bearish OB** (red rectangle) overlapping an **FVG**
5. Red **aggressive sell bubbles** + `◉ EXH` at the top
6. **Signal arrow** ▼ fires with score 4.0★
7. → **Enter short**, SL above OB, TP at PDL or next support

### What NOT to trade
- Regime = **CHOPPY — NO TRADE** (gray) → skip
- Signal arrow score < 3.0 → weak confluence, wait
- No structure confirmation (no BOS/ChoCH) → the arrow alone isn't enough

---

## Recommended Workflows

### Scalping (M5–M15)
- Lower `Swing Lookback` to 3
- Lower `Signal Cooldown` to 3
- Raise `Aggression Threshold` to 1.5 (reduce noise)
- Watch for sweeps → ChoCH → OB entry

### Intraday Swing (M30–H1)
- Default parameters work well
- Use PDH/PDL as primary targets
- Session kill zones help time entries (London/NY opens)

### Position Trading (H4–D1)
- Raise `Swing Lookback` to 8–10
- Raise `Signal Cooldown` to 10
- VP `Bin Size` should match instrument: XAUUSD = 1.0, EURUSD = 0.001
- Focus on ChoCH + OB confluence for high-R entries

---

## Tuning for Your Symbol

| Symbol | VP Bin Size | Min Sweep Pips | Notes |
|--------|------------|----------------|-------|
| **XAUUSD** | 0.5–1.0 | 2.0 | Default settings work well |
| **EURUSD** | 0.0005 | 3.0 | Small pip size, increase sweep threshold |
| **GBPJPY** | 0.05 | 5.0 | Volatile, wider sweeps |
| **US30** | 5.0 | 20.0 | Index scale needs larger bins |
| **BTCUSD** | 50.0 | 50.0 | Crypto needs much larger values |

---

## FAQ & Troubleshooting

### Q: I don't see any bubbles
**A:** Check these settings:
1. `Show Flow Bubbles` = true
2. `Aggression Threshold` — try lowering to 1.0
3. `Min Volume Mult` — try lowering to 1.0
4. On low-volatility pairs, bubbles are naturally rarer. They appear when volume spikes relative to the lookback average.

### Q: Too many bubbles cluttering the chart
**A:** Raise `Aggression Threshold` to 1.5–2.0 and/or `Min Volume Mult` to 1.5.

### Q: Signal arrows appear too often / not enough
**A:** Adjust `Min Confluence Score`:
- More arrows: lower to 2.0
- Fewer, higher-quality arrows: raise to 3.0–3.5

### Q: Volume Profile lines seem wrong
**A:** The `Bin Size` must match your instrument's price scale. For XAUUSD (price ~2000), use 0.5–1.0. For EURUSD (price ~1.08), use 0.0005–0.001.

### Q: Can I use this with the bot simultaneously?
**A:** Yes. The indicator and bot are fully independent. The indicator draws on the chart for your analysis; the bot executes trades based on its own internal calculations using the same engines.

### Q: What timeframe works best?
**A:** M15–H1 for intraday. H4 for swing. The indicator adapts via ATR-based offsets, so visuals scale automatically to any timeframe.

### Q: The regime label says CHOPPY. Should I still trade signals?
**A:** Not recommended. The CHOPPY regime (ADX < 15) means price is moving sideways with no directional edge. Wait for ADX to rise above 15–20.

### Q: Sound alerts aren't working
**A:** The alert uses `C:\Windows\Media\notify.wav`. Ensure this file exists on your system, or the alert will be silent. cTrader must also have sound enabled.

---

*Built with the same institutional-grade engines as the ApexFlow Execution Bot.*
