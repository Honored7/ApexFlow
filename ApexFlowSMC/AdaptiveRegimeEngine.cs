using System;

namespace cAlgo.Indicators
{
    /// <summary>
    /// Regime classification using ADX for trend strength and Donchian channels
    /// for trend direction. Replaces the old momentum-based RegimeStateEngine.
    /// 
    /// Regimes:
    ///   StrongTrend — ADX >= 30, Donchian breakout confirmed. Use trend following.
    ///   Trend       — ADX >= 25. Use trend following with tighter filters.
    ///   Range       — ADX &lt; 20, price inside Donchian band. Use mean reversion.
    ///   Choppy      — ADX &lt; 15, contracting ATR. No trade.
    /// </summary>
    public enum AdaptiveRegime
    {
        Choppy,
        Range,
        Trend,
        StrongTrend
    }

    public enum StrategyMode
    {
        NoTrade,
        MeanReversion,
        TrendFollowing
    }

    public sealed class AdaptiveRegimeState
    {
        public AdaptiveRegime Regime { get; set; }
        public StrategyMode Strategy { get; set; }
        public StructureDirection TrendDirection { get; set; }
        public double AdxValue { get; set; }
        public double DonchianHigh { get; set; }
        public double DonchianLow { get; set; }
        public double DonchianMid { get; set; }
        public bool HtfBullish { get; set; }
        public bool HtfBearish { get; set; }
        public double TrendStrength { get; set; }
        public bool AllowLong { get; set; }
        public bool AllowShort { get; set; }
    }

    /// <summary>
    /// ADX calculation state. Since cTrader's built-in ADX may not be available
    /// in all contexts (e.g., linked source files), we provide a self-contained
    /// implementation using Wilder's smoothing.
    /// </summary>
    public sealed class AdxCalculator
    {
        private readonly int _period;
        private double _smoothedPlusDM;
        private double _smoothedMinusDM;
        private double _smoothedTR;
        private double _smoothedAdx;
        private int _barCount;
        private double _prevHigh;
        private double _prevLow;
        private double _prevClose;
        private bool _initialized;

        // Buffers for initial averaging
        private double _sumPlusDM;
        private double _sumMinusDM;
        private double _sumTR;
        private double _sumDX;
        private int _dxCount;

        public double Value { get; private set; }
        public double PlusDI { get; private set; }
        public double MinusDI { get; private set; }

        public AdxCalculator(int period = 14)
        {
            _period = Math.Max(2, period);
        }

        public double Update(double high, double low, double close)
        {
            _barCount++;

            if (_barCount == 1)
            {
                _prevHigh = high;
                _prevLow = low;
                _prevClose = close;
                Value = 0;
                return Value;
            }

            double plusDM = high - _prevHigh;
            double minusDM = _prevLow - low;

            if (plusDM < 0) plusDM = 0;
            if (minusDM < 0) minusDM = 0;
            if (plusDM > minusDM) minusDM = 0;
            else if (minusDM > plusDM) plusDM = 0;
            else { plusDM = 0; minusDM = 0; }

            double tr = Math.Max(high - low,
                        Math.Max(Math.Abs(high - _prevClose), Math.Abs(low - _prevClose)));

            if (_barCount <= _period)
            {
                _sumPlusDM += plusDM;
                _sumMinusDM += minusDM;
                _sumTR += tr;

                if (_barCount == _period)
                {
                    _smoothedPlusDM = _sumPlusDM;
                    _smoothedMinusDM = _sumMinusDM;
                    _smoothedTR = _sumTR;
                    _initialized = true;

                    PlusDI = _smoothedTR > 0 ? (_smoothedPlusDM / _smoothedTR) * 100 : 0;
                    MinusDI = _smoothedTR > 0 ? (_smoothedMinusDM / _smoothedTR) * 100 : 0;
                    double sumDI = PlusDI + MinusDI;
                    double dx = sumDI > 0 ? Math.Abs(PlusDI - MinusDI) / sumDI * 100 : 0;
                    _sumDX += dx;
                    _dxCount++;
                }
            }
            else if (_initialized)
            {
                _smoothedPlusDM = _smoothedPlusDM - (_smoothedPlusDM / _period) + plusDM;
                _smoothedMinusDM = _smoothedMinusDM - (_smoothedMinusDM / _period) + minusDM;
                _smoothedTR = _smoothedTR - (_smoothedTR / _period) + tr;

                PlusDI = _smoothedTR > 0 ? (_smoothedPlusDM / _smoothedTR) * 100 : 0;
                MinusDI = _smoothedTR > 0 ? (_smoothedMinusDM / _smoothedTR) * 100 : 0;
                double sumDI = PlusDI + MinusDI;
                double dx = sumDI > 0 ? Math.Abs(PlusDI - MinusDI) / sumDI * 100 : 0;

                if (_dxCount < _period)
                {
                    _sumDX += dx;
                    _dxCount++;
                    if (_dxCount == _period)
                        _smoothedAdx = _sumDX / _period;
                }
                else
                {
                    _smoothedAdx = ((_smoothedAdx * (_period - 1)) + dx) / _period;
                }

                Value = _smoothedAdx;
            }

            _prevHigh = high;
            _prevLow = low;
            _prevClose = close;
            return Value;
        }
    }

    /// <summary>
    /// Donchian Channel calculator.
    /// </summary>
    public sealed class DonchianCalculator
    {
        private readonly int _period;
        private readonly Queue<double> _highs = new Queue<double>();
        private readonly Queue<double> _lows = new Queue<double>();

        // Use System.Collections.Generic Queue
        private sealed class Queue<T>
        {
            private readonly System.Collections.Generic.List<T> _items = new System.Collections.Generic.List<T>();
            public int Count => _items.Count;
            public void Enqueue(T item) => _items.Add(item);
            public void Dequeue() { if (_items.Count > 0) _items.RemoveAt(0); }
            public T Max()
            {
                if (_items.Count == 0) return default;
                T max = _items[0];
                for (int i = 1; i < _items.Count; i++)
                    if (System.Collections.Generic.Comparer<T>.Default.Compare(_items[i], max) > 0)
                        max = _items[i];
                return max;
            }
            public T Min()
            {
                if (_items.Count == 0) return default;
                T min = _items[0];
                for (int i = 1; i < _items.Count; i++)
                    if (System.Collections.Generic.Comparer<T>.Default.Compare(_items[i], min) < 0)
                        min = _items[i];
                return min;
            }
        }

        public double UpperBand { get; private set; }
        public double LowerBand { get; private set; }
        public double MidBand { get; private set; }

        public DonchianCalculator(int period = 20)
        {
            _period = Math.Max(2, period);
        }

        public void Update(double high, double low)
        {
            _highs.Enqueue(high);
            _lows.Enqueue(low);
            while (_highs.Count > _period) _highs.Dequeue();
            while (_lows.Count > _period) _lows.Dequeue();

            UpperBand = _highs.Max();
            LowerBand = _lows.Min();
            MidBand = (UpperBand + LowerBand) / 2.0;
        }

        public bool IsReady => _highs.Count >= _period;
    }

    /// <summary>
    /// Simple Bollinger Band calculator for mean reversion mode.
    /// </summary>
    public sealed class BollingerCalculator
    {
        private readonly int _period;
        private readonly double _stdDevMultiplier;
        private readonly System.Collections.Generic.Queue<double> _prices = new System.Collections.Generic.Queue<double>();

        public double UpperBand { get; private set; }
        public double LowerBand { get; private set; }
        public double MiddleBand { get; private set; }

        public BollingerCalculator(int period = 20, double stdDevMultiplier = 2.0)
        {
            _period = Math.Max(2, period);
            _stdDevMultiplier = stdDevMultiplier;
        }

        public void Update(double close)
        {
            _prices.Enqueue(close);
            while (_prices.Count > _period) _prices.Dequeue();

            if (_prices.Count < _period)
            {
                MiddleBand = close;
                UpperBand = close;
                LowerBand = close;
                return;
            }

            double sum = 0;
            foreach (var p in _prices) sum += p;
            MiddleBand = sum / _prices.Count;

            double sumSq = 0;
            foreach (var p in _prices)
            {
                double diff = p - MiddleBand;
                sumSq += diff * diff;
            }
            double stdDev = Math.Sqrt(sumSq / _prices.Count);
            UpperBand = MiddleBand + _stdDevMultiplier * stdDev;
            LowerBand = MiddleBand - _stdDevMultiplier * stdDev;
        }

        public bool IsReady => _prices.Count >= _period;
    }

    /// <summary>
    /// Simple RSI calculator (Wilder's smoothing).
    /// </summary>
    public sealed class RsiCalculator
    {
        private readonly int _period;
        private double _avgGain;
        private double _avgLoss;
        private double _prevClose;
        private int _barCount;

        public double Value { get; private set; } = 50;

        public RsiCalculator(int period = 14)
        {
            _period = Math.Max(2, period);
        }

        public double Update(double close)
        {
            _barCount++;

            if (_barCount == 1)
            {
                _prevClose = close;
                return Value;
            }

            double change = close - _prevClose;
            double gain = change > 0 ? change : 0;
            double loss = change < 0 ? -change : 0;

            if (_barCount <= _period + 1)
            {
                _avgGain += gain;
                _avgLoss += loss;

                if (_barCount == _period + 1)
                {
                    _avgGain /= _period;
                    _avgLoss /= _period;
                }
            }
            else
            {
                _avgGain = (_avgGain * (_period - 1) + gain) / _period;
                _avgLoss = (_avgLoss * (_period - 1) + loss) / _period;
            }

            if (_barCount >= _period + 1)
            {
                if (_avgLoss == 0)
                    Value = 100;
                else
                {
                    double rs = _avgGain / _avgLoss;
                    Value = 100 - (100 / (1 + rs));
                }
            }

            _prevClose = close;
            return Value;
        }
    }

    // ── Adaptive Regime Engine ─────────────────────────────────────────
    public static class AdaptiveRegimeEngine
    {
        public static AdaptiveRegimeState Evaluate(
            double adxValue,
            double plusDI,
            double minusDI,
            double donchianHigh,
            double donchianLow,
            double donchianMid,
            double currentClose,
            double htfFast,
            double htfSlow,
            StructureDirection structureTrend)
        {
            bool htfBull = htfFast > htfSlow;
            bool htfBear = htfFast < htfSlow;

            // Determine trend direction from Donchian + DI
            StructureDirection donchianDirection;
            if (currentClose >= donchianHigh)
                donchianDirection = StructureDirection.Bullish;
            else if (currentClose <= donchianLow)
                donchianDirection = StructureDirection.Bearish;
            else if (plusDI > minusDI)
                donchianDirection = StructureDirection.Bullish;
            else if (minusDI > plusDI)
                donchianDirection = StructureDirection.Bearish;
            else
                donchianDirection = StructureDirection.None;

            // Combine structure trend with Donchian
            StructureDirection finalTrend = structureTrend != StructureDirection.None
                ? structureTrend
                : donchianDirection;

            // Classify regime
            AdaptiveRegime regime;
            StrategyMode strategy;

            if (adxValue < 15)
            {
                regime = AdaptiveRegime.Choppy;
                strategy = StrategyMode.NoTrade;
            }
            else if (adxValue < 25)
            {
                regime = AdaptiveRegime.Range;
                strategy = StrategyMode.MeanReversion;
            }
            else if (adxValue < 35)
            {
                regime = AdaptiveRegime.Trend;
                strategy = StrategyMode.TrendFollowing;
            }
            else
            {
                regime = AdaptiveRegime.StrongTrend;
                strategy = StrategyMode.TrendFollowing;
            }

            // Direction permissions
            bool allowLong = false;
            bool allowShort = false;

            switch (strategy)
            {
                case StrategyMode.TrendFollowing:
                    allowLong = (finalTrend == StructureDirection.Bullish) && (htfBull || !htfBear);
                    allowShort = (finalTrend == StructureDirection.Bearish) && (htfBear || !htfBull);
                    break;
                case StrategyMode.MeanReversion:
                    // Mean reversion can go both ways; HTF alignment is softer
                    allowLong = true;
                    allowShort = true;
                    break;
                case StrategyMode.NoTrade:
                    break;
            }

            return new AdaptiveRegimeState
            {
                Regime = regime,
                Strategy = strategy,
                TrendDirection = finalTrend,
                AdxValue = adxValue,
                DonchianHigh = donchianHigh,
                DonchianLow = donchianLow,
                DonchianMid = donchianMid,
                HtfBullish = htfBull,
                HtfBearish = htfBear,
                TrendStrength = adxValue,
                AllowLong = allowLong,
                AllowShort = allowShort
            };
        }
    }
}
