using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using cAlgo.API;
using cAlgo.API.Internals;
using cAlgo.API.Indicators;
using cAlgo.Indicators;

namespace cAlgo.Robots
{
    [Robot(TimeZone = TimeZones.UTC, AccessRights = AccessRights.FullAccess)]
    public class ApexFlowExecutionBot : Robot
    {
        // ── Execution ──────────────────────────────────────────────
        [Parameter("Enable Symbol Scanner", Group = "Execution", DefaultValue = true)]
        public bool EnableSymbolScanner { get; set; }

        [Parameter("Symbols CSV", Group = "Execution", DefaultValue = "EURUSD,GBPUSD,XAUUSD")]
        public string SymbolsCsv { get; set; }

        [Parameter("Enable Auto Execution", Group = "Execution", DefaultValue = false)]
        public bool EnableAutoExecution { get; set; }

        [Parameter("Trade Label", Group = "Execution", DefaultValue = "ApexFlowExec")]
        public string TradeLabel { get; set; }

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

        // ── Risk Profile ───────────────────────────────────────────
        [Parameter("Risk Profile", Group = "Risk", DefaultValue = 1)]
        public int RiskProfileIndex { get; set; }

        [Parameter("Custom Risk %", Group = "Risk", DefaultValue = 0.75, MinValue = 0.05, MaxValue = 5.0)]
        public double CustomRiskPercent { get; set; }

        [Parameter("Custom Max Trades/Day", Group = "Risk", DefaultValue = 8, MinValue = 1, MaxValue = 50)]
        public int CustomMaxTradesPerDay { get; set; }

        [Parameter("Custom Max DD %", Group = "Risk", DefaultValue = 2.5, MinValue = 0.5, MaxValue = 10)]
        public double CustomMaxDrawdownPct { get; set; }

        [Parameter("Custom Max Concurrent", Group = "Risk", DefaultValue = 4, MinValue = 1, MaxValue = 20)]
        public int CustomMaxConcurrent { get; set; }

        [Parameter("Max Gross Exposure (Lots)", Group = "Risk", DefaultValue = 2.0, MinValue = 0.01, MaxValue = 500)]
        public double MaxGrossExposureLots { get; set; }

        // ── Stops ──────────────────────────────────────────────────
        [Parameter("ATR Period", Group = "Stops", DefaultValue = 14, MinValue = 2, MaxValue = 200)]
        public int AtrPeriod { get; set; }

        [Parameter("SL ATR Multiplier", Group = "Stops", DefaultValue = 1.8, MinValue = 0.5, MaxValue = 10)]
        public double SlAtrMultiplier { get; set; }

        [Parameter("TP ATR Multiplier", Group = "Stops", DefaultValue = 5.0, MinValue = 0.5, MaxValue = 20)]
        public double TpAtrMultiplier { get; set; }

        [Parameter("MeanRev SL ATR Mult", Group = "Stops", DefaultValue = 1.2, MinValue = 0.3, MaxValue = 5)]
        public double MeanRevSlAtrMult { get; set; }

        [Parameter("MeanRev TP to Mid Band", Group = "Stops", DefaultValue = true)]
        public bool MeanRevTpToMidBand { get; set; }

        [Parameter("Min SL (pips)", Group = "Stops", DefaultValue = 5, MinValue = 1, MaxValue = 500)]
        public double MinSlPips { get; set; }

        [Parameter("Max SL (pips)", Group = "Stops", DefaultValue = 100, MinValue = 5, MaxValue = 5000)]
        public double MaxSlPips { get; set; }

        [Parameter("Min R:R", Group = "Stops", DefaultValue = 1.8, MinValue = 1.0, MaxValue = 10)]
        public double MinRiskReward { get; set; }

        // ── Trailing ───────────────────────────────────────────────
        [Parameter("Trail Mode (0=Chandelier 1=Structure 2=Step 3=BE only)", Group = "Trailing", DefaultValue = 0)]
        public int TrailModeIndex { get; set; }

        [Parameter("Chandelier Multiplier", Group = "Trailing", DefaultValue = 2.5, MinValue = 1.0, MaxValue = 6.0)]
        public double ChandelierMult { get; set; }

        [Parameter("Breakeven at R", Group = "Trailing", DefaultValue = 0.6, MinValue = 0.2, MaxValue = 3.0)]
        public double BreakevenAtR { get; set; }

        [Parameter("Partial Close %", Group = "Trailing", DefaultValue = 50, MinValue = 0, MaxValue = 100)]
        public int PartialClosePct { get; set; }

        [Parameter("Partial Close at R", Group = "Trailing", DefaultValue = 1.0, MinValue = 0.3, MaxValue = 5.0)]
        public double PartialCloseAtR { get; set; }

        // ── Regime / Signals ───────────────────────────────────────
        [Parameter("ADX Period", Group = "Regime", DefaultValue = 14, MinValue = 5, MaxValue = 50)]
        public int AdxPeriod { get; set; }

        [Parameter("Donchian Period", Group = "Regime", DefaultValue = 20, MinValue = 5, MaxValue = 100)]
        public int DonchianPeriod { get; set; }

        [Parameter("Bollinger Period", Group = "Regime", DefaultValue = 20, MinValue = 5, MaxValue = 100)]
        public int BollingerPeriod { get; set; }

        [Parameter("Bollinger StdDev", Group = "Regime", DefaultValue = 2.0, MinValue = 0.5, MaxValue = 4.0)]
        public double BollingerStdDev { get; set; }

        [Parameter("RSI Period", Group = "Regime", DefaultValue = 14, MinValue = 5, MaxValue = 50)]
        public int RsiPeriod { get; set; }

        [Parameter("Swing Lookback", Group = "Regime", DefaultValue = 5, MinValue = 2, MaxValue = 20)]
        public int SwingLookback { get; set; }

        [Parameter("Signal Cooldown (bars)", Group = "Regime", DefaultValue = 8, MinValue = 1, MaxValue = 100)]
        public int SignalCooldownBars { get; set; }

        [Parameter("Min Confluence", Group = "Regime", DefaultValue = 2.5, MinValue = 0.5, MaxValue = 10)]
        public double MinConfluence { get; set; }

        [Parameter("MeanRev Min Confluence", Group = "Regime", DefaultValue = 2.0, MinValue = 0.5, MaxValue = 10)]
        public double MeanRevMinConfluence { get; set; }

        [Parameter("Use HTF Filter", Group = "Regime", DefaultValue = true)]
        public bool UseHtfFilter { get; set; }

        [Parameter("HTF", Group = "Regime", DefaultValue = "Hour")]
        public TimeFrame HigherTimeframe { get; set; }

        [Parameter("HTF Fast MA", Group = "Regime", DefaultValue = 34, MinValue = 2, MaxValue = 300)]
        public int HtfFastMaPeriod { get; set; }

        [Parameter("HTF Slow MA", Group = "Regime", DefaultValue = 89, MinValue = 5, MaxValue = 500)]
        public int HtfSlowMaPeriod { get; set; }

        // ── Spread Filter ──────────────────────────────────────────
        [Parameter("Max Spread (pips)", Group = "Spread", DefaultValue = 3.0, MinValue = 0.1, MaxValue = 30)]
        public double MaxSpreadPips { get; set; }

        [Parameter("Spread Window", Group = "Spread", DefaultValue = 80, MinValue = 10, MaxValue = 1000)]
        public int SpreadProfileWindow { get; set; }

        [Parameter("Max Spread/Avg", Group = "Spread", DefaultValue = 2.0, MinValue = 1.0, MaxValue = 10)]
        public double MaxSpreadToAvgRatio { get; set; }

        [Parameter("Entry Retries", Group = "Spread", DefaultValue = 2, MinValue = 1, MaxValue = 5)]
        public int EntryRetryAttempts { get; set; }

        [Parameter("Max Entry Deviation (pips)", Group = "Spread", DefaultValue = 1.0, MinValue = 0.1, MaxValue = 20)]
        public double MaxEntryDeviationPips { get; set; }

        // ── Private state ──────────────────────────────────────────
        private RiskProfile _riskProfile;
        private TrailMode _trailMode;
        private DateTime _currentTradingDate;
        private bool _dailyLockTriggered;
        private double _startOfDayEquity;
        private double _startingBalance;
        private readonly Dictionary<string, SymbolContext> _symbolContexts =
            new Dictionary<string, SymbolContext>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<long, TrailingStopEngine> _trailEngines =
            new Dictionary<long, TrailingStopEngine>();
        private readonly Dictionary<long, double> _originalSlPips =
            new Dictionary<long, double>();

        // ════════════════════════════════════════════════════════════
        //  LIFECYCLE
        // ════════════════════════════════════════════════════════════

        protected override void OnStart()
        {
            // Resolve risk profile from integer parameter
            var profileType = (RiskProfileType)Math.Min(3, Math.Max(0, RiskProfileIndex));
            _riskProfile = RiskProfileEngine.GetProfile(profileType);
            if (profileType == RiskProfileType.Custom)
            {
                _riskProfile.RiskPerTrade = CustomRiskPercent / 100.0;
                _riskProfile.MaxTotalTradesPerDay = CustomMaxTradesPerDay;
                _riskProfile.MaxDailyDrawdownPct = CustomMaxDrawdownPct / 100.0;
                _riskProfile.MaxConcurrentPositions = CustomMaxConcurrent;
            }

            // Apply trailing params from UI
            _riskProfile.BreakevenAtR = BreakevenAtR;
            _riskProfile.PartialCloseFraction = PartialClosePct / 100.0;
            _riskProfile.PartialCloseAtR = PartialCloseAtR;
            _riskProfile.CooldownBars = SignalCooldownBars;

            // Resolve trail mode from integer parameter
            switch (TrailModeIndex)
            {
                case 0: _trailMode = TrailMode.ChandelierExit; break;
                case 1: _trailMode = TrailMode.StructureTrail; break;
                case 2: _trailMode = TrailMode.StepTrail; break;
                default: _trailMode = TrailMode.BreakevenOnly; break;
            }

            var symbolsToTrack = ResolveTradeSymbols();
            foreach (var sym in symbolsToTrack)
            {
                var ctx = CreateSymbolContext(sym);
                if (ctx != null)
                    _symbolContexts[sym] = ctx;
            }

            if (_symbolContexts.Count == 0)
            {
                Print("No valid symbols resolved. Bot stopped.");
                Stop();
                return;
            }

            if (EnableSessionFilter && !TradeAsiaSession && !TradeLondonSession && !TradeNewYorkSession)
            {
                Print("All sessions disabled. Bot stopped.");
                Stop();
                return;
            }

            _currentTradingDate = Server.TimeInUtc.Date;
            _dailyLockTriggered = false;
            _startOfDayEquity = Account.Equity;
            _startingBalance = Account.Balance;

            Positions.Opened += OnPositionOpened;
            Positions.Closed += OnPositionClosed;

            Print("ApexFlowBot v2 started | Profile={0} | Risk={1:P2} | Trail={2} | Symbols={3}",
                profileType, _riskProfile.RiskPerTrade, _trailMode, string.Join(",", _symbolContexts.Keys));
        }

        protected override void OnStop()
        {
            Positions.Opened -= OnPositionOpened;
            Positions.Closed -= OnPositionClosed;

            ExportResults();
        }

        private void ExportResults()
        {
            try
            {
                var closedTrades = History
                    .Where(t => t.Label == TradeLabel)
                    .OrderBy(t => t.ClosingTime)
                    .Select(t => new TradeRecord
                    {
                        EntryTime = t.EntryTime,
                        ExitTime = t.ClosingTime,
                        Symbol = t.SymbolName,
                        Direction = t.TradeType.ToString(),
                        Lots = t.Quantity,
                        EntryPrice = t.EntryPrice,
                        ExitPrice = t.ClosingPrice,
                        StopLoss = null, // HistoricalTrade has no SL/TP
                        TakeProfit = null,
                        Pips = t.Pips,
                        NetProfit = t.NetProfit,
                        GrossProfit = t.GrossProfit,
                        Commissions = t.Commissions,
                        Swap = t.Swap,
                        BalanceAfter = t.Balance,
                        Label = t.Label
                    })
                    .ToList();

                if (closedTrades.Count == 0)
                {
                    Print("[Export] No trades to export.");
                    return;
                }

                string csvPath = ResultsExporter.ExportTrades(
                    closedTrades, Account.Balance, Account.Equity, TradeLabel);

                string jsonPath = ResultsExporter.ExportSummary(
                    closedTrades, Account.Balance, Account.Equity, _startingBalance, TradeLabel);

                Print("[Export] CSV:  {0}", csvPath);
                Print("[Export] JSON: {0}", jsonPath);
            }
            catch (Exception ex)
            {
                Print("[Export] Failed: {0}", ex.Message);
            }
        }

        private void OnPositionOpened(PositionOpenedEventArgs args)
        {
            var pos = args.Position;
            if (pos.Label != TradeLabel) return;

            var engine = new TrailingStopEngine
            {
                Mode = _trailMode,
                ChandelierMultiplier = ChandelierMult,
                BreakevenAtR = _riskProfile.BreakevenAtR,
                PartialCloseAtR = _riskProfile.PartialCloseAtR,
                PartialCloseFraction = _riskProfile.PartialCloseFraction
            };
            engine.OnPositionOpened(pos.EntryPrice, pos.TradeType == TradeType.Buy);
            _trailEngines[pos.Id] = engine;

            if (pos.StopLoss.HasValue)
            {
                var sym = Symbols.GetSymbol(pos.SymbolName);
                if (sym != null)
                    _originalSlPips[pos.Id] = Math.Abs(pos.EntryPrice - pos.StopLoss.Value) / sym.PipSize;
            }
        }

        private void OnPositionClosed(PositionClosedEventArgs args)
        {
            _trailEngines.Remove(args.Position.Id);
            _originalSlPips.Remove(args.Position.Id);
        }

        // ════════════════════════════════════════════════════════════
        //  MAIN LOOP
        // ════════════════════════════════════════════════════════════

        protected override void OnBar()
        {
            HandleDayRollover();

            if (IsDailyDrawdownBreached())
            {
                if (!_dailyLockTriggered)
                {
                    _dailyLockTriggered = true;
                    Print("Daily DD limit ({0:P1}) reached. Locked for {1:yyyy-MM-dd}.",
                        _riskProfile.MaxDailyDrawdownPct, _currentTradingDate);
                    CloseBotPositions();
                }
                return;
            }

            ManageOpenPositions();

            var candidates = new List<TradeCandidate>();
            foreach (var ctx in _symbolContexts.Values)
            {
                var candidate = EvaluateCandidate(ctx);
                if (candidate != null)
                    candidates.Add(candidate);
            }

            if (candidates.Count == 0) return;

            var ordered = candidates.OrderByDescending(c => c.ConfluenceScore).ToList();

            if (SingleBestSignalPerBar)
            {
                var best = ordered[0];
                if (TryExecute(best))
                    MarkLifecycle(best);
            }
            else
            {
                foreach (var c in ordered)
                {
                    if (TryExecute(c))
                        MarkLifecycle(c);
                }
            }
        }

        // ════════════════════════════════════════════════════════════
        //  POSITION MANAGEMENT (trailing stops, partials, breakeven)
        // ════════════════════════════════════════════════════════════

        private void ManageOpenPositions()
        {
            var positions = Positions.Where(p => p.Label == TradeLabel).ToArray();
            foreach (var pos in positions)
            {
                if (!_trailEngines.TryGetValue(pos.Id, out var trail))
                    continue;

                var sym = Symbols.GetSymbol(pos.SymbolName);
                if (sym == null) continue;

                double atrPips = GetCurrentAtrPips(pos.SymbolName, sym);
                double origSlPips = _originalSlPips.ContainsKey(pos.Id) ? _originalSlPips[pos.Id] : 20;

                double nearSwingLow = double.NaN;
                double nearSwingHigh = double.NaN;
                if (_symbolContexts.TryGetValue(pos.SymbolName, out var ctx) && ctx.StructureState != null)
                {
                    foreach (var sp in ctx.StructureState.SwingLows)
                    {
                        if (sp.Price < pos.EntryPrice) { nearSwingLow = sp.Price; break; }
                    }
                    foreach (var sp in ctx.StructureState.SwingHighs)
                    {
                        if (sp.Price > pos.EntryPrice) { nearSwingHigh = sp.Price; break; }
                    }
                }

                bool isBuy = pos.TradeType == TradeType.Buy;
                double currentPrice = isBuy ? sym.Bid : sym.Ask;
                double currentSl = pos.StopLoss ?? double.NaN;

                var action = trail.Evaluate(
                    isBuy, pos.EntryPrice, currentSl, currentPrice,
                    atrPips, sym.PipSize, origSlPips,
                    nearSwingLow, nearSwingHigh);

                if (action.TriggerPartialClose && action.PartialCloseFraction > 0)
                {
                    double closeVolume = sym.NormalizeVolumeInUnits(
                        pos.VolumeInUnits * action.PartialCloseFraction, RoundingMode.Down);
                    if (closeVolume >= sym.VolumeInUnitsMin && closeVolume < pos.VolumeInUnits)
                    {
                        ClosePosition(pos, closeVolume);
                        Print("{0} {1}: {2}", pos.SymbolName, pos.TradeType, action.Reason);
                    }
                }

                if (!double.IsNaN(action.NewStopLoss))
                {
                    ModifyPosition(pos, action.NewStopLoss, pos.TakeProfit);
                    if (!string.IsNullOrEmpty(action.Reason) && !action.TriggerPartialClose)
                        Print("{0} {1}: SL -> {2:F5} ({3})", pos.SymbolName, pos.TradeType,
                            action.NewStopLoss, action.Reason);
                }
            }
        }

        private double GetCurrentAtrPips(string symbolName, Symbol sym)
        {
            if (_symbolContexts.TryGetValue(symbolName, out var ctx) && ctx.Atr != null)
            {
                int idx = ctx.Bars.Count - 1;
                if (idx >= 0)
                {
                    double atrVal = ctx.Atr.Result[idx];
                    if (!double.IsNaN(atrVal) && atrVal > 0)
                        return atrVal / sym.PipSize;
                }
            }
            return 10;
        }

        // ════════════════════════════════════════════════════════════
        //  SIGNAL GENERATION
        // ════════════════════════════════════════════════════════════

        private TradeCandidate EvaluateCandidate(SymbolContext ctx)
        {
            if (ctx.Bars == null || ctx.Bars.Count < 60) return null;
            if (!IsSessionAllowed(Server.TimeInUtc)) return null;

            int index = ctx.Bars.Count - 1;
            DateTime barTime = ctx.Bars.OpenTimes[index];
            if (barTime == ctx.LastProcessedBarTime) return null;
            ctx.LastProcessedBarTime = barTime;

            UpdateSpreadProfile(ctx);
            if (!IsSpreadTradeable(ctx)) return null;

            // ── Update all engines ──
            double high = ctx.Bars.HighPrices[index];
            double low = ctx.Bars.LowPrices[index];
            double close = ctx.Bars.ClosePrices[index];
            double open = ctx.Bars.OpenPrices[index];
            double prevClose = index > 0 ? ctx.Bars.ClosePrices[index - 1] : close;
            double tickVol = ctx.Bars.TickVolumes[index];

            double atrVal = ctx.Atr != null ? ctx.Atr.Result[index] : 0;
            if (double.IsNaN(atrVal)) atrVal = 0;
            double atrPips = atrVal > 0 ? atrVal / ctx.Symbol.PipSize : 10;

            ctx.AdxCalc.Update(high, low, close);
            ctx.DonchianCalc.Update(high, low);
            ctx.BollingerCalc.Update(close);
            ctx.RsiCalc.Update(close);
            ctx.VolEngine.Update(atrVal, high, low, tickVol, ctx.Symbol.PipSize);

            if (ctx.StructureState == null)
                ctx.StructureState = new MarketStructureState();
            MarketStructureEngine.Update(
                ctx.StructureState, index,
                i => ctx.Bars.OpenPrices[i], i => ctx.Bars.HighPrices[i],
                i => ctx.Bars.LowPrices[i], i => ctx.Bars.ClosePrices[i],
                SwingLookback, ctx.Symbol.PipSize, 2.0);

            GetHtfValues(ctx, index, out double htfFast, out double htfSlow);

            var regime = AdaptiveRegimeEngine.Evaluate(
                ctx.AdxCalc.Value, ctx.AdxCalc.PlusDI, ctx.AdxCalc.MinusDI,
                ctx.DonchianCalc.UpperBand, ctx.DonchianCalc.LowerBand, ctx.DonchianCalc.MidBand,
                close, htfFast, htfSlow, ctx.StructureState.PrevailingTrend);

            if (regime.Strategy == StrategyMode.NoTrade) return null;

            ctx.LifecycleRule.MinBarsBetweenSameDirection = _riskProfile.CooldownBars;

            // ── Entry logic by strategy mode ──
            TradeType? signal = null;
            double confluenceScore = 0;

            if (regime.Strategy == StrategyMode.TrendFollowing)
                signal = EvaluateTrendSignal(ctx, regime, close, high, low, atrPips, index, out confluenceScore);
            else if (regime.Strategy == StrategyMode.MeanReversion)
                signal = EvaluateMeanReversionSignal(ctx, regime, close, index, out confluenceScore);

            if (signal == null) return null;

            if (signal == TradeType.Buy && (!AllowLong || !regime.AllowLong)) return null;
            if (signal == TradeType.Sell && (!AllowShort || !regime.AllowShort)) return null;

            if (signal == TradeType.Buy && !SignalLifecycleEngine.CanEmitBuy(index, ctx.LifecycleState, ctx.LifecycleRule))
                return null;
            if (signal == TradeType.Sell && !SignalLifecycleEngine.CanEmitSell(index, ctx.LifecycleState, ctx.LifecycleRule))
                return null;

            return new TradeCandidate
            {
                Context = ctx,
                TradeType = signal.Value,
                Regime = regime,
                AtrPips = atrPips,
                ConfluenceScore = confluenceScore,
                Index = index,
                Strategy = regime.Strategy,
                BollingerMidBandDistance = ctx.BollingerCalc.IsReady
                    ? Math.Abs(close - ctx.BollingerCalc.MiddleBand) / ctx.Symbol.PipSize
                    : 0
            };
        }

        /// <summary>
        /// Trend Following: Donchian breakout + BOS + OB/FVG/sweep confluence.
        /// Requires structural confirmation — not just a crossover.
        /// </summary>
        private TradeType? EvaluateTrendSignal(
            SymbolContext ctx, AdaptiveRegimeState regime,
            double close, double high, double low, double atrPips,
            int index, out double confluenceScore)
        {
            confluenceScore = 0;
            var state = ctx.StructureState;
            if (state == null) return null;

            // Candle strength filter — reject indecisive bars
            double barRange = high - low;
            if (barRange <= 0) return null;
            double bodyRatio = (close - low) / barRange;  // 1.0 = close at high, 0.0 = close at low

            // ── LONG ──
            if (regime.TrendDirection == StructureDirection.Bullish)
            {
                // Require bullish candle: close in upper 40% of bar
                if (bodyRatio < 0.6) return null;

                bool recentBosUp = state.LastBreak != null
                    && state.LastBreak.Direction == StructureDirection.Bullish
                    && state.LastBreak.BarIndex >= index - 3;

                bool donchianBreakout = close >= ctx.DonchianCalc.UpperBand && ctx.DonchianCalc.IsReady;

                // Fresh breakout: require previous bar was inside the channel
                if (donchianBreakout && index > 0)
                {
                    double prevHigh = ctx.Bars.HighPrices[index - 1];
                    if (prevHigh >= ctx.DonchianCalc.UpperBand)
                        donchianBreakout = false; // not fresh, price was already above
                }

                if (!recentBosUp && !donchianBreakout) return null;

                confluenceScore = 1.0;
                if (recentBosUp) confluenceScore += 1.0;
                if (donchianBreakout) confluenceScore += 0.5;

                // Bullish order block (pullback into demand zone)
                foreach (var ob in state.ActiveOrderBlocks)
                {
                    if (!ob.Mitigated && ob.Direction == StructureDirection.Bullish && low <= ob.High && close > ob.Low)
                    {
                        confluenceScore += 1.5;
                        break;
                    }
                }

                // Bullish FVG (price filling gap from below)
                foreach (var fvg in state.ActiveFvgs)
                {
                    if (!fvg.Filled && fvg.Direction == StructureDirection.Bullish && low <= fvg.High && close > fvg.Low)
                    {
                        confluenceScore += 1.0;
                        break;
                    }
                }

                // Liquidity sweep below (stop hunt reversal)
                foreach (var sweep in state.RecentSweeps)
                {
                    if (sweep.Direction != StructureDirection.Bullish && sweep.BarIndex >= index - 5)
                    {
                        confluenceScore += 1.5;
                        break;
                    }
                }

                confluenceScore += Math.Min(1.0, regime.AdxValue / 50.0);

                if (confluenceScore < MinConfluence) return null;
                return TradeType.Buy;
            }

            // ── SHORT ──
            if (regime.TrendDirection == StructureDirection.Bearish)
            {
                // Require bearish candle: close in lower 40% of bar
                if (bodyRatio > 0.4) return null;

                bool recentBosDown = state.LastBreak != null
                    && state.LastBreak.Direction == StructureDirection.Bearish
                    && state.LastBreak.BarIndex >= index - 3;

                bool donchianBreakdown = close <= ctx.DonchianCalc.LowerBand && ctx.DonchianCalc.IsReady;

                // Fresh breakdown: require previous bar was inside the channel
                if (donchianBreakdown && index > 0)
                {
                    double prevLow = ctx.Bars.LowPrices[index - 1];
                    if (prevLow <= ctx.DonchianCalc.LowerBand)
                        donchianBreakdown = false; // not fresh
                }

                if (!recentBosDown && !donchianBreakdown) return null;

                confluenceScore = 1.0;
                if (recentBosDown) confluenceScore += 1.0;
                if (donchianBreakdown) confluenceScore += 0.5;

                foreach (var ob in state.ActiveOrderBlocks)
                {
                    if (!ob.Mitigated && ob.Direction == StructureDirection.Bearish && high >= ob.Low && close < ob.High)
                    {
                        confluenceScore += 1.5;
                        break;
                    }
                }

                foreach (var fvg in state.ActiveFvgs)
                {
                    if (!fvg.Filled && fvg.Direction == StructureDirection.Bearish && high >= fvg.Low && close < fvg.High)
                    {
                        confluenceScore += 1.0;
                        break;
                    }
                }

                foreach (var sweep in state.RecentSweeps)
                {
                    if (sweep.Direction == StructureDirection.Bullish && sweep.BarIndex >= index - 5)
                    {
                        confluenceScore += 1.5;
                        break;
                    }
                }

                confluenceScore += Math.Min(1.0, regime.AdxValue / 50.0);

                if (confluenceScore < MinConfluence) return null;
                return TradeType.Sell;
            }

            return null;
        }

        /// <summary>
        /// Mean Reversion: Bollinger Band extremes + RSI + optional OB support.
        /// Active only in ranging markets (ADX &lt; 20).
        /// </summary>
        private TradeType? EvaluateMeanReversionSignal(
            SymbolContext ctx, AdaptiveRegimeState regime,
            double close, int index, out double confluenceScore)
        {
            confluenceScore = 0;
            if (!ctx.BollingerCalc.IsReady) return null;

            double rsi = ctx.RsiCalc.Value;

            // LONG: price at/below lower BB + RSI oversold
            if (close <= ctx.BollingerCalc.LowerBand && rsi < 35)
            {
                confluenceScore = 1.0;
                if (rsi < 25) confluenceScore += 0.5;
                if (close < ctx.BollingerCalc.LowerBand) confluenceScore += 0.5;

                if (ctx.StructureState != null)
                {
                    foreach (var ob in ctx.StructureState.ActiveOrderBlocks)
                    {
                        if (!ob.Mitigated && ob.Direction == StructureDirection.Bullish && close <= ob.High
                            && close >= ob.Low - (ob.High - ob.Low))
                        {
                            confluenceScore += 1.0;
                            break;
                        }
                    }
                }

                if (confluenceScore < MeanRevMinConfluence) return null;
                return TradeType.Buy;
            }

            // SHORT: price at/above upper BB + RSI overbought
            if (close >= ctx.BollingerCalc.UpperBand && rsi > 65)
            {
                confluenceScore = 1.0;
                if (rsi > 75) confluenceScore += 0.5;
                if (close > ctx.BollingerCalc.UpperBand) confluenceScore += 0.5;

                if (ctx.StructureState != null)
                {
                    foreach (var ob in ctx.StructureState.ActiveOrderBlocks)
                    {
                        if (!ob.Mitigated && ob.Direction == StructureDirection.Bearish && close >= ob.Low
                            && close <= ob.High + (ob.High - ob.Low))
                        {
                            confluenceScore += 1.0;
                            break;
                        }
                    }
                }

                if (confluenceScore < MeanRevMinConfluence) return null;
                return TradeType.Sell;
            }

            return null;
        }

        private void MarkLifecycle(TradeCandidate c)
        {
            if (c.TradeType == TradeType.Buy)
                SignalLifecycleEngine.MarkBuy(c.Index, c.Context.LifecycleState);
            else
                SignalLifecycleEngine.MarkSell(c.Index, c.Context.LifecycleState);
        }

        // ════════════════════════════════════════════════════════════
        //  EXECUTION
        // ════════════════════════════════════════════════════════════

        private bool TryExecute(TradeCandidate candidate)
        {
            var ctx = candidate.Context;

            if (!EnableAutoExecution)
            {
                Print("[SIGNAL] {0} {1} | {2}/{3} | ADX={4:F1} | Confluence={5:F1}",
                    ctx.SymbolName, candidate.TradeType,
                    candidate.Regime.Regime, candidate.Regime.Strategy,
                    candidate.Regime.AdxValue, candidate.ConfluenceScore);
                return false;
            }

            UpdateSpreadProfile(ctx);
            if (!IsSpreadTradeable(ctx)) return false;

            // Check all risk limits
            int tradesToday = GetTradesCountToday();
            int tradesTodaySymbol = GetTradesCountTodaySymbol(ctx.SymbolName);
            int concurrent = Positions.Count(p => p.Label == TradeLabel);
            double dailyPnlPct = GetDailyPnlPct();
            GetRecentPerformance(out int recentCount, out double recentWinRate);

            if (!RiskProfileEngine.IsTradingAllowed(
                _riskProfile, tradesToday, tradesTodaySymbol,
                concurrent, dailyPnlPct, recentCount, recentWinRate))
                return false;

            if (!CanOpenOnSymbol(ctx.SymbolName)) return false;

            // Calculate stops — strategy-specific
            double slMult = candidate.Strategy == StrategyMode.MeanReversion
                ? MeanRevSlAtrMult : SlAtrMultiplier;
            double slPips = candidate.AtrPips * slMult * _riskProfile.SlMultiplier;
            slPips = Math.Max(MinSlPips, Math.Min(MaxSlPips, slPips));

            double tpPips;
            if (candidate.Strategy == StrategyMode.MeanReversion
                && MeanRevTpToMidBand
                && candidate.BollingerMidBandDistance > 0)
            {
                // Target the Bollinger mid-band for mean reversion
                tpPips = candidate.BollingerMidBandDistance * _riskProfile.TpMultiplier;
            }
            else
            {
                tpPips = candidate.AtrPips * TpAtrMultiplier * _riskProfile.TpMultiplier;
            }
            double minTpPips = slPips * Math.Max(MinRiskReward, _riskProfile.MinRiskReward);
            tpPips = Math.Max(minTpPips, tpPips);

            // Volume via risk engine
            double volumeUnits = RiskProfileEngine.CalculateVolume(
                Account.Equity,
                _riskProfile.RiskPerTrade,
                slPips,
                ctx.Symbol.PipValue,
                ctx.Symbol.VolumeInUnitsMin,
                ctx.Symbol.VolumeInUnitsMax,
                ctx.Symbol.VolumeInUnitsStep);

            if (volumeUnits <= 0) return false;
            if (!IsExposureWithinLimits(ctx.Symbol, volumeUnits, slPips)) return false;

            // Execute with retry
            double firstQuote = candidate.TradeType == TradeType.Buy ? ctx.Symbol.Ask : ctx.Symbol.Bid;
            TradeResult result = null;

            for (int attempt = 0; attempt < EntryRetryAttempts; attempt++)
            {
                double currentQuote = candidate.TradeType == TradeType.Buy ? ctx.Symbol.Ask : ctx.Symbol.Bid;
                if (Math.Abs(currentQuote - firstQuote) / ctx.Symbol.PipSize > MaxEntryDeviationPips)
                    return false;

                result = ExecuteMarketOrder(candidate.TradeType, ctx.SymbolName, volumeUnits,
                    TradeLabel, slPips, tpPips);
                if (result.IsSuccessful) break;
                if (IsNonRetriableError(result)) break;
            }

            if (result == null || !result.IsSuccessful)
            {
                Print("Order failed {0} {1}: {2}", ctx.SymbolName, candidate.TradeType,
                    result != null ? result.Error.ToString() : "Unknown");
                return false;
            }

            Print("EXECUTED {0} {1} | {2}/{3} | SL={4:F1} TP={5:F1} | ADX={6:F1} | Conf={7:F1}",
                ctx.SymbolName, candidate.TradeType,
                candidate.Regime.Regime, candidate.Regime.Strategy,
                slPips, tpPips, candidate.Regime.AdxValue, candidate.ConfluenceScore);
            return true;
        }

        // ════════════════════════════════════════════════════════════
        //  SUPPORT METHODS
        // ════════════════════════════════════════════════════════════

        private bool IsSessionAllowed(DateTime utcTime)
        {
            if (!EnableSessionFilter) return true;
            int hour = utcTime.Hour;

            // Exclude daily rollover hour (22:00-22:59 UTC) — wide spreads, whipsaw
            if (hour == 22) return false;

            // Sessions can overlap — each is checked independently
            if (TradeAsiaSession && (hour >= 23 || hour < 7)) return true;  // skip rollover hour
            if (TradeLondonSession && hour >= 7 && hour < 16) return true;
            if (TradeNewYorkSession && hour >= 13 && hour < 22) return true;

            return false;
        }

        private void UpdateSpreadProfile(SymbolContext ctx)
        {
            double spread = GetSpreadPips(ctx.Symbol);
            ctx.RecentSpreads.Enqueue(spread);
            while (ctx.RecentSpreads.Count > SpreadProfileWindow)
                ctx.RecentSpreads.Dequeue();

            double sum = 0;
            foreach (var v in ctx.RecentSpreads) sum += v;
            ctx.AvgSpread = ctx.RecentSpreads.Count > 0 ? sum / ctx.RecentSpreads.Count : spread;
        }

        private bool IsSpreadTradeable(SymbolContext ctx)
        {
            double spread = GetSpreadPips(ctx.Symbol);
            if (spread > MaxSpreadPips) return false;
            if (ctx.RecentSpreads.Count < 8) return true;
            return spread <= Math.Max(0.01, ctx.AvgSpread) * MaxSpreadToAvgRatio;
        }

        private double GetSpreadPips(Symbol symbol)
        {
            return (symbol.Ask - symbol.Bid) / symbol.PipSize;
        }

        private bool IsNonRetriableError(TradeResult result)
        {
            if (result == null) return false;
            var c = result.Error;
            return c == ErrorCode.NoMoney || c == ErrorCode.BadVolume ||
                   c == ErrorCode.MarketClosed || c == ErrorCode.TechnicalError ||
                   c == ErrorCode.Disconnected;
        }

        private bool IsExposureWithinLimits(Symbol newSym, double newVolUnits, double slPips)
        {
            var positions = Positions.Where(p => p.Label == TradeLabel).ToArray();

            // Gross lots check
            double currentGrossLots = positions.Sum(p =>
            {
                var s = Symbols.GetSymbol(p.SymbolName);
                return s != null ? s.VolumeInUnitsToQuantity(p.VolumeInUnits) : 0;
            });
            double incomingLots = newSym.VolumeInUnitsToQuantity(newVolUnits);
            if (currentGrossLots + incomingLots > MaxGrossExposureLots)
                return false;

            // Risk amount check
            double currentRisk = positions.Sum(p => EstimatePositionRisk(p));
            double incomingRisk = slPips * newSym.VolumeInUnitsToQuantity(newVolUnits) * newSym.PipValue;
            double maxRisk = Account.Equity * _riskProfile.RiskPerTrade * _riskProfile.MaxConcurrentPositions;

            return currentRisk + incomingRisk <= maxRisk;
        }

        private double EstimatePositionRisk(Position pos)
        {
            var sym = Symbols.GetSymbol(pos.SymbolName);
            if (sym == null) return 0;
            double slDist = pos.StopLoss.HasValue
                ? Math.Abs(pos.EntryPrice - pos.StopLoss.Value) / sym.PipSize
                : 20;
            return slDist * sym.VolumeInUnitsToQuantity(pos.VolumeInUnits) * sym.PipValue;
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
            return activeSymbols.Count < _riskProfile.MaxConcurrentPositions;
        }

        private int GetTradesCountToday()
        {
            return History.Count(t => t.Label == TradeLabel && t.ClosingTime.Date == _currentTradingDate);
        }

        private int GetTradesCountTodaySymbol(string sym)
        {
            return History.Count(t => t.Label == TradeLabel && t.ClosingTime.Date == _currentTradingDate
                && string.Equals(t.SymbolName, sym, StringComparison.OrdinalIgnoreCase));
        }

        private double GetDailyPnlPct()
        {
            double closedPnl = History
                .Where(t => t.Label == TradeLabel && t.ClosingTime.Date == _currentTradingDate)
                .Sum(t => t.NetProfit);
            double floatingPnl = Positions.Where(p => p.Label == TradeLabel).Sum(p => p.NetProfit);
            double equity = Math.Max(1, _startOfDayEquity);
            return (closedPnl + floatingPnl) / equity;
        }

        private void GetRecentPerformance(out int count, out double winRate)
        {
            var recent = History
                .Where(t => t.Label == TradeLabel)
                .OrderByDescending(t => t.ClosingTime)
                .Take(30)
                .ToList();
            count = recent.Count;
            winRate = count > 0 ? (double)recent.Count(t => t.NetProfit > 0) / count : 0.5;
        }

        private bool IsDailyDrawdownBreached()
        {
            return GetDailyPnlPct() <= -_riskProfile.MaxDailyDrawdownPct;
        }

        private void HandleDayRollover()
        {
            var today = Server.TimeInUtc.Date;
            if (today != _currentTradingDate)
            {
                _currentTradingDate = today;
                _dailyLockTriggered = false;
                _startOfDayEquity = Account.Equity;
            }
        }

        private void CloseBotPositions()
        {
            foreach (var p in Positions.Where(p => p.Label == TradeLabel).ToArray())
                ClosePosition(p);
        }

        private void GetHtfValues(SymbolContext ctx, int index, out double htfFast, out double htfSlow)
        {
            htfFast = ctx.Bars.ClosePrices[index];
            htfSlow = ctx.Bars.ClosePrices[index];

            if (!UseHtfFilter || ctx.HtfBars == null || ctx.HtfFastMa == null || ctx.HtfSlowMa == null)
                return;

            int htfIdx = ctx.HtfBars.OpenTimes.GetIndexByTime(ctx.Bars.OpenTimes[index]);
            if (htfIdx < 0 || htfIdx >= ctx.HtfBars.Count) return;

            htfFast = ctx.HtfFastMa.Result[htfIdx];
            htfSlow = ctx.HtfSlowMa.Result[htfIdx];
        }

        // ════════════════════════════════════════════════════════════
        //  SYMBOL RESOLUTION & CONTEXT
        // ════════════════════════════════════════════════════════════

        private List<string> ResolveTradeSymbols()
        {
            if (!EnableSymbolScanner)
                return new List<string> { SymbolName };

            var parsed = (SymbolsCsv ?? "")
                .Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (parsed.Count == 0) parsed.Add(SymbolName);
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

            var ctx = new SymbolContext
            {
                SymbolName = symbolName,
                Symbol = symbol,
                Bars = bars,
                Atr = Indicators.AverageTrueRange(bars, AtrPeriod, MovingAverageType.Exponential),
                AdxCalc = new AdxCalculator(AdxPeriod),
                DonchianCalc = new DonchianCalculator(DonchianPeriod),
                BollingerCalc = new BollingerCalculator(BollingerPeriod, BollingerStdDev),
                RsiCalc = new RsiCalculator(RsiPeriod),
                VolEngine = new VolatilityEngine(AtrPeriod),
                StructureState = new MarketStructureState(),
                LifecycleState = new SignalLifecycleState(),
                LifecycleRule = new SignalLifecycleRule
                {
                    MinBarsBetweenSameDirection = SignalCooldownBars,
                    MaxSignalAgeBars = SignalCooldownBars * 3
                }
            };

            if (UseHtfFilter)
            {
                ctx.HtfBars = MarketData.GetBars(HigherTimeframe, symbolName);
                if (ctx.HtfBars != null)
                {
                    ctx.HtfFastMa = Indicators.ExponentialMovingAverage(
                        ctx.HtfBars.ClosePrices, HtfFastMaPeriod);
                    ctx.HtfSlowMa = Indicators.ExponentialMovingAverage(
                        ctx.HtfBars.ClosePrices, HtfSlowMaPeriod);
                }
            }

            // ── Warm up engines with historical bars ──
            int warmup = Math.Min(bars.Count - 1, 250);
            for (int i = Math.Max(0, bars.Count - 1 - warmup); i < bars.Count - 1; i++)
            {
                ctx.AdxCalc.Update(bars.HighPrices[i], bars.LowPrices[i], bars.ClosePrices[i]);
                ctx.DonchianCalc.Update(bars.HighPrices[i], bars.LowPrices[i]);
                ctx.BollingerCalc.Update(bars.ClosePrices[i]);
                ctx.RsiCalc.Update(bars.ClosePrices[i]);

                double atr = ctx.Atr.Result[i];
                if (!double.IsNaN(atr) && atr > 0)
                    ctx.VolEngine.Update(atr, bars.HighPrices[i], bars.LowPrices[i], bars.TickVolumes[i], symbol.PipSize);

                MarketStructureEngine.Update(
                    ctx.StructureState, i,
                    j => bars.OpenPrices[j], j => bars.HighPrices[j],
                    j => bars.LowPrices[j], j => bars.ClosePrices[j],
                    SwingLookback, symbol.PipSize, 2.0);
            }

            return ctx;
        }

        // ════════════════════════════════════════════════════════════
        //  INNER TYPES
        // ════════════════════════════════════════════════════════════

        private sealed class SymbolContext
        {
            public string SymbolName { get; set; }
            public Symbol Symbol { get; set; }
            public Bars Bars { get; set; }
            public AverageTrueRange Atr { get; set; }
            public Bars HtfBars { get; set; }
            public MovingAverage HtfFastMa { get; set; }
            public MovingAverage HtfSlowMa { get; set; }

            // Custom calculators
            public AdxCalculator AdxCalc { get; set; }
            public DonchianCalculator DonchianCalc { get; set; }
            public BollingerCalculator BollingerCalc { get; set; }
            public RsiCalculator RsiCalc { get; set; }
            public VolatilityEngine VolEngine { get; set; }
            public MarketStructureState StructureState { get; set; }

            // Lifecycle
            public SignalLifecycleState LifecycleState { get; set; }
            public SignalLifecycleRule LifecycleRule { get; set; }

            // Spread tracking
            public Queue<double> RecentSpreads { get; } = new Queue<double>();
            public double AvgSpread { get; set; }
            public DateTime LastProcessedBarTime { get; set; } = DateTime.MinValue;
        }

        private sealed class TradeCandidate
        {
            public SymbolContext Context { get; set; }
            public TradeType TradeType { get; set; }
            public AdaptiveRegimeState Regime { get; set; }
            public double AtrPips { get; set; }
            public double ConfluenceScore { get; set; }
            public int Index { get; set; }
            public StrategyMode Strategy { get; set; }
            public double BollingerMidBandDistance { get; set; } // pips to mid-band for mean reversion TP
        }
    }
}
