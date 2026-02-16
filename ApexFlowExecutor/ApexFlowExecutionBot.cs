using System;
using System.Collections.Generic;
using System.Linq;
using cAlgo.API;
using cAlgo.API.Internals;
using cAlgo.API.Indicators;
using cAlgo.Indicators;

namespace cAlgo.Robots
{
    [Robot(TimeZone = TimeZones.UTC, AccessRights = AccessRights.None)]
    public class ApexFlowExecutionBot : Robot
    {
        [Parameter("Enable Symbol Scanner", Group = "Execution", DefaultValue = true)]
        public bool EnableSymbolScanner { get; set; }

        [Parameter("Symbols CSV", Group = "Execution", DefaultValue = "EURUSD,GBPUSD,XAUUSD,US30")]
        public string SymbolsCsv { get; set; }

        [Parameter("Enable Auto Execution", Group = "Execution", DefaultValue = false)]
        public bool EnableAutoExecution { get; set; }

        [Parameter("Trade Label", Group = "Execution", DefaultValue = "ApexFlowExec")]
        public string TradeLabel { get; set; }

        [Parameter("Volume (Lots)", Group = "Execution", DefaultValue = 0.01, MinValue = 0.01, MaxValue = 100)]
        public double VolumeInLots { get; set; }

        [Parameter("Use Dynamic Sizing", Group = "Execution", DefaultValue = true)]
        public bool UseDynamicSizing { get; set; }

        [Parameter("Risk Per Trade (%)", Group = "Execution", DefaultValue = 0.25, MinValue = 0.05, MaxValue = 5.0)]
        public double RiskPerTradePercent { get; set; }

        [Parameter("Max Volume (Lots)", Group = "Execution", DefaultValue = 1.0, MinValue = 0.01, MaxValue = 100)]
        public double MaxVolumeLots { get; set; }

        [Parameter("Max Positions", Group = "Execution", DefaultValue = 1, MinValue = 1, MaxValue = 10)]
        public int MaxPositions { get; set; }

        [Parameter("Max Concurrent Symbols", Group = "Execution", DefaultValue = 1, MinValue = 1, MaxValue = 100)]
        public int MaxConcurrentSymbols { get; set; }

        [Parameter("Single Best Signal", Group = "Execution", DefaultValue = true)]
        public bool SingleBestSignalPerBar { get; set; }

        [Parameter("Enable Session Filter", Group = "Execution", DefaultValue = true)]
        public bool EnableSessionFilter { get; set; }

        [Parameter("Trade Asia", Group = "Execution", DefaultValue = true)]
        public bool TradeAsiaSession { get; set; }

        [Parameter("Trade London", Group = "Execution", DefaultValue = true)]
        public bool TradeLondonSession { get; set; }

        [Parameter("Trade New York", Group = "Execution", DefaultValue = true)]
        public bool TradeNewYorkSession { get; set; }

        [Parameter("Allow Long", Group = "Execution", DefaultValue = true)]
        public bool AllowLong { get; set; }

        [Parameter("Allow Short", Group = "Execution", DefaultValue = true)]
        public bool AllowShort { get; set; }

        [Parameter("Stop Loss (pips)", Group = "Risk", DefaultValue = 20, MinValue = 1, MaxValue = 1000)]
        public double StopLossPips { get; set; }

        [Parameter("Take Profit (pips)", Group = "Risk", DefaultValue = 35, MinValue = 1, MaxValue = 2000)]
        public double TakeProfitPips { get; set; }

        [Parameter("Use Adaptive Stops", Group = "Risk", DefaultValue = true)]
        public bool UseAdaptiveStops { get; set; }

        [Parameter("ATR Period", Group = "Risk", DefaultValue = 14, MinValue = 2, MaxValue = 200)]
        public int AtrPeriod { get; set; }

        [Parameter("SL ATR Mult", Group = "Risk", DefaultValue = 1.8, MinValue = 0.1, MaxValue = 20)]
        public double StopLossAtrMultiplier { get; set; }

        [Parameter("TP ATR Mult", Group = "Risk", DefaultValue = 2.8, MinValue = 0.1, MaxValue = 30)]
        public double TakeProfitAtrMultiplier { get; set; }

        [Parameter("Min SL (pips)", Group = "Risk", DefaultValue = 8, MinValue = 1, MaxValue = 1000)]
        public double MinStopLossPips { get; set; }

        [Parameter("Max SL (pips)", Group = "Risk", DefaultValue = 120, MinValue = 1, MaxValue = 5000)]
        public double MaxStopLossPips { get; set; }

        [Parameter("Min TP (pips)", Group = "Risk", DefaultValue = 12, MinValue = 1, MaxValue = 5000)]
        public double MinTakeProfitPips { get; set; }

        [Parameter("Max Spread (pips)", Group = "Risk", DefaultValue = 1.8, MinValue = 0.1, MaxValue = 20)]
        public double MaxSpreadPips { get; set; }

        [Parameter("Max Entry Deviation", Group = "Risk", DefaultValue = 0.8, MinValue = 0.1, MaxValue = 20)]
        public double MaxEntryDeviationPips { get; set; }

        [Parameter("Entry Retries", Group = "Risk", DefaultValue = 2, MinValue = 1, MaxValue = 5)]
        public int EntryRetryAttempts { get; set; }

        [Parameter("Spread Profile Window", Group = "Risk", DefaultValue = 80, MinValue = 10, MaxValue = 1000)]
        public int SpreadProfileWindow { get; set; }

        [Parameter("Max Spread/Avg Ratio", Group = "Risk", DefaultValue = 1.8, MinValue = 1.0, MaxValue = 10)]
        public double MaxSpreadToAverageRatio { get; set; }

        [Parameter("Max Daily Loss", Group = "Risk", DefaultValue = 100, MinValue = 1, MaxValue = 100000)]
        public double MaxDailyLoss { get; set; }

        [Parameter("Close On Daily Lock", Group = "Risk", DefaultValue = true)]
        public bool CloseOnDailyLock { get; set; }

        [Parameter("Max Open Risk (%)", Group = "Risk", DefaultValue = 1.0, MinValue = 0.1, MaxValue = 20)]
        public double MaxOpenRiskPercent { get; set; }

        [Parameter("Max Gross Exposure (Lots)", Group = "Risk", DefaultValue = 2.0, MinValue = 0.01, MaxValue = 500)]
        public double MaxGrossExposureLots { get; set; }

        [Parameter("Max Trades / Day", Group = "Risk", DefaultValue = 3, MinValue = 1, MaxValue = 200)]
        public int MaxTradesPerDay { get; set; }

        [Parameter("Enable Perf Guard", Group = "Risk", DefaultValue = true)]
        public bool EnablePerformanceGuard { get; set; }

        [Parameter("Guard Window Trades", Group = "Risk", DefaultValue = 20, MinValue = 5, MaxValue = 500)]
        public int PerformanceGuardWindowTrades { get; set; }

        [Parameter("Min Win Rate (%)", Group = "Risk", DefaultValue = 40.0, MinValue = 0.0, MaxValue = 100.0)]
        public double PerformanceMinWinRatePercent { get; set; }

        [Parameter("Max Window DD (%)", Group = "Risk", DefaultValue = 3.0, MinValue = 0.1, MaxValue = 100.0)]
        public double PerformanceMaxWindowDrawdownPercent { get; set; }

        [Parameter("Perf Guard Cooldown", Group = "Risk", DefaultValue = 30, MinValue = 1, MaxValue = 1000)]
        public int PerformanceGuardCooldownBars { get; set; }

        [Parameter("Signal Cooldown (bars)", Group = "Signals", DefaultValue = 8, MinValue = 1, MaxValue = 200)]
        public int SignalCooldownBars { get; set; }

        [Parameter("Swing Lookback", Group = "Signals", DefaultValue = 4, MinValue = 2, MaxValue = 20)]
        public int SwingLookback { get; set; }

        [Parameter("Momentum Period", Group = "Signals", DefaultValue = 8, MinValue = 3, MaxValue = 100)]
        public int MomentumPeriod { get; set; }

        [Parameter("Fast MA", Group = "Signals", DefaultValue = 20, MinValue = 2, MaxValue = 300)]
        public int FastMaPeriod { get; set; }

        [Parameter("Slow MA", Group = "Signals", DefaultValue = 50, MinValue = 5, MaxValue = 500)]
        public int SlowMaPeriod { get; set; }

        [Parameter("Use HTF Filter", Group = "Regime", DefaultValue = true)]
        public bool UseHigherTimeframeFilter { get; set; }

        [Parameter("HTF", Group = "Regime", DefaultValue = "Hour")]
        public TimeFrame HigherTimeframe { get; set; }

        [Parameter("HTF Fast MA", Group = "Regime", DefaultValue = 34, MinValue = 2, MaxValue = 300)]
        public int HigherTimeframeFastMaPeriod { get; set; }

        [Parameter("HTF Slow MA", Group = "Regime", DefaultValue = 89, MinValue = 5, MaxValue = 500)]
        public int HigherTimeframeSlowMaPeriod { get; set; }

        [Parameter("Regime Chop Thresh", Group = "Regime", DefaultValue = 0.95, MinValue = 0.1, MaxValue = 20)]
        public double RegimeChopThreshold { get; set; }

        [Parameter("Regime Hysteresis", Group = "Regime", DefaultValue = 0.17, MinValue = 0, MaxValue = 0.5)]
        public double RegimeHysteresis { get; set; }

        [Parameter("Regime Vol Window", Group = "Regime", DefaultValue = 55, MinValue = 10, MaxValue = 500)]
        public int RegimeVolatilityWindow { get; set; }

        [Parameter("Regime Sess Window", Group = "Regime", DefaultValue = 90, MinValue = 10, MaxValue = 500)]
        public int RegimeSessionWindow { get; set; }

        [Parameter("Min Regime Strength", Group = "Regime", DefaultValue = 1.0, MinValue = 0.1, MaxValue = 20)]
        public double MinRegimeStrength { get; set; }

        [Parameter("Allow Pullback Entries", Group = "Regime", DefaultValue = false)]
        public bool AllowPullbackEntries { get; set; }

        private DateTime _currentTradingDate;
        private bool _dailyLockTriggered;
        private int _guardPauseBarsRemaining;
        private int _lastClosedTradesCountForGuard;
        private readonly Dictionary<string, SymbolContext> _symbolContexts = new Dictionary<string, SymbolContext>(StringComparer.OrdinalIgnoreCase);

        protected override void OnStart()
        {
            var symbolsToTrack = ResolveTradeSymbols();
            foreach (var symbol in symbolsToTrack)
            {
                var context = CreateSymbolContext(symbol);
                if (context != null)
                    _symbolContexts[symbol] = context;
            }

            if (_symbolContexts.Count == 0)
            {
                Print("No valid symbols resolved for scanner. Bot stopped.");
                Stop();
                return;
            }

            if (!TradeAsiaSession && !TradeLondonSession && !TradeNewYorkSession)
            {
                Print("All sessions are disabled. Bot stopped.");
                Stop();
                return;
            }

            _currentTradingDate = Server.TimeInUtc.Date;
            _dailyLockTriggered = false;
            _guardPauseBarsRemaining = 0;
            _lastClosedTradesCountForGuard = GetClosedTradesCount();

            Print("ApexFlowExecutionBot started. AutoExecution={0}, Symbols={1}, TF={2}", EnableAutoExecution, string.Join(",", _symbolContexts.Keys), TimeFrame);
        }

        protected override void OnBar()
        {
            HandleDayRollover();

            if (IsDailyLossLimitReached())
            {
                if (!_dailyLockTriggered)
                {
                    _dailyLockTriggered = true;
                    Print("Daily loss limit reached. Trading locked for {0:yyyy-MM-dd}", _currentTradingDate);

                    if (CloseOnDailyLock)
                        CloseBotPositions();
                }

                return;
            }

            if (_guardPauseBarsRemaining > 0)
            {
                _guardPauseBarsRemaining--;
                if (_guardPauseBarsRemaining == 0)
                    Print("Performance guard cooldown complete. Trading resumed.");
                return;
            }

            if (EnablePerformanceGuard && IsPerformanceGuardTriggered())
            {
                _guardPauseBarsRemaining = PerformanceGuardCooldownBars;
                Print("Performance guard activated. Pausing entries for {0} bars.", PerformanceGuardCooldownBars);
                return;
            }

            var candidates = new List<TradeCandidate>();
            foreach (var context in _symbolContexts.Values)
            {
                var candidate = EvaluateCandidate(context);
                if (candidate != null)
                    candidates.Add(candidate);
            }

            if (candidates.Count == 0)
                return;

            var orderedCandidates = candidates
                .OrderByDescending(c => c.Regime.TrendStrength)
                .ToList();

            if (SingleBestSignalPerBar)
            {
                var best = orderedCandidates[0];
                if (TryExecute(best.Context, best.TradeType, best.Regime, best.VolatilityPerBarPips, best.SessionActivityRatio, best.Index))
                    MarkLifecycle(best);
                return;
            }

            foreach (var candidate in orderedCandidates)
            {
                if (TryExecute(candidate.Context, candidate.TradeType, candidate.Regime, candidate.VolatilityPerBarPips, candidate.SessionActivityRatio, candidate.Index))
                    MarkLifecycle(candidate);
            }
        }

        private TradeCandidate EvaluateCandidate(SymbolContext context)
        {
            if (context.Bars == null)
                return null;

            if (!IsSessionAllowed(Server.TimeInUtc))
                return null;

            int minimumBars = Math.Max(SlowMaPeriod + 2, MomentumPeriod + 2);
            if (context.Bars.Count < minimumBars)
                return null;

            int index = context.Bars.Count - 1;
            DateTime barTime = context.Bars.OpenTimes[index];
            if (barTime == context.LastProcessedBarTime)
                return null;

            context.LastProcessedBarTime = barTime;
            context.LifecycleRule.MinBarsBetweenSameDirection = SignalCooldownBars;
            UpdateSpreadProfile(context);

            if (!IsSpreadTradeable(context))
                return null;

            UpdateSwingStructure(context, index);
            GetHtfValues(context, index, out double htfFast, out double htfSlow);
            UpdateRegimeNormalization(context, index, out double volatilityPerBarPips, out double sessionActivityRatio);

            bool bosUp = !double.IsNaN(context.LastSwingHigh) && context.Bars.ClosePrices[index] > context.LastSwingHigh;
            bool bosDown = !double.IsNaN(context.LastSwingLow) && context.Bars.ClosePrices[index] < context.LastSwingLow;
            double momentum = (context.Bars.ClosePrices[index] - context.Bars.ClosePrices[index - MomentumPeriod]) / (MomentumPeriod * context.Symbol.PipSize);

            var regime = RegimeStateEngine.Evaluate(
                momentum,
                bosUp,
                bosDown,
                htfFast,
                htfSlow,
                RegimeChopThreshold,
                context.PreviousRegime,
                volatilityPerBarPips,
                sessionActivityRatio,
                RegimeHysteresis);
            context.PreviousRegime = regime.Regime;

            bool fastAboveSlow = context.FastMa.Result[index] > context.SlowMa.Result[index];
            bool fastBelowSlow = context.FastMa.Result[index] < context.SlowMa.Result[index];
            bool strengthPass = regime.TrendStrength >= MinRegimeStrength;
            bool trendRegimePass = regime.Regime == MarketRegime.Uptrend || regime.Regime == MarketRegime.Downtrend;
            bool pullbackRegimePass = AllowPullbackEntries && regime.Regime == MarketRegime.Pullback;
            bool regimeTypePass = trendRegimePass || pullbackRegimePass;

            bool buySignal = AllowLong && strengthPass && regimeTypePass && regime.AllowLongContinuation && fastAboveSlow;
            bool sellSignal = AllowShort && strengthPass && regimeTypePass && regime.AllowShortContinuation && fastBelowSlow;

            if (buySignal && SignalLifecycleEngine.CanEmitBuy(index, context.LifecycleState, context.LifecycleRule))
            {
                return new TradeCandidate
                {
                    Context = context,
                    TradeType = TradeType.Buy,
                    Regime = regime,
                    VolatilityPerBarPips = volatilityPerBarPips,
                    SessionActivityRatio = sessionActivityRatio,
                    Index = index
                };
            }

            if (sellSignal && SignalLifecycleEngine.CanEmitSell(index, context.LifecycleState, context.LifecycleRule))
            {
                return new TradeCandidate
                {
                    Context = context,
                    TradeType = TradeType.Sell,
                    Regime = regime,
                    VolatilityPerBarPips = volatilityPerBarPips,
                    SessionActivityRatio = sessionActivityRatio,
                    Index = index
                };
            }

            return null;
        }

        private void MarkLifecycle(TradeCandidate candidate)
        {
            if (candidate.TradeType == TradeType.Buy)
                SignalLifecycleEngine.MarkBuy(candidate.Index, candidate.Context.LifecycleState);
            else
                SignalLifecycleEngine.MarkSell(candidate.Index, candidate.Context.LifecycleState);
        }

        private bool TryExecute(SymbolContext context, TradeType tradeType, RegimeState regime, double volatilityPerBarPips, double sessionActivityRatio, int index)
        {
            if (!EnableAutoExecution)
            {
                Print("Signal {0} {1} blocked: auto execution OFF. Regime={2}, Strength={3:0.00}, Vol={4:0.0}, Sess={5:0.00}",
                    context.SymbolName,
                    tradeType,
                    regime.Regime,
                    regime.TrendStrength,
                    volatilityPerBarPips,
                    sessionActivityRatio);
                return false;
            }

            UpdateSpreadProfile(context);
            if (!IsSpreadTradeable(context))
                return false;

            var openPositions = Positions.FindAll(TradeLabel, context.SymbolName);
            if (openPositions.Length >= MaxPositions)
                return false;

            if (GetTradesCountToday() >= MaxTradesPerDay)
                return false;

            if (!CanOpenOnSymbol(context.SymbolName))
                return false;

            ResolveStopsPips(context, index, out double stopLossPips, out double takeProfitPips);

            double volumeInUnits = GetRequestedVolumeInUnits(context.Symbol, stopLossPips);
            if (volumeInUnits <= 0)
                return false;

            if (!IsExposureWithinLimits(context.Symbol, volumeInUnits, stopLossPips))
                return false;

            double firstQuote = tradeType == TradeType.Buy ? context.Symbol.Ask : context.Symbol.Bid;
            TradeResult result = null;

            for (int attempt = 0; attempt < EntryRetryAttempts; attempt++)
            {
                double currentQuote = tradeType == TradeType.Buy ? context.Symbol.Ask : context.Symbol.Bid;
                double deviationPips = Math.Abs(currentQuote - firstQuote) / context.Symbol.PipSize;
                if (deviationPips > MaxEntryDeviationPips)
                    return false;

                result = ExecuteMarketOrder(tradeType, context.SymbolName, volumeInUnits, TradeLabel, stopLossPips, takeProfitPips);
                if (result.IsSuccessful)
                    break;

                if (IsNonRetriableError(result))
                    break;
            }

            if (result == null || !result.IsSuccessful)
            {
                Print("Order failed ({0} {1}): {2}", context.SymbolName, tradeType, result != null ? result.Error.ToString() : "Unknown");
                return false;
            }

            Print("Executed {0} {1} | Regime={2} | Strength={3:0.00} | Spread={4:0.00}",
                context.SymbolName,
                tradeType,
                regime.Regime,
                regime.TrendStrength,
                GetSpreadPips(context.Symbol));
            return true;
        }

        private void ResolveStopsPips(SymbolContext context, int index, out double stopLossPips, out double takeProfitPips)
        {
            stopLossPips = StopLossPips;
            takeProfitPips = TakeProfitPips;

            if (!UseAdaptiveStops || context.Atr == null)
                return;

            if (index < 0 || index >= context.Bars.Count)
                return;

            double atrPrice = context.Atr.Result[index];
            if (double.IsNaN(atrPrice) || atrPrice <= 0)
                return;

            double atrPips = atrPrice / context.Symbol.PipSize;
            if (double.IsNaN(atrPips) || atrPips <= 0)
                return;

            double adaptiveSl = atrPips * StopLossAtrMultiplier;
            adaptiveSl = Math.Max(MinStopLossPips, Math.Min(MaxStopLossPips, adaptiveSl));

            double adaptiveTp = atrPips * TakeProfitAtrMultiplier;
            adaptiveTp = Math.Max(MinTakeProfitPips, adaptiveTp);
            adaptiveTp = Math.Max(adaptiveTp, adaptiveSl * 1.1);

            stopLossPips = adaptiveSl;
            takeProfitPips = adaptiveTp;
        }

        private bool IsSessionAllowed(DateTime utcTime)
        {
            if (!EnableSessionFilter)
                return true;

            int hour = utcTime.Hour;
            bool asia = hour >= 0 && hour < 8;
            bool london = hour >= 7 && hour < 16;
            bool newYork = hour >= 13 && hour < 22;

            if (TradeAsiaSession && asia)
                return true;
            if (TradeLondonSession && london)
                return true;
            if (TradeNewYorkSession && newYork)
                return true;

            return false;
        }

        private void UpdateSpreadProfile(SymbolContext context)
        {
            double spreadPips = GetSpreadPips(context.Symbol);
            context.RecentSpreadsPips.Enqueue(spreadPips);
            while (context.RecentSpreadsPips.Count > SpreadProfileWindow)
                context.RecentSpreadsPips.Dequeue();

            double sum = 0;
            foreach (var value in context.RecentSpreadsPips)
                sum += value;

            context.AverageSpreadPips = context.RecentSpreadsPips.Count > 0 ? sum / context.RecentSpreadsPips.Count : spreadPips;
        }

        private bool IsSpreadTradeable(SymbolContext context)
        {
            double spreadPips = GetSpreadPips(context.Symbol);
            if (spreadPips > MaxSpreadPips)
                return false;

            if (context.RecentSpreadsPips.Count < 8)
                return true;

            double average = Math.Max(0.01, context.AverageSpreadPips);
            return spreadPips <= average * MaxSpreadToAverageRatio;
        }

        private bool IsNonRetriableError(TradeResult result)
        {
            if (result == null)
                return false;

            var code = result.Error;
            return code == ErrorCode.NoMoney ||
                   code == ErrorCode.BadVolume ||
                   code == ErrorCode.MarketClosed ||
                   code == ErrorCode.TechnicalError ||
                   code == ErrorCode.Disconnected;
        }

        private double GetRequestedVolumeInUnits(Symbol symbol, double stopLossPips)
        {
            double requestedVolumeInUnits;

            if (UseDynamicSizing)
            {
                double equity = Math.Max(0, Account.Equity);
                double riskAmount = equity * (RiskPerTradePercent / 100.0);
                requestedVolumeInUnits = symbol.VolumeForFixedRisk(riskAmount, stopLossPips);
            }
            else
            {
                requestedVolumeInUnits = symbol.QuantityToVolumeInUnits(VolumeInLots);
            }

            double maxUnitsByParam = symbol.QuantityToVolumeInUnits(MaxVolumeLots);
            requestedVolumeInUnits = Math.Min(requestedVolumeInUnits, maxUnitsByParam);

            requestedVolumeInUnits = symbol.NormalizeVolumeInUnits(requestedVolumeInUnits, RoundingMode.Down);
            if (requestedVolumeInUnits < symbol.VolumeInUnitsMin)
                return 0;

            return requestedVolumeInUnits;
        }

        private bool IsExposureWithinLimits(Symbol newTradeSymbol, double newTradeVolumeInUnits, double stopLossPips)
        {
            var positions = Positions.Where(p => p.Label == TradeLabel).ToArray();

            double currentGrossLots = positions.Sum(p =>
            {
                var positionSymbol = Symbols.GetSymbol(p.SymbolName);
                return positionSymbol != null ? positionSymbol.VolumeInUnitsToQuantity(p.VolumeInUnits) : 0;
            });
            double incomingLots = newTradeSymbol.VolumeInUnitsToQuantity(newTradeVolumeInUnits);
            if (currentGrossLots + incomingLots > MaxGrossExposureLots)
                return false;

            double currentOpenRisk = positions.Sum(EstimatePositionRiskAmount);
            double incomingRisk = EstimateRiskAmount(newTradeSymbol, newTradeVolumeInUnits, stopLossPips);
            double maxAllowedRisk = Account.Equity * (MaxOpenRiskPercent / 100.0);

            return currentOpenRisk + incomingRisk <= maxAllowedRisk;
        }

        private double EstimatePositionRiskAmount(Position position)
        {
            var symbol = Symbols.GetSymbol(position.SymbolName);
            if (symbol == null)
                return 0;

            double stopDistancePips;
            if (position.StopLoss.HasValue)
                stopDistancePips = Math.Abs(position.EntryPrice - position.StopLoss.Value) / symbol.PipSize;
            else
                stopDistancePips = StopLossPips;

            return EstimateRiskAmount(symbol, position.VolumeInUnits, stopDistancePips);
        }

        private double EstimateRiskAmount(Symbol symbol, double volumeInUnits, double stopDistancePips)
        {
            double lots = symbol.VolumeInUnitsToQuantity(volumeInUnits);
            double pipValuePerLot = symbol.PipValue;
            return Math.Max(0, stopDistancePips) * Math.Max(0, lots) * Math.Max(0, pipValuePerLot);
        }

        private bool CanOpenOnSymbol(string symbolName)
        {
            var activeSymbols = Positions
                .Where(p => p.Label == TradeLabel)
                .Select(p => p.SymbolName)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (activeSymbols.Contains(symbolName, StringComparer.OrdinalIgnoreCase))
                return true;

            return activeSymbols.Count < MaxConcurrentSymbols;
        }

        private int GetTradesCountToday()
        {
            return History.Count(t => t.Label == TradeLabel && t.ClosingTime.Date == _currentTradingDate);
        }

        private bool IsPerformanceGuardTriggered()
        {
            var closedTrades = History
                .Where(t => t.Label == TradeLabel)
                .OrderByDescending(t => t.ClosingTime)
                .ToList();

            int closedCount = closedTrades.Count;
            if (closedCount <= _lastClosedTradesCountForGuard)
                return false;

            _lastClosedTradesCountForGuard = closedCount;

            var recentTrades = closedTrades.Take(PerformanceGuardWindowTrades).ToList();

            if (recentTrades.Count < Math.Max(5, PerformanceGuardWindowTrades / 2))
                return false;

            int wins = recentTrades.Count(t => t.NetProfit > 0);
            double winRatePercent = (double)wins / recentTrades.Count * 100.0;

            double cumulative = 0;
            double peak = 0;
            double worstDrawdown = 0;

            foreach (var trade in recentTrades.OrderBy(t => t.ClosingTime))
            {
                cumulative += trade.NetProfit;
                if (cumulative > peak)
                    peak = cumulative;

                double drawdown = peak - cumulative;
                if (drawdown > worstDrawdown)
                    worstDrawdown = drawdown;
            }

            double equityBase = Math.Max(1.0, Account.Equity);
            double worstDrawdownPercent = worstDrawdown / equityBase * 100.0;

            return winRatePercent < PerformanceMinWinRatePercent || worstDrawdownPercent > PerformanceMaxWindowDrawdownPercent;
        }

        private int GetClosedTradesCount()
        {
            return History.Count(t => t.Label == TradeLabel);
        }

        private double GetSpreadPips(Symbol symbol)
        {
            return (symbol.Ask - symbol.Bid) / symbol.PipSize;
        }

        private void HandleDayRollover()
        {
            var today = Server.TimeInUtc.Date;
            if (today != _currentTradingDate)
            {
                _currentTradingDate = today;
                _dailyLockTriggered = false;
            }
        }

        private bool IsDailyLossLimitReached()
        {
            double closedPnl = History
                .Where(t => t.Label == TradeLabel && t.ClosingTime.Date == _currentTradingDate)
                .Sum(t => t.NetProfit);

            double floatingPnl = Positions
                .Where(p => p.Label == TradeLabel)
                .Sum(p => p.NetProfit);

            double totalPnl = closedPnl + floatingPnl;
            return totalPnl <= -Math.Abs(MaxDailyLoss);
        }

        private void CloseBotPositions()
        {
            var positions = Positions.Where(p => p.Label == TradeLabel).ToArray();
            foreach (var position in positions)
                ClosePosition(position);
        }

        private void UpdateSwingStructure(SymbolContext context, int index)
        {
            int swingIndex = index - SwingLookback;
            if (swingIndex <= SwingLookback || swingIndex + SwingLookback >= context.Bars.Count)
                return;

            if (IsSwingHigh(context.Bars, swingIndex))
                context.LastSwingHigh = context.Bars.HighPrices[swingIndex];

            if (IsSwingLow(context.Bars, swingIndex))
                context.LastSwingLow = context.Bars.LowPrices[swingIndex];
        }

        private bool IsSwingHigh(Bars bars, int index)
        {
            for (int i = 1; i <= SwingLookback; i++)
            {
                if (bars.HighPrices[index] <= bars.HighPrices[index - i] || bars.HighPrices[index] <= bars.HighPrices[index + i])
                    return false;
            }

            return true;
        }

        private bool IsSwingLow(Bars bars, int index)
        {
            for (int i = 1; i <= SwingLookback; i++)
            {
                if (bars.LowPrices[index] >= bars.LowPrices[index - i] || bars.LowPrices[index] >= bars.LowPrices[index + i])
                    return false;
            }

            return true;
        }

        private void GetHtfValues(SymbolContext context, int index, out double htfFast, out double htfSlow)
        {
            htfFast = context.Bars.ClosePrices[index];
            htfSlow = context.Bars.ClosePrices[index];

            if (!UseHigherTimeframeFilter || context.HigherTimeframeBars == null || context.HigherTimeframeFastMa == null || context.HigherTimeframeSlowMa == null)
                return;

            int htfIndex = context.HigherTimeframeBars.OpenTimes.GetIndexByTime(context.Bars.OpenTimes[index]);
            if (htfIndex < 0 || htfIndex >= context.HigherTimeframeBars.Count)
                return;

            htfFast = context.HigherTimeframeFastMa.Result[htfIndex];
            htfSlow = context.HigherTimeframeSlowMa.Result[htfIndex];
        }

        private void UpdateRegimeNormalization(SymbolContext context, int index, out double volatilityPerBarPips, out double sessionActivityRatio)
        {
            double rangePips = Math.Max(context.Bars.HighPrices[index] - context.Bars.LowPrices[index], context.Symbol.PipSize) / context.Symbol.PipSize;
            context.RecentRangePips.Enqueue(rangePips);
            while (context.RecentRangePips.Count > RegimeVolatilityWindow)
                context.RecentRangePips.Dequeue();

            double rangeSum = 0;
            foreach (var value in context.RecentRangePips)
                rangeSum += value;
            volatilityPerBarPips = context.RecentRangePips.Count > 0 ? rangeSum / context.RecentRangePips.Count : 1.0;

            double tickVolume = context.Bars.TickVolumes[index];
            context.RecentTickVolumes.Enqueue(tickVolume);
            while (context.RecentTickVolumes.Count > RegimeSessionWindow)
                context.RecentTickVolumes.Dequeue();

            double volumeSum = 0;
            foreach (var value in context.RecentTickVolumes)
                volumeSum += value;

            double averageVolume = context.RecentTickVolumes.Count > 0 ? volumeSum / context.RecentTickVolumes.Count : Math.Max(1.0, tickVolume);
            sessionActivityRatio = averageVolume > 0 ? tickVolume / averageVolume : 1.0;
            sessionActivityRatio = Math.Max(0.25, Math.Min(4.0, sessionActivityRatio));
        }

        private List<string> ResolveTradeSymbols()
        {
            if (!EnableSymbolScanner)
                return new List<string> { SymbolName };

            var parsed = (SymbolsCsv ?? string.Empty)
                .Split(new[] { ',', ';', ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(value => value.Trim())
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (parsed.Count == 0)
                parsed.Add(SymbolName);

            return parsed;
        }

        private SymbolContext CreateSymbolContext(string symbolName)
        {
            var symbol = Symbols.GetSymbol(symbolName);
            if (symbol == null)
            {
                Print("Symbol not found: {0}", symbolName);
                return null;
            }

            var bars = string.Equals(symbolName, SymbolName, StringComparison.OrdinalIgnoreCase)
                ? Bars
                : MarketData.GetBars(TimeFrame, symbolName);

            if (bars == null)
            {
                Print("Bars unavailable: {0}", symbolName);
                return null;
            }

            var context = new SymbolContext
            {
                SymbolName = symbolName,
                Symbol = symbol,
                Bars = bars,
                FastMa = Indicators.ExponentialMovingAverage(bars.ClosePrices, FastMaPeriod),
                SlowMa = Indicators.ExponentialMovingAverage(bars.ClosePrices, SlowMaPeriod),
                Atr = Indicators.AverageTrueRange(bars, AtrPeriod, MovingAverageType.Exponential),
                LifecycleState = new SignalLifecycleState(),
                LifecycleRule = new SignalLifecycleRule
                {
                    MinBarsBetweenSameDirection = SignalCooldownBars,
                    MaxSignalAgeBars = SignalCooldownBars * 2
                }
            };

            if (UseHigherTimeframeFilter)
            {
                context.HigherTimeframeBars = MarketData.GetBars(HigherTimeframe, symbolName);
                if (context.HigherTimeframeBars != null)
                {
                    context.HigherTimeframeFastMa = Indicators.ExponentialMovingAverage(context.HigherTimeframeBars.ClosePrices, HigherTimeframeFastMaPeriod);
                    context.HigherTimeframeSlowMa = Indicators.ExponentialMovingAverage(context.HigherTimeframeBars.ClosePrices, HigherTimeframeSlowMaPeriod);
                }
            }

            return context;
        }

        private sealed class SymbolContext
        {
            public string SymbolName { get; set; }
            public Symbol Symbol { get; set; }
            public Bars Bars { get; set; }
            public MovingAverage FastMa { get; set; }
            public MovingAverage SlowMa { get; set; }
            public AverageTrueRange Atr { get; set; }
            public Bars HigherTimeframeBars { get; set; }
            public MovingAverage HigherTimeframeFastMa { get; set; }
            public MovingAverage HigherTimeframeSlowMa { get; set; }
            public Queue<double> RecentRangePips { get; } = new Queue<double>();
            public Queue<double> RecentTickVolumes { get; } = new Queue<double>();
            public Queue<double> RecentSpreadsPips { get; } = new Queue<double>();
            public SignalLifecycleState LifecycleState { get; set; }
            public SignalLifecycleRule LifecycleRule { get; set; }
            public double LastSwingHigh { get; set; } = double.NaN;
            public double LastSwingLow { get; set; } = double.NaN;
            public MarketRegime PreviousRegime { get; set; } = MarketRegime.Unknown;
            public DateTime LastProcessedBarTime { get; set; } = DateTime.MinValue;
            public double AverageSpreadPips { get; set; }
        }

        private sealed class TradeCandidate
        {
            public SymbolContext Context { get; set; }
            public TradeType TradeType { get; set; }
            public RegimeState Regime { get; set; }
            public double VolatilityPerBarPips { get; set; }
            public double SessionActivityRatio { get; set; }
            public int Index { get; set; }
        }
    }
}
