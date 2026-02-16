using System;
using System.Collections.Generic;

namespace cAlgo.Indicators
{
    // ── Direction + Regime Enums ────────────────────────────────────────
    public enum StructureDirection { None, Bullish, Bearish }

    // ── Zones (Order Blocks, FVGs, Liquidity Sweeps) ───────────────────
    public sealed class OrderBlock
    {
        public double High { get; set; }
        public double Low { get; set; }
        public StructureDirection Direction { get; set; }
        public int BarIndex { get; set; }
        public bool Mitigated { get; set; }
        public double MidPrice => (High + Low) / 2.0;
    }

    public sealed class FairValueGap
    {
        public double High { get; set; }
        public double Low { get; set; }
        public StructureDirection Direction { get; set; }
        public int BarIndex { get; set; }
        public bool Filled { get; set; }
        public double MidPrice => (High + Low) / 2.0;
        public double SizeInPips(double pipSize) => pipSize > 0 ? Math.Abs(High - Low) / pipSize : 0;
    }

    public sealed class LiquiditySweep
    {
        public double SweptLevel { get; set; }
        public double WickExtreme { get; set; }
        public StructureDirection Direction { get; set; }
        public int BarIndex { get; set; }
    }

    public sealed class StructurePoint
    {
        public double Price { get; set; }
        public int BarIndex { get; set; }
        public bool IsHigh { get; set; }
    }

    public sealed class StructureBreak
    {
        public StructureDirection Direction { get; set; }
        public bool IsChoCH { get; set; }
        public double BrokenLevel { get; set; }
        public int BarIndex { get; set; }
    }

    // ── Market Structure State (per symbol) ────────────────────────────
    public sealed class MarketStructureState
    {
        public List<StructurePoint> SwingHighs { get; } = new List<StructurePoint>();
        public List<StructurePoint> SwingLows { get; } = new List<StructurePoint>();
        public List<OrderBlock> ActiveOrderBlocks { get; } = new List<OrderBlock>();
        public List<FairValueGap> ActiveFvgs { get; } = new List<FairValueGap>();
        public List<LiquiditySweep> RecentSweeps { get; } = new List<LiquiditySweep>();
        public StructureBreak LastBreak { get; set; }
        public StructureDirection PrevailingTrend { get; set; } = StructureDirection.None;
        public double LastSwingHigh { get; set; } = double.NaN;
        public double LastSwingLow { get; set; } = double.NaN;
        public int LastSwingHighIndex { get; set; } = -1;
        public int LastSwingLowIndex { get; set; } = -1;

        private const int MaxZones = 20;
        private const int MaxSweeps = 10;
        private const int MaxSwings = 30;

        public void Prune()
        {
            while (ActiveOrderBlocks.Count > MaxZones) ActiveOrderBlocks.RemoveAt(0);
            while (ActiveFvgs.Count > MaxZones) ActiveFvgs.RemoveAt(0);
            while (RecentSweeps.Count > MaxSweeps) RecentSweeps.RemoveAt(0);
            while (SwingHighs.Count > MaxSwings) SwingHighs.RemoveAt(0);
            while (SwingLows.Count > MaxSwings) SwingLows.RemoveAt(0);
        }
    }

    // ── Engine Result ──────────────────────────────────────────────────
    public sealed class StructureUpdate
    {
        public bool BosUp { get; set; }
        public bool BosDown { get; set; }
        public bool IsChoCH { get; set; }
        public OrderBlock NewOrderBlock { get; set; }
        public FairValueGap NewFvg { get; set; }
        public LiquiditySweep NewSweep { get; set; }
        public StructureDirection PrevailingTrend { get; set; }
        public double LastSwingHigh { get; set; } = double.NaN;
        public double LastSwingLow { get; set; } = double.NaN;
    }

    // ── Market Structure Engine ────────────────────────────────────────
    public static class MarketStructureEngine
    {
        /// <summary>
        /// Main per-bar update. Call once per completed bar.
        /// Needs high/low/close/open of at least swingLookback*2+3 bars.
        /// </summary>
        public static StructureUpdate Update(
            MarketStructureState state,
            int index,
            Func<int, double> open,
            Func<int, double> high,
            Func<int, double> low,
            Func<int, double> close,
            int swingLookback,
            double pipSize,
            double minSweepPips)
        {
            var result = new StructureUpdate
            {
                PrevailingTrend = state.PrevailingTrend,
                LastSwingHigh = state.LastSwingHigh,
                LastSwingLow = state.LastSwingLow
            };

            // ── 1. Detect swing points (lagged by swingLookback) ──────
            int swingCandidate = index - swingLookback;
            if (swingCandidate > swingLookback)
            {
                if (IsSwingHigh(high, swingCandidate, swingLookback))
                {
                    state.LastSwingHigh = high(swingCandidate);
                    state.LastSwingHighIndex = swingCandidate;
                    state.SwingHighs.Add(new StructurePoint
                    {
                        Price = state.LastSwingHigh,
                        BarIndex = swingCandidate,
                        IsHigh = true
                    });
                    result.LastSwingHigh = state.LastSwingHigh;
                }

                if (IsSwingLow(low, swingCandidate, swingLookback))
                {
                    state.LastSwingLow = low(swingCandidate);
                    state.LastSwingLowIndex = swingCandidate;
                    state.SwingLows.Add(new StructurePoint
                    {
                        Price = state.LastSwingLow,
                        BarIndex = swingCandidate,
                        IsHigh = false
                    });
                    result.LastSwingLow = state.LastSwingLow;
                }
            }

            // ── 2. Detect Break of Structure (BOS) / Change of Character (ChoCH) ──
            double prevClose = close(index - 1);
            double currClose = close(index);

            bool bosUp = !double.IsNaN(state.LastSwingHigh) &&
                         prevClose <= state.LastSwingHigh &&
                         currClose > state.LastSwingHigh;

            bool bosDown = !double.IsNaN(state.LastSwingLow) &&
                           prevClose >= state.LastSwingLow &&
                           currClose < state.LastSwingLow;

            bool isChoCH = false;

            if (bosUp)
            {
                isChoCH = state.PrevailingTrend == StructureDirection.Bearish;
                state.PrevailingTrend = StructureDirection.Bullish;
                state.LastBreak = new StructureBreak
                {
                    Direction = StructureDirection.Bullish,
                    IsChoCH = isChoCH,
                    BrokenLevel = state.LastSwingHigh,
                    BarIndex = index
                };

                // Create bullish Order Block: last bearish candle before BOS
                var ob = FindOrderBlock(index, StructureDirection.Bullish, open, high, low, close, 10);
                if (ob != null)
                {
                    state.ActiveOrderBlocks.Add(ob);
                    result.NewOrderBlock = ob;
                }
            }

            if (bosDown)
            {
                isChoCH = state.PrevailingTrend == StructureDirection.Bullish;
                state.PrevailingTrend = StructureDirection.Bearish;
                state.LastBreak = new StructureBreak
                {
                    Direction = StructureDirection.Bearish,
                    IsChoCH = isChoCH,
                    BrokenLevel = state.LastSwingLow,
                    BarIndex = index
                };

                // Create bearish Order Block: last bullish candle before BOS
                var ob = FindOrderBlock(index, StructureDirection.Bearish, open, high, low, close, 10);
                if (ob != null)
                {
                    state.ActiveOrderBlocks.Add(ob);
                    result.NewOrderBlock = ob;
                }
            }

            result.BosUp = bosUp;
            result.BosDown = bosDown;
            result.IsChoCH = isChoCH;
            result.PrevailingTrend = state.PrevailingTrend;

            // ── 3. Detect Fair Value Gaps ──────────────────────────────
            if (index >= 2)
            {
                var fvg = DetectFvg(index, open, high, low, close, pipSize);
                if (fvg != null)
                {
                    state.ActiveFvgs.Add(fvg);
                    result.NewFvg = fvg;
                }
            }

            // ── 4. Detect Liquidity Sweeps ─────────────────────────────
            var sweep = DetectLiquiditySweep(state, index, high, low, close, pipSize, minSweepPips);
            if (sweep != null)
            {
                state.RecentSweeps.Add(sweep);
                result.NewSweep = sweep;
            }

            // ── 5. Mark mitigated Order Blocks and filled FVGs ─────────
            UpdateZoneMitigation(state, high(index), low(index));

            state.Prune();
            return result;
        }

        // ── Helpers ────────────────────────────────────────────────────
        private static bool IsSwingHigh(Func<int, double> high, int index, int lookback)
        {
            double pivot = high(index);
            for (int i = 1; i <= lookback; i++)
            {
                if (pivot <= high(index - i) || pivot <= high(index + i))
                    return false;
            }
            return true;
        }

        private static bool IsSwingLow(Func<int, double> low, int index, int lookback)
        {
            double pivot = low(index);
            for (int i = 1; i <= lookback; i++)
            {
                if (pivot >= low(index - i) || pivot >= low(index + i))
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Find the last opposite candle before BOS for order block zone.
        /// For bullish OB: find the last bearish candle (close &lt; open) before BOS.
        /// For bearish OB: find the last bullish candle (close &gt; open) before BOS.
        /// </summary>
        private static OrderBlock FindOrderBlock(
            int bosIndex,
            StructureDirection bosDirection,
            Func<int, double> open,
            Func<int, double> high,
            Func<int, double> low,
            Func<int, double> close,
            int maxLookback)
        {
            for (int i = bosIndex - 1; i >= Math.Max(0, bosIndex - maxLookback); i--)
            {
                bool isBearishCandle = close(i) < open(i);
                bool isBullishCandle = close(i) > open(i);

                if (bosDirection == StructureDirection.Bullish && isBearishCandle)
                {
                    return new OrderBlock
                    {
                        High = high(i),
                        Low = low(i),
                        Direction = StructureDirection.Bullish,
                        BarIndex = i,
                        Mitigated = false
                    };
                }

                if (bosDirection == StructureDirection.Bearish && isBullishCandle)
                {
                    return new OrderBlock
                    {
                        High = high(i),
                        Low = low(i),
                        Direction = StructureDirection.Bearish,
                        BarIndex = i,
                        Mitigated = false
                    };
                }
            }

            return null;
        }

        /// <summary>
        /// Detect a Fair Value Gap using the 3-candle pattern.
        /// Bullish FVG: candle[i-2].high &lt; candle[i].low (gap up)
        /// Bearish FVG: candle[i-2].low &gt; candle[i].high (gap down)
        /// </summary>
        private static FairValueGap DetectFvg(
            int index,
            Func<int, double> open,
            Func<int, double> high,
            Func<int, double> low,
            Func<int, double> close,
            double pipSize)
        {
            double candle1High = high(index - 2);
            double candle3Low = low(index);
            double candle1Low = low(index - 2);
            double candle3High = high(index);

            // Bullish FVG: gap between candle 1 high and candle 3 low
            if (candle3Low > candle1High)
            {
                double gapPips = pipSize > 0 ? (candle3Low - candle1High) / pipSize : 0;
                if (gapPips >= 0.5) // minimum gap size
                {
                    return new FairValueGap
                    {
                        High = candle3Low,
                        Low = candle1High,
                        Direction = StructureDirection.Bullish,
                        BarIndex = index - 1,
                        Filled = false
                    };
                }
            }

            // Bearish FVG: gap between candle 1 low and candle 3 high
            if (candle1Low > candle3High)
            {
                double gapPips = pipSize > 0 ? (candle1Low - candle3High) / pipSize : 0;
                if (gapPips >= 0.5)
                {
                    return new FairValueGap
                    {
                        High = candle1Low,
                        Low = candle3High,
                        Direction = StructureDirection.Bearish,
                        BarIndex = index - 1,
                        Filled = false
                    };
                }
            }

            return null;
        }

        /// <summary>
        /// Detect a liquidity sweep: wick past a swing level that sweeps stops then closes back inside.
        /// </summary>
        private static LiquiditySweep DetectLiquiditySweep(
            MarketStructureState state,
            int index,
            Func<int, double> high,
            Func<int, double> low,
            Func<int, double> close,
            double pipSize,
            double minSweepPips)
        {
            double currHigh = high(index);
            double currLow = low(index);
            double currClose = close(index);
            double minSweepPrice = minSweepPips * pipSize;

            // Check for bearish sweep (wick above swing high then close back below)
            if (!double.IsNaN(state.LastSwingHigh) && currHigh > state.LastSwingHigh + minSweepPrice)
            {
                if (currClose < state.LastSwingHigh)
                {
                    return new LiquiditySweep
                    {
                        SweptLevel = state.LastSwingHigh,
                        WickExtreme = currHigh,
                        Direction = StructureDirection.Bearish, // swept buyside, expect sell
                        BarIndex = index
                    };
                }
            }

            // Check for bullish sweep (wick below swing low then close back above)
            if (!double.IsNaN(state.LastSwingLow) && currLow < state.LastSwingLow - minSweepPrice)
            {
                if (currClose > state.LastSwingLow)
                {
                    return new LiquiditySweep
                    {
                        SweptLevel = state.LastSwingLow,
                        WickExtreme = currLow,
                        Direction = StructureDirection.Bullish, // swept sellside, expect buy
                        BarIndex = index
                    };
                }
            }

            return null;
        }

        /// <summary>
        /// Mark OBs as mitigated and FVGs as filled when price passes through them.
        /// </summary>
        private static void UpdateZoneMitigation(MarketStructureState state, double barHigh, double barLow)
        {
            for (int i = state.ActiveOrderBlocks.Count - 1; i >= 0; i--)
            {
                var ob = state.ActiveOrderBlocks[i];
                if (ob.Mitigated) continue;

                if (ob.Direction == StructureDirection.Bullish && barLow <= ob.Low)
                    ob.Mitigated = true;
                else if (ob.Direction == StructureDirection.Bearish && barHigh >= ob.High)
                    ob.Mitigated = true;
            }

            for (int i = state.ActiveFvgs.Count - 1; i >= 0; i--)
            {
                var fvg = state.ActiveFvgs[i];
                if (fvg.Filled) continue;

                if (fvg.Direction == StructureDirection.Bullish && barLow <= fvg.Low)
                    fvg.Filled = true;
                else if (fvg.Direction == StructureDirection.Bearish && barHigh >= fvg.High)
                    fvg.Filled = true;
            }

            // Remove old mitigated zones
            state.ActiveOrderBlocks.RemoveAll(ob => ob.Mitigated);
            state.ActiveFvgs.RemoveAll(fvg => fvg.Filled);
        }

        /// <summary>
        /// Check if price is at/near an active Order Block.
        /// </summary>
        public static OrderBlock FindNearestActiveOB(
            MarketStructureState state,
            double currentPrice,
            StructureDirection direction,
            double tolerancePrice)
        {
            OrderBlock nearest = null;
            double minDist = double.MaxValue;

            foreach (var ob in state.ActiveOrderBlocks)
            {
                if (ob.Mitigated || ob.Direction != direction) continue;

                // Price is near or inside the OB zone
                double dist;
                if (currentPrice >= ob.Low - tolerancePrice && currentPrice <= ob.High + tolerancePrice)
                    dist = 0;
                else if (direction == StructureDirection.Bullish)
                    dist = currentPrice - ob.High; // positive = above OB, want price to come down to it
                else
                    dist = ob.Low - currentPrice;

                if (dist >= 0 && dist < minDist)
                {
                    minDist = dist;
                    nearest = ob;
                }
            }

            return nearest;
        }

        /// <summary>
        /// Check if price is at/in an active FVG zone.
        /// </summary>
        public static FairValueGap FindNearestActiveFvg(
            MarketStructureState state,
            double currentPrice,
            StructureDirection direction,
            double tolerancePrice)
        {
            foreach (var fvg in state.ActiveFvgs)
            {
                if (fvg.Filled || fvg.Direction != direction) continue;

                if (currentPrice >= fvg.Low - tolerancePrice && currentPrice <= fvg.High + tolerancePrice)
                    return fvg;
            }
            return null;
        }

        /// <summary>
        /// Check if a recent liquidity sweep occurred within maxAge bars.
        /// </summary>
        public static LiquiditySweep FindRecentSweep(
            MarketStructureState state,
            int currentIndex,
            StructureDirection direction,
            int maxAgeBars)
        {
            for (int i = state.RecentSweeps.Count - 1; i >= 0; i--)
            {
                var sweep = state.RecentSweeps[i];
                if (sweep.Direction == direction && currentIndex - sweep.BarIndex <= maxAgeBars)
                    return sweep;
            }
            return null;
        }
    }
}
