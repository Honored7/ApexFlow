using System;

namespace cAlgo.Indicators
{
    /// <summary>
    /// Post-entry position management:
    ///  - ATR Chandelier Exit trailing stop
    ///  - Breakeven move at configurable R-multiple
    ///  - Partial close at configurable R-multiple
    ///  - Structure-based trail (move SL behind swing points)
    /// 
    /// All methods are pure calculations — the bot calls them and applies
    /// the resulting SL/volume changes via cTrader API.
    /// </summary>
    public enum TrailMode
    {
        /// <summary>Chandelier Exit: price ± ATR × multiplier</summary>
        ChandelierExit,

        /// <summary>Trail behind swing points (order blocks / structure)</summary>
        StructureTrail,

        /// <summary>Step trail: move SL in fixed ATR increments</summary>
        StepTrail,

        /// <summary>No trailing, just breakeven + partial close</summary>
        BreakevenOnly
    }

    public sealed class TrailAction
    {
        /// <summary>New SL price (NaN = no change)</summary>
        public double NewStopLoss { get; set; } = double.NaN;

        /// <summary>Whether to trigger a partial close</summary>
        public bool TriggerPartialClose { get; set; }

        /// <summary>Fraction of position to close (e.g. 0.5)</summary>
        public double PartialCloseFraction { get; set; }

        /// <summary>Description for logging</summary>
        public string Reason { get; set; } = "";
    }

    public sealed class TrailingStopEngine
    {
        // Configuration
        public TrailMode Mode { get; set; } = TrailMode.ChandelierExit;
        public double ChandelierMultiplier { get; set; } = 2.5;
        public double StepMultiplier { get; set; } = 1.0; // for StepTrail
        public double BreakevenAtR { get; set; } = 0.6;
        public double PartialCloseAtR { get; set; } = 1.0;
        public double PartialCloseFraction { get; set; } = 0.5;

        // Per-position state tracking
        // These are set once when the trade opens, then updated
        private bool _breakevenApplied;
        private bool _partialClosed;
        private double _highestSinceFill;
        private double _lowestSinceFill;
        private double _lastTrailSl;

        /// <summary>
        /// Reset state for a newly opened position.
        /// </summary>
        public void OnPositionOpened(double fillPrice, bool isBuy)
        {
            _breakevenApplied = false;
            _partialClosed = false;
            _highestSinceFill = fillPrice;
            _lowestSinceFill = fillPrice;
            _lastTrailSl = double.NaN;
        }

        /// <summary>
        /// Evaluate trailing logic on each bar close (or tick).
        /// </summary>
        /// <param name="isBuy">True for long, false for short</param>
        /// <param name="entryPrice">Original fill price</param>
        /// <param name="currentSl">Current SL of the position</param>
        /// <param name="currentPrice">Current bid (sell) or ask (buy) — use close for bar-based</param>
        /// <param name="atrPips">Current ATR in pips</param>
        /// <param name="pipSize">Instrument pip size</param>
        /// <param name="slDistancePips">Original SL distance in pips (for R calculation)</param>
        /// <param name="nearestSwingLow">Nearest swing low for structure trail (buy)</param>
        /// <param name="nearestSwingHigh">Nearest swing high for structure trail (sell)</param>
        /// <returns>TrailAction describing what changes to make</returns>
        public TrailAction Evaluate(
            bool isBuy,
            double entryPrice,
            double currentSl,
            double currentPrice,
            double atrPips,
            double pipSize,
            double slDistancePips,
            double nearestSwingLow = double.NaN,
            double nearestSwingHigh = double.NaN)
        {
            var action = new TrailAction();

            // Track extremes
            if (isBuy)
                _highestSinceFill = Math.Max(_highestSinceFill, currentPrice);
            else
                _lowestSinceFill = Math.Min(_lowestSinceFill, currentPrice);

            double pricePips = pipSize > 0 ? pipSize : 0.0001;
            double currentPnlPips = isBuy
                ? (currentPrice - entryPrice) / pricePips
                : (entryPrice - currentPrice) / pricePips;
            double currentR = slDistancePips > 0 ? currentPnlPips / slDistancePips : 0;

            // 1) Partial close check
            if (!_partialClosed && currentR >= PartialCloseAtR)
            {
                _partialClosed = true;
                action.TriggerPartialClose = true;
                action.PartialCloseFraction = PartialCloseFraction;
                action.Reason = $"Partial close {PartialCloseFraction:P0} at {currentR:F1}R";
            }

            // 2) Breakeven check
            if (!_breakevenApplied && currentR >= BreakevenAtR)
            {
                _breakevenApplied = true;
                // Move SL to entry + small buffer (2 pips)
                double bePrice = isBuy
                    ? entryPrice + 2 * pricePips
                    : entryPrice - 2 * pricePips;

                // Only move if better than current SL
                bool better = isBuy
                    ? (double.IsNaN(currentSl) || bePrice > currentSl)
                    : (double.IsNaN(currentSl) || bePrice < currentSl);

                if (better)
                {
                    action.NewStopLoss = bePrice;
                    action.Reason += (action.Reason.Length > 0 ? " + " : "") + "Breakeven";
                    _lastTrailSl = bePrice;
                    return action; // Don't apply trail on same bar as BE
                }
            }

            // 3) Trailing stop (only after breakeven)
            if (_breakevenApplied)
            {
                double trailSl = double.NaN;

                switch (Mode)
                {
                    case TrailMode.ChandelierExit:
                        trailSl = isBuy
                            ? _highestSinceFill - atrPips * ChandelierMultiplier * pricePips
                            : _lowestSinceFill + atrPips * ChandelierMultiplier * pricePips;
                        break;

                    case TrailMode.StepTrail:
                        double stepSize = atrPips * StepMultiplier * pricePips;
                        if (!double.IsNaN(_lastTrailSl))
                        {
                            if (isBuy && currentPrice > _lastTrailSl + 2 * stepSize)
                                trailSl = _lastTrailSl + stepSize;
                            else if (!isBuy && currentPrice < _lastTrailSl - 2 * stepSize)
                                trailSl = _lastTrailSl - stepSize;
                        }
                        else
                        {
                            trailSl = isBuy
                                ? currentPrice - stepSize
                                : currentPrice + stepSize;
                        }
                        break;

                    case TrailMode.StructureTrail:
                        if (isBuy && !double.IsNaN(nearestSwingLow))
                        {
                            // Trail behind the most recent swing low
                            double structureSl = nearestSwingLow - 2 * pricePips;
                            trailSl = structureSl;
                        }
                        else if (!isBuy && !double.IsNaN(nearestSwingHigh))
                        {
                            double structureSl = nearestSwingHigh + 2 * pricePips;
                            trailSl = structureSl;
                        }
                        break;

                    case TrailMode.BreakevenOnly:
                        // No trailing beyond breakeven
                        break;
                }

                // Only move SL in favorable direction
                if (!double.IsNaN(trailSl))
                {
                    bool isBetter = isBuy
                        ? (double.IsNaN(currentSl) || trailSl > currentSl)
                        : (double.IsNaN(currentSl) || trailSl < currentSl);

                    if (isBetter)
                    {
                        action.NewStopLoss = trailSl;
                        action.Reason += (action.Reason.Length > 0 ? " + " : "")
                            + $"Trail ({Mode})";
                        _lastTrailSl = trailSl;
                    }
                }
            }

            return action;
        }
    }
}
