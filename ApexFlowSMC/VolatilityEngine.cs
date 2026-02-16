using System;
using System.Collections.Generic;

namespace cAlgo.Indicators
{
    public enum VolatilityLevel { Low, Normal, High, Extreme }

    public sealed class VolatilitySnapshot
    {
        public double Atr { get; set; }
        public double AtrPips { get; set; }
        public VolatilityLevel Level { get; set; }
        public double Multiplier { get; set; }
        public double PercentileRank { get; set; }
        public double VolatilityPerBarPips { get; set; }
        public double SessionActivityRatio { get; set; }
    }

    /// <summary>
    /// Centralized volatility computation used by both indicator and bot.
    /// Replaces the duplicated UpdateRegimeNormalization logic.
    /// </summary>
    public sealed class VolatilityEngine
    {
        private readonly Queue<double> _atrHistory = new Queue<double>();
        private readonly Queue<double> _rangePips = new Queue<double>();
        private readonly Queue<double> _tickVolumes = new Queue<double>();
        private readonly int _percentileWindow;
        private readonly int _volatilityWindow;
        private readonly int _sessionWindow;

        public VolatilityEngine(int percentileWindow = 200, int volatilityWindow = 55, int sessionWindow = 90)
        {
            _percentileWindow = Math.Max(20, percentileWindow);
            _volatilityWindow = Math.Max(10, volatilityWindow);
            _sessionWindow = Math.Max(10, sessionWindow);
        }

        /// <summary>
        /// Call once per bar with the ATR value (in price), bar range, and tick volume.
        /// </summary>
        public VolatilitySnapshot Update(double atrPrice, double barHigh, double barLow, double tickVolume, double pipSize)
        {
            double safePip = Math.Max(pipSize, 0.0000001);
            double atrPips = atrPrice / safePip;

            // Track ATR history for percentile rank
            _atrHistory.Enqueue(atrPips);
            while (_atrHistory.Count > _percentileWindow)
                _atrHistory.Dequeue();

            // Track bar range for per-bar volatility
            double rangePips = Math.Max(barHigh - barLow, safePip) / safePip;
            _rangePips.Enqueue(rangePips);
            while (_rangePips.Count > _volatilityWindow)
                _rangePips.Dequeue();

            // Track tick volume for session activity
            _tickVolumes.Enqueue(tickVolume);
            while (_tickVolumes.Count > _sessionWindow)
                _tickVolumes.Dequeue();

            // Compute volatility per bar
            double rangeSum = 0;
            foreach (var v in _rangePips) rangeSum += v;
            double volatilityPerBarPips = _rangePips.Count > 0 ? rangeSum / _rangePips.Count : 1.0;

            // Compute session activity ratio
            double volSum = 0;
            foreach (var v in _tickVolumes) volSum += v;
            double avgVol = _tickVolumes.Count > 0 ? volSum / _tickVolumes.Count : Math.Max(1.0, tickVolume);
            double sessionActivityRatio = avgVol > 0 ? tickVolume / avgVol : 1.0;
            sessionActivityRatio = Math.Clamp(sessionActivityRatio, 0.25, 4.0);

            // Compute percentile rank
            double percentileRank = ComputePercentileRank(atrPips);

            // Classify volatility level
            VolatilityLevel level;
            double multiplier;
            if (percentileRank <= 0.25)
            {
                level = VolatilityLevel.Low;
                multiplier = 0.7;
            }
            else if (percentileRank <= 0.65)
            {
                level = VolatilityLevel.Normal;
                multiplier = 1.0;
            }
            else if (percentileRank <= 0.90)
            {
                level = VolatilityLevel.High;
                multiplier = 1.3;
            }
            else
            {
                level = VolatilityLevel.Extreme;
                multiplier = 1.8;
            }

            return new VolatilitySnapshot
            {
                Atr = atrPrice,
                AtrPips = atrPips,
                Level = level,
                Multiplier = multiplier,
                PercentileRank = percentileRank,
                VolatilityPerBarPips = volatilityPerBarPips,
                SessionActivityRatio = sessionActivityRatio
            };
        }

        private double ComputePercentileRank(double currentAtr)
        {
            if (_atrHistory.Count < 5)
                return 0.5; // neutral when insufficient data

            int count = 0;
            int below = 0;
            foreach (var value in _atrHistory)
            {
                count++;
                if (value < currentAtr) below++;
            }

            return count > 0 ? (double)below / count : 0.5;
        }

        /// <summary>
        /// Static helper for simple ATR value extraction.
        /// </summary>
        public static double GetAtrPips(double atrPrice, double pipSize)
        {
            if (double.IsNaN(atrPrice) || atrPrice <= 0 || pipSize <= 0)
                return 0;
            return atrPrice / pipSize;
        }
    }
}
