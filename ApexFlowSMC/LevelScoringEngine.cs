using System;
using System.Collections.Generic;

namespace cAlgo.Indicators
{
    public sealed class LevelScoreResult
    {
        public double PrimarySupport { get; set; }
        public double PrimaryResistance { get; set; }
        public double SupportScore { get; set; }
        public double ResistanceScore { get; set; }
        public string Source { get; set; }
    }

    public static class LevelScoringEngine
    {
        public static LevelScoreResult Evaluate(
            double currentPrice,
            double poc,
            IReadOnlyList<double> hvn,
            IReadOnlyList<double> lvn,
            IReadOnlyCollection<double> swingLevels,
            double pipSize)
        {
            var result = new LevelScoreResult
            {
                PrimarySupport = double.NaN,
                PrimaryResistance = double.NaN,
                SupportScore = 0,
                ResistanceScore = 0,
                Source = "None"
            };

            if (pipSize <= 0)
                return result;

            double nearestHvn = FindNearest(hvn, currentPrice);
            double nearestLvn = FindNearest(lvn, currentPrice);
            double nearestSwing = FindNearest(swingLevels, currentPrice);

            double hvnDistance = DistanceInPips(currentPrice, nearestHvn, pipSize);
            double lvnDistance = DistanceInPips(currentPrice, nearestLvn, pipSize);
            double swingDistance = DistanceInPips(currentPrice, nearestSwing, pipSize);
            double pocDistance = DistanceInPips(currentPrice, poc, pipSize);

            double hvnScore = ScoreByDistance(hvnDistance, 18, 1.0);
            double lvnScore = ScoreByDistance(lvnDistance, 14, 0.85);
            double swingScore = ScoreByDistance(swingDistance, 12, 0.7);
            double pocScore = ScoreByDistance(pocDistance, 20, 1.1);

            double volumeDominance = Math.Max(hvnScore, pocScore);
            double structureDominance = Math.Max(lvnScore, swingScore);

            if (volumeDominance >= structureDominance)
            {
                result.Source = "VolumeNodes";
                result.PrimarySupport = !double.IsNaN(nearestHvn) ? nearestHvn : poc;
                result.PrimaryResistance = !double.IsNaN(poc) ? poc : nearestHvn;
                result.SupportScore = Math.Max(hvnScore, pocScore);
                result.ResistanceScore = Math.Max(hvnScore, pocScore);
            }
            else
            {
                result.Source = "Structure";
                result.PrimarySupport = !double.IsNaN(nearestSwing) ? nearestSwing : nearestLvn;
                result.PrimaryResistance = !double.IsNaN(nearestLvn) ? nearestLvn : nearestSwing;
                result.SupportScore = Math.Max(swingScore, lvnScore);
                result.ResistanceScore = Math.Max(swingScore, lvnScore);
            }

            return result;
        }

        private static double FindNearest(IReadOnlyCollection<double> levels, double price)
        {
            if (levels == null || levels.Count == 0)
                return double.NaN;

            double nearest = double.NaN;
            double minDistance = double.MaxValue;
            foreach (var level in levels)
            {
                double distance = Math.Abs(level - price);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    nearest = level;
                }
            }

            return nearest;
        }

        private static double DistanceInPips(double a, double b, double pipSize)
        {
            if (double.IsNaN(a) || double.IsNaN(b) || pipSize <= 0)
                return double.MaxValue;
            return Math.Abs(a - b) / pipSize;
        }

        private static double ScoreByDistance(double distancePips, double decayPips, double weight)
        {
            if (distancePips == double.MaxValue)
                return 0;

            double normalized = Math.Max(0, 1.0 - distancePips / Math.Max(1, decayPips));
            return normalized * 100 * weight;
        }
    }
}
