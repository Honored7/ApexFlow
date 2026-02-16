using System;

namespace cAlgo.Indicators
{
    public enum MarketRegime
    {
        Unknown,
        Uptrend,
        Downtrend,
        Pullback,
        Transition,
        Chop
    }

    public sealed class RegimeState
    {
        public MarketRegime Regime { get; set; }
        public bool AllowLongContinuation { get; set; }
        public bool AllowShortContinuation { get; set; }
        public double TrendStrength { get; set; }
        public double NormalizedMomentum { get; set; }
        public double VolatilityPerBarPips { get; set; }
        public double SessionActivityRatio { get; set; }
    }

    public static class RegimeStateEngine
    {
        public static RegimeState Evaluate(
            double momentum,
            bool bosUp,
            bool bosDown,
            double htfFast,
            double htfSlow,
            double chopThreshold,
            MarketRegime previousRegime,
            double volatilityPerBarPips,
            double sessionActivityRatio,
            double hysteresisFraction)
        {
            double safeVolatility = Math.Max(0.1, volatilityPerBarPips);
            double safeSession = Clamp(sessionActivityRatio, 0.25, 4.0);
            double sessionWeight = Clamp(safeSession, 0.6, 1.4);
            double normalizedMomentum = (momentum / safeVolatility) * sessionWeight;

            bool htfBull = htfFast > htfSlow;
            bool htfBear = htfFast < htfSlow;
            double absMomentum = Math.Abs(normalizedMomentum);
            double hysteresis = Clamp(hysteresisFraction, 0.0, 0.5);
            double enterChopThreshold = chopThreshold * (1.0 - hysteresis);
            double exitChopThreshold = chopThreshold * (1.0 + hysteresis);

            var state = new RegimeState
            {
                Regime = MarketRegime.Unknown,
                AllowLongContinuation = false,
                AllowShortContinuation = false,
                TrendStrength = absMomentum,
                NormalizedMomentum = normalizedMomentum,
                VolatilityPerBarPips = safeVolatility,
                SessionActivityRatio = safeSession
            };

            bool noStructureBreak = !bosUp && !bosDown;
            bool stayInChop = previousRegime == MarketRegime.Chop && absMomentum < exitChopThreshold && noStructureBreak;
            bool enterChop = absMomentum < enterChopThreshold && noStructureBreak;

            if (stayInChop || enterChop)
            {
                state.Regime = MarketRegime.Chop;
                return state;
            }

            if ((bosUp || normalizedMomentum > 0) && htfBull)
            {
                state.Regime = bosUp ? MarketRegime.Uptrend : MarketRegime.Pullback;
                state.AllowLongContinuation = true;
                return state;
            }

            if ((bosDown || normalizedMomentum < 0) && htfBear)
            {
                state.Regime = bosDown ? MarketRegime.Downtrend : MarketRegime.Pullback;
                state.AllowShortContinuation = true;
                return state;
            }

            state.Regime = MarketRegime.Transition;
            state.AllowLongContinuation = htfBull && normalizedMomentum > 0 && absMomentum >= exitChopThreshold;
            state.AllowShortContinuation = htfBear && normalizedMomentum < 0 && absMomentum >= exitChopThreshold;
            return state;
        }

        private static double Clamp(double value, double min, double max)
        {
            if (value < min)
                return min;
            if (value > max)
                return max;
            return value;
        }
    }
}
