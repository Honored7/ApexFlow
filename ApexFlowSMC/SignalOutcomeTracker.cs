using System;
using System.Collections.Generic;

namespace cAlgo.Indicators
{
    public enum SignalKind
    {
        BubbleBuy,
        BubbleSell,
        CpBuy,
        CpSell
    }

    public sealed class SignalOutcomeSnapshot
    {
        public int TotalResolved { get; set; }
        public int TotalWins { get; set; }
        public int TotalLosses { get; set; }
        public double WinRatePercent { get; set; }
        public int BubbleResolved { get; set; }
        public int BubbleWins { get; set; }
        public double BubbleWinRatePercent { get; set; }
        public int CpResolved { get; set; }
        public int CpWins { get; set; }
        public double CpWinRatePercent { get; set; }
    }

    public sealed class SignalOutcomeTracker
    {
        private sealed class PendingSignal
        {
            public int Index { get; set; }
            public double EntryPrice { get; set; }
            public SignalKind Kind { get; set; }
            public bool IsBullish { get; set; }
        }

        private readonly List<PendingSignal> _pendingSignals = new List<PendingSignal>();

        private int _totalResolved;
        private int _totalWins;
        private int _bubbleResolved;
        private int _bubbleWins;
        private int _cpResolved;
        private int _cpWins;

        public void TrackSignal(int index, double entryPrice, SignalKind kind)
        {
            _pendingSignals.Add(new PendingSignal
            {
                Index = index,
                EntryPrice = entryPrice,
                Kind = kind,
                IsBullish = kind == SignalKind.BubbleBuy || kind == SignalKind.CpBuy
            });
        }

        public void Update(int currentIndex, double closePrice, int horizonBars, double successPips, double pipSize)
        {
            if (horizonBars <= 0 || pipSize <= 0)
                return;

            for (int i = _pendingSignals.Count - 1; i >= 0; i--)
            {
                var signal = _pendingSignals[i];
                if (currentIndex - signal.Index < horizonBars)
                    continue;

                double signedMovePips = (closePrice - signal.EntryPrice) / pipSize;
                if (!signal.IsBullish)
                    signedMovePips *= -1;

                bool isWin = signedMovePips >= successPips;

                _totalResolved++;
                if (isWin)
                    _totalWins++;

                if (signal.Kind == SignalKind.BubbleBuy || signal.Kind == SignalKind.BubbleSell)
                {
                    _bubbleResolved++;
                    if (isWin)
                        _bubbleWins++;
                }
                else
                {
                    _cpResolved++;
                    if (isWin)
                        _cpWins++;
                }

                _pendingSignals.RemoveAt(i);
            }
        }

        public SignalOutcomeSnapshot GetSnapshot()
        {
            return new SignalOutcomeSnapshot
            {
                TotalResolved = _totalResolved,
                TotalWins = _totalWins,
                TotalLosses = Math.Max(0, _totalResolved - _totalWins),
                WinRatePercent = Percent(_totalWins, _totalResolved),
                BubbleResolved = _bubbleResolved,
                BubbleWins = _bubbleWins,
                BubbleWinRatePercent = Percent(_bubbleWins, _bubbleResolved),
                CpResolved = _cpResolved,
                CpWins = _cpWins,
                CpWinRatePercent = Percent(_cpWins, _cpResolved)
            };
        }

        private static double Percent(int wins, int total)
        {
            if (total <= 0)
                return 0;
            return 100.0 * wins / total;
        }
    }
}
