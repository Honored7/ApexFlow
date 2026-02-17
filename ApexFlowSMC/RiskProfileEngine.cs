using System;

namespace cAlgo.Indicators
{
    /// <summary>
    /// Configurable risk profiles: Conservative, Moderate, Aggressive, Custom.
    /// Each profile controls position sizing, trade frequency limits,
    /// drawdown thresholds, and SL/TP multipliers.
    /// </summary>
    public enum RiskProfileType
    {
        Conservative,
        Moderate,
        Aggressive,
        Custom
    }

    public sealed class RiskProfile
    {
        /// <summary>Risk per trade as fraction (0.0025 = 0.25%)</summary>
        public double RiskPerTrade { get; set; }

        /// <summary>Max trades allowed per calendar day per symbol</summary>
        public int MaxTradesPerDayPerSymbol { get; set; }

        /// <summary>Max total trades per day across all symbols</summary>
        public int MaxTotalTradesPerDay { get; set; }

        /// <summary>Max daily drawdown as fraction of equity (0.01 = 1%)</summary>
        public double MaxDailyDrawdownPct { get; set; }

        /// <summary>Max concurrent positions across all symbols</summary>
        public int MaxConcurrentPositions { get; set; }

        /// <summary>SL distance multiplier relative to base ATR SL</summary>
        public double SlMultiplier { get; set; }

        /// <summary>TP distance multiplier relative to base ATR TP</summary>
        public double TpMultiplier { get; set; }

        /// <summary>Minimum risk:reward ratio allowed</summary>
        public double MinRiskReward { get; set; }

        /// <summary>Move SL to breakeven at this R-multiple</summary>
        public double BreakevenAtR { get; set; }

        /// <summary>Close this fraction of position at first TP target</summary>
        public double PartialCloseFraction { get; set; }

        /// <summary>Close partial at this R-multiple</summary>
        public double PartialCloseAtR { get; set; }

        /// <summary>Win rate performance guard (trades halted below this)</summary>
        public double MinWinRateGuard { get; set; }

        /// <summary>Minimum recent trades before performance guard applies</summary>
        public int MinTradesForGuard { get; set; }

        /// <summary>Cooldown bars between trades on same symbol</summary>
        public int CooldownBars { get; set; }
    }

    public static class RiskProfileEngine
    {
        public static RiskProfile GetProfile(RiskProfileType type)
        {
            switch (type)
            {
                case RiskProfileType.Conservative:
                    return new RiskProfile
                    {
                        RiskPerTrade = 0.0025,          // 0.25%
                        MaxTradesPerDayPerSymbol = 2,
                        MaxTotalTradesPerDay = 4,
                        MaxDailyDrawdownPct = 0.01,     // 1%
                        MaxConcurrentPositions = 2,
                        SlMultiplier = 1.2,             // slightly wider SL
                        TpMultiplier = 1.0,
                        MinRiskReward = 2.5,
                        BreakevenAtR = 1.2,             // don't BE too early
                        PartialCloseFraction = 0.5,
                        PartialCloseAtR = 2.0,          // let winners develop
                        MinWinRateGuard = 0.25,
                        MinTradesForGuard = 15,
                        CooldownBars = 10
                    };

                case RiskProfileType.Moderate:
                    return new RiskProfile
                    {
                        RiskPerTrade = 0.0075,          // 0.75%
                        MaxTradesPerDayPerSymbol = 3,
                        MaxTotalTradesPerDay = 8,
                        MaxDailyDrawdownPct = 0.025,    // 2.5%
                        MaxConcurrentPositions = 4,
                        SlMultiplier = 1.0,
                        TpMultiplier = 1.0,
                        MinRiskReward = 2.0,
                        BreakevenAtR = 1.0,             // give room to breathe
                        PartialCloseFraction = 0.5,
                        PartialCloseAtR = 1.5,          // let winners develop
                        MinWinRateGuard = 0.28,
                        MinTradesForGuard = 12,
                        CooldownBars = 8
                    };

                case RiskProfileType.Aggressive:
                    return new RiskProfile
                    {
                        RiskPerTrade = 0.015,           // 1.5%
                        MaxTradesPerDayPerSymbol = 5,
                        MaxTotalTradesPerDay = 12,
                        MaxDailyDrawdownPct = 0.04,     // 4%
                        MaxConcurrentPositions = 6,
                        SlMultiplier = 0.8,             // tighter SL
                        TpMultiplier = 1.2,             // wider TP
                        MinRiskReward = 1.5,
                        BreakevenAtR = 0.8,             // moved from 0.5
                        PartialCloseFraction = 0.4,
                        PartialCloseAtR = 1.2,          // moved from 0.8
                        MinWinRateGuard = 0.22,
                        MinTradesForGuard = 10,
                        CooldownBars = 3
                    };

                default: // Custom — return Moderate as base, user overrides in bot params
                    return GetProfile(RiskProfileType.Moderate);
            }
        }

        /// <summary>
        /// Calculate position size in volume (lots) given risk parameters.
        /// </summary>
        /// <param name="equity">Account equity in deposit currency</param>
        /// <param name="riskFraction">Risk per trade as fraction (e.g. 0.0075)</param>
        /// <param name="slDistancePips">SL distance in pips</param>
        /// <param name="pipValue">Value of 1 pip per 1 lot in deposit currency</param>
        /// <param name="minVolume">Minimum allowed volume</param>
        /// <param name="maxVolume">Maximum allowed volume</param>
        /// <param name="volumeStep">Volume step/increment</param>
        /// <returns>Position volume in lots, clamped to min/max and rounded to step</returns>
        public static double CalculateVolume(
            double equity,
            double riskFraction,
            double slDistancePips,
            double pipValue,
            double minVolume,
            double maxVolume,
            double volumeStep)
        {
            if (slDistancePips <= 0 || pipValue <= 0 || equity <= 0)
                return minVolume;

            double riskAmount = equity * riskFraction;
            double rawVolume = riskAmount / (slDistancePips * pipValue);

            // Round down to volume step
            double volume = Math.Floor(rawVolume / volumeStep) * volumeStep;

            // Clamp
            volume = Math.Max(minVolume, Math.Min(maxVolume, volume));

            return volume;
        }

        /// <summary>
        /// Check whether a new trade is allowed given daily performance.
        /// </summary>
        public static bool IsTradingAllowed(
            RiskProfile profile,
            int tradesTodayTotal,
            int tradesTodayThisSymbol,
            int currentConcurrentPositions,
            double dailyPnLPct,
            int recentTradeCount,
            double recentWinRate)
        {
            if (tradesTodayTotal >= profile.MaxTotalTradesPerDay)
                return false;

            if (tradesTodayThisSymbol >= profile.MaxTradesPerDayPerSymbol)
                return false;

            if (currentConcurrentPositions >= profile.MaxConcurrentPositions)
                return false;

            // Daily drawdown guard (dailyPnLPct is negative when losing)
            if (dailyPnLPct <= -profile.MaxDailyDrawdownPct)
                return false;

            // Performance guard: only applies after minimum trades
            if (recentTradeCount >= profile.MinTradesForGuard
                && recentWinRate < profile.MinWinRateGuard)
                return false;

            return true;
        }
    }
}
