using System;
using System.Collections.Generic;

namespace cAlgo.Indicators
{
    public enum StrictTriggerMode
    {
        Off,
        LvnBreak,
        PocReclaim,
        LvnOrPocReclaim
    }

    public sealed class StrictEvaluationResult
    {
        public bool PassBuy { get; set; }
        public bool PassSell { get; set; }
        public string Context { get; set; }
    }

    public static class SignalReliabilityEngine
    {
        public static StrictEvaluationResult EvaluateStrictMode(
            StrictTriggerMode mode,
            double previousClose,
            double currentClose,
            double pocPrice,
            double nearestLvn,
            bool hasProfileData,
            double tolerancePrice,
            double minBreakMovePips,
            double pipSize)
        {
            var result = new StrictEvaluationResult { PassBuy = true, PassSell = true, Context = "Off" };

            if (mode == StrictTriggerMode.Off)
                return result;

            if (!hasProfileData)
                return new StrictEvaluationResult { PassBuy = false, PassSell = false, Context = "NoProfile" };

            double movePips = pipSize > 0 ? Math.Abs(currentClose - previousClose) / pipSize : 0;
            bool minMove = movePips >= minBreakMovePips;

            bool lvnBreakUp = !double.IsNaN(nearestLvn) && previousClose <= nearestLvn - tolerancePrice && currentClose >= nearestLvn + tolerancePrice;
            bool lvnBreakDown = !double.IsNaN(nearestLvn) && previousClose >= nearestLvn + tolerancePrice && currentClose <= nearestLvn - tolerancePrice;

            bool pocReclaimUp = !double.IsNaN(pocPrice) && previousClose < pocPrice - tolerancePrice && currentClose > pocPrice + tolerancePrice;
            bool pocReclaimDown = !double.IsNaN(pocPrice) && previousClose > pocPrice + tolerancePrice && currentClose < pocPrice - tolerancePrice;

            bool passBuy;
            bool passSell;
            string context;

            if (mode == StrictTriggerMode.LvnBreak)
            {
                passBuy = lvnBreakUp && minMove;
                passSell = lvnBreakDown && minMove;
                context = passBuy || passSell ? "LVNBreak" : "LVNBlock";
            }
            else if (mode == StrictTriggerMode.PocReclaim)
            {
                passBuy = pocReclaimUp && minMove;
                passSell = pocReclaimDown && minMove;
                context = passBuy || passSell ? "POCReclaim" : "POCBlock";
            }
            else
            {
                passBuy = (lvnBreakUp || pocReclaimUp) && minMove;
                passSell = (lvnBreakDown || pocReclaimDown) && minMove;
                context = passBuy || passSell ? "LVNorPOC" : "StrictBlock";
            }

            return new StrictEvaluationResult
            {
                PassBuy = passBuy,
                PassSell = passSell,
                Context = context
            };
        }

        public static int ComputeConfidence(
            double nodeParticipation,
            double bubbleMultiple,
            double depthImbalance,
            bool isDepthAvailable,
            bool htfAligned,
            bool strictPass,
            bool nearHvn,
            bool nearLvn)
        {
            double nodeScore = Clamp01(nodeParticipation) * 38.0;
            double impulseScore = Clamp01((bubbleMultiple - 1.0) / 1.2) * 26.0;

            double depthScore;
            if (!isDepthAvailable)
                depthScore = 8.0;
            else
                depthScore = Clamp01(Math.Abs(depthImbalance) / 0.25) * 16.0;

            double contextScore = (nearHvn || nearLvn) ? 8.0 : 0.0;
            double htfScore = htfAligned ? 8.0 : 0.0;
            double strictScore = strictPass ? 4.0 : 0.0;

            double total = nodeScore + impulseScore + depthScore + contextScore + htfScore + strictScore;
            total = Math.Max(0, Math.Min(100, total));
            return (int)Math.Round(total);
        }

        public static double FindNearestLevel(IReadOnlyList<double> levels, double price)
        {
            if (levels == null || levels.Count == 0)
                return double.NaN;

            double nearest = levels[0];
            double minDistance = Math.Abs(levels[0] - price);

            for (int i = 1; i < levels.Count; i++)
            {
                double distance = Math.Abs(levels[i] - price);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    nearest = levels[i];
                }
            }

            return nearest;
        }

        private static double Clamp01(double value)
        {
            if (value < 0)
                return 0;
            if (value > 1)
                return 1;
            return value;
        }
    }
}
