using System;
using System.Collections.Generic;
using cAlgo.API;
using cAlgo.API.Indicators;

namespace cAlgo.Indicators
{
    [Indicator(IsOverlay = true, TimeZone = TimeZones.UTC, AccessRights = AccessRights.None)]
    public class ApexFlowSmartMoneyIndicator : Indicator
    {
        [Parameter("Signal Preset", Group = "Presets", DefaultValue = SignalPreset.Intraday, Description = "Scalp: M1-M5 (strict). Intraday: M15-H1 (balanced). Swing: H4-D1 (clean trend). Custom: manual values.")]
        public SignalPreset SelectedPreset { get; set; }

        [Parameter("Swing Lookback", Group = "Price Action", DefaultValue = 4, MinValue = 2, MaxValue = 20)]
        public int SwingLookback { get; set; }

        [Parameter("SR Max Lines", Group = "Price Action", DefaultValue = 6, MinValue = 2, MaxValue = 20)]
        public int MaxSrLines { get; set; }

        [Parameter("Momentum Period", Group = "Price Action", DefaultValue = 8, MinValue = 3, MaxValue = 50)]
        public int MomentumPeriod { get; set; }

        [Parameter("Depth Levels", Group = "Order Flow", DefaultValue = 5, MinValue = 1, MaxValue = 20)]
        public int DepthLevels { get; set; }

        [Parameter("Bubble Sensitivity", Group = "Order Flow", DefaultValue = 1.4, MinValue = 1.0, MaxValue = 5.0)]
        public double BubbleSensitivity { get; set; }

        [Parameter("Min Bubble Body", Group = "Order Flow", DefaultValue = 0.35, MinValue = 0.05, MaxValue = 1.0)]
        public double MinBubbleBodyPressure { get; set; }

        [Parameter("Min Depth Imb", Group = "Order Flow", DefaultValue = 0.05, MinValue = 0.0, MaxValue = 1.0)]
        public double MinDepthImbalanceForBubble { get; set; }

        [Parameter("Enable Volume Nodes", Group = "Volume Profile", DefaultValue = true)]
        public bool EnableVolumeNodes { get; set; }

        [Parameter("Bin Size (pips)", Group = "Volume Profile", DefaultValue = 2.0, MinValue = 0.5, MaxValue = 20)]
        public double ProfileBinPips { get; set; }

        [Parameter("HVN Percentile", Group = "Volume Profile", DefaultValue = 0.8, MinValue = 0.5, MaxValue = 0.99)]
        public double HvnPercentile { get; set; }

        [Parameter("LVN Percentile", Group = "Volume Profile", DefaultValue = 0.25, MinValue = 0.01, MaxValue = 0.45)]
        public double LvnPercentile { get; set; }

        [Parameter("Node Touch Tol (pips)", Group = "Volume Profile", DefaultValue = 3.0, MinValue = 0.5, MaxValue = 50)]
        public double NodeTouchTolerancePips { get; set; }

        [Parameter("Min Node Ratio", Group = "Volume Profile", DefaultValue = 0.2, MinValue = 0.01, MaxValue = 2.0)]
        public double MinNodeParticipationRatio { get; set; }

        [Parameter("Bubble Node Gate", Group = "Volume Profile", DefaultValue = BubbleNodeGateMode.PreferNodes, Description = "StrictNodes = only near HVN/LVN. PreferNodes = allow strong non-node impulse. Off = no node-location gating.")]
        public BubbleNodeGateMode BubbleNodeGate { get; set; }

        [Parameter("Profile London", Group = "Volume Profile", DefaultValue = true)]
        public bool EnableLondonProfile { get; set; }

        [Parameter("Profile New York", Group = "Volume Profile", DefaultValue = true)]
        public bool EnableNewYorkProfile { get; set; }

        [Parameter("Profile Asia", Group = "Volume Profile", DefaultValue = true)]
        public bool EnableAsiaProfile { get; set; }

        [Parameter("Strict Trigger", Group = "Volume Profile", DefaultValue = StrictTriggerMode.LvnOrPocReclaim)]
        public StrictTriggerMode StrictTrigger { get; set; }

        [Parameter("Min Signal Confidence", Group = "Volume Profile", DefaultValue = 70, MinValue = 0, MaxValue = 100)]
        public int MinSignalConfidence { get; set; }

        [Parameter("Min Break Move (pips)", Group = "Volume Profile", DefaultValue = 2.0, MinValue = 0.5, MaxValue = 50)]
        public double MinBreakMovePips { get; set; }

        [Parameter("Enable FVG", Group = "Patterns", DefaultValue = true)]
        public bool EnableFvg { get; set; }

        [Parameter("Enable Continuation Pattern", Group = "Patterns", DefaultValue = true)]
        public bool EnableContinuationPattern { get; set; }

        [Parameter("CP Min Momentum", Group = "Patterns", DefaultValue = 0.8, MinValue = 0.1, MaxValue = 20)]
        public double CpMinMomentum { get; set; }

        [Parameter("CP Cooldown Bars", Group = "Patterns", DefaultValue = 5, MinValue = 1, MaxValue = 100)]
        public int CpCooldownBars { get; set; }

        [Parameter("Enable Structure Signals", Group = "Display", DefaultValue = true)]
        public bool EnableStructureSignals { get; set; }

        [Parameter("Enable S/R Lines", Group = "Display", DefaultValue = true)]
        public bool EnableSupportResistanceLines { get; set; }

        [Parameter("Enable Bubbles", Group = "Display", DefaultValue = true)]
        public bool EnableOrderFlowBubbles { get; set; }

        [Parameter("Enable Info Panel", Group = "Display", DefaultValue = true)]
        public bool EnableInfoPanel { get; set; }

        [Parameter("Clean Chart Mode", Group = "Display", DefaultValue = true, Description = "Shows only primary levels and key signals by default.")]
        public bool CleanChartMode { get; set; }

        [Parameter("Show Secondary Levels", Group = "Display", DefaultValue = false)]
        public bool ShowSecondaryLevels { get; set; }

        [Parameter("Debug Signals", Group = "Display", DefaultValue = false, Description = "Shows why bubble candidates are accepted/rejected.")]
        public bool DebugSignals { get; set; }

        [Parameter("Show Guide On Chart", Group = "Display", DefaultValue = false, Description = "Off = clean chart. Preset guidance is available in Signal Preset description.")]
        public bool ShowGuideOnChart { get; set; }

        [Parameter("Info Text Mode", Group = "Display", DefaultValue = InfoTextMode.Compact)]
        public InfoTextMode PanelTextMode { get; set; }

        [Parameter("Max Visual Bars", Group = "Display", DefaultValue = 500, MinValue = 50, MaxValue = 5000)]
        public int MaxVisualBars { get; set; }

        [Parameter("Bubble Cooldown Bars", Group = "Display", DefaultValue = 3, MinValue = 1, MaxValue = 100)]
        public int BubbleCooldownBars { get; set; }

        [Parameter("Signal Cooldown Bars", Group = "Signals", DefaultValue = 6, MinValue = 1, MaxValue = 100)]
        public int SignalCooldownBars { get; set; }

        [Parameter("Regime Chop Thresh", Group = "Signals", DefaultValue = 0.9, MinValue = 0.1, MaxValue = 20)]
        public double RegimeChopThreshold { get; set; }

        [Parameter("Regime Hysteresis", Group = "Signals", DefaultValue = 0.15, MinValue = 0.0, MaxValue = 0.5)]
        public double RegimeHysteresis { get; set; }

        [Parameter("Regime Vol Window", Group = "Signals", DefaultValue = 50, MinValue = 10, MaxValue = 500)]
        public int RegimeVolatilityWindow { get; set; }

        [Parameter("Regime Sess Window", Group = "Signals", DefaultValue = 80, MinValue = 10, MaxValue = 500)]
        public int RegimeSessionWindow { get; set; }

        [Parameter("Enable Analytics", Group = "Analytics", DefaultValue = true)]
        public bool EnableAnalytics { get; set; }

        [Parameter("Outcome Horizon Bars", Group = "Analytics", DefaultValue = 8, MinValue = 1, MaxValue = 200)]
        public int OutcomeHorizonBars { get; set; }

        [Parameter("Success Move (pips)", Group = "Analytics", DefaultValue = 6.0, MinValue = 0.5, MaxValue = 500)]
        public double SuccessMovePips { get; set; }

        [Parameter("Use HTF Filter", Group = "MTF", DefaultValue = true)]
        public bool UseHigherTimeframeFilter { get; set; }

        [Parameter("HTF", Group = "MTF", DefaultValue = "Hour")]
        public TimeFrame HigherTimeframe { get; set; }

        [Parameter("HTF Fast MA", Group = "MTF", DefaultValue = 20, MinValue = 2, MaxValue = 200)]
        public int HigherTimeframeFastMaPeriod { get; set; }

        [Parameter("HTF Slow MA", Group = "MTF", DefaultValue = 50, MinValue = 5, MaxValue = 400)]
        public int HigherTimeframeSlowMaPeriod { get; set; }

        [Parameter("Enable External Model", Group = "Hybrid", DefaultValue = false)]
        public bool EnableExternalModel { get; set; }

        [Parameter("External Weight", Group = "Hybrid", DefaultValue = 0.25, MinValue = 0, MaxValue = 1)]
        public double ExternalWeight { get; set; }

        [Parameter("Min Ext Confidence", Group = "Hybrid", DefaultValue = 0.6, MinValue = 0, MaxValue = 1)]
        public double MinExternalConfidence { get; set; }

        [Parameter("Max Ext Age (sec)", Group = "Hybrid", DefaultValue = 10, MinValue = 1, MaxValue = 300)]
        public int MaxExternalSignalAgeSeconds { get; set; }

        [Output("Regime", LineColor = "Transparent")]
        public IndicatorDataSeries Regime { get; set; }

        private readonly Queue<double> _recentAbsDelta = new Queue<double>();
        private readonly Queue<double> _srLevels = new Queue<double>();

        private MarketDepth _marketDepth;
        private IExternalSignalProvider _externalSignalProvider;
        private HybridBlendConfig _hybridBlendConfig;
        private ExternalSignalResponse _lastExternalSignal;
        private double _lastRegimeScore;
        private bool _isDepthAvailable;
        private string _htfStateText;
        private string _presetSummary;
        private SignalPreset _activePreset;
        private int _lastCpSignalIndex;
        private int _lastBubbleSignalIndex;
        private int _lastRenderedHvnCount;
        private int _lastRenderedLvnCount;
        private double _lastSwingHigh = double.NaN;
        private double _lastSwingLow = double.NaN;
        private double _lastNodeParticipation;
        private string _lastNodeContext;
        private double _lastPocDistancePips;
        private int _effectiveMaxVisualBars;
        private int _effectiveBubbleCooldownBars;
        private int _effectiveCpCooldownBars;
        private int _effectiveHigherTimeframeFastMaPeriod;
        private int _effectiveHigherTimeframeSlowMaPeriod;
        private double _effectiveBubbleSensitivity;
        private double _effectiveMinBubbleBodyPressure;
        private double _effectiveMinDepthImbalanceForBubble;
        private double _effectiveCpMinMomentum;
        private int _effectiveMinSignalConfidence;
        private double _effectiveMinBreakMovePips;
        private int _effectiveSignalCooldownBars;
        private double _effectiveRegimeChopThreshold;
        private double _effectiveRegimeHysteresis;
        private int _effectiveRegimeVolatilityWindow;
        private int _effectiveRegimeSessionWindow;
        private bool _effectiveUseHigherTimeframeFilter;
        private TimeFrame _effectiveHigherTimeframe;
        private Bars _higherTimeframeBars;
        private MovingAverage _higherTimeframeFastMa;
        private MovingAverage _higherTimeframeSlowMa;
        private SessionVolumeProfileEngine _volumeProfileEngine;
        private VolumeProfileSnapshot _volumeProfileSnapshot;
        private RegimeState _regimeState;
        private LevelScoreResult _levelScore;
        private SignalLifecycleState _signalLifecycleState;
        private SignalLifecycleRule _signalLifecycleRule;
        private string _lastDebugBuy;
        private string _lastDebugSell;
        private SignalOutcomeTracker _signalOutcomeTracker;
        private readonly Queue<double> _recentRangePips = new Queue<double>();
        private readonly Queue<double> _recentTickVolumes = new Queue<double>();

        protected override void Initialize()
        {
            ApplyPresetSettings();

            _marketDepth = MarketData.GetMarketDepth(SymbolName);
            _externalSignalProvider = new DisabledExternalSignalProvider();
            _hybridBlendConfig = new HybridBlendConfig
            {
                EnableExternalModel = EnableExternalModel,
                ExternalWeight = ExternalWeight,
                MaxExternalSignalAgeSeconds = MaxExternalSignalAgeSeconds,
                MinimumExternalConfidence = MinExternalConfidence
            };
            _lastExternalSignal = ExternalSignalResponse.Disabled();
            _lastRegimeScore = 0;
            _isDepthAvailable = false;
            _htfStateText = "Disabled";
            _lastCpSignalIndex = -100000;
            _lastBubbleSignalIndex = -100000;
            _lastRenderedHvnCount = 0;
            _lastRenderedLvnCount = 0;
            _lastNodeParticipation = 0;
            _lastNodeContext = "None";
            _lastPocDistancePips = double.NaN;
            _regimeState = new RegimeState { Regime = MarketRegime.Unknown };
            _levelScore = new LevelScoreResult { Source = "None" };
            _signalLifecycleState = new SignalLifecycleState();
            _signalLifecycleRule = new SignalLifecycleRule
            {
                MinBarsBetweenSameDirection = _effectiveSignalCooldownBars,
                MaxSignalAgeBars = _effectiveSignalCooldownBars * 2
            };
            _lastDebugBuy = "n/a";
            _lastDebugSell = "n/a";
            _signalOutcomeTracker = new SignalOutcomeTracker();

            _volumeProfileEngine = new SessionVolumeProfileEngine(
                Symbol.PipSize * ProfileBinPips,
                EnableLondonProfile,
                EnableNewYorkProfile,
                EnableAsiaProfile);
            _volumeProfileSnapshot = new VolumeProfileSnapshot { HasData = false, SessionLabel = "Disabled" };

            if (_effectiveUseHigherTimeframeFilter)
            {
                _higherTimeframeBars = MarketData.GetBars(_effectiveHigherTimeframe, SymbolName);
                _higherTimeframeFastMa = Indicators.SimpleMovingAverage(_higherTimeframeBars.ClosePrices, _effectiveHigherTimeframeFastMaPeriod);
                _higherTimeframeSlowMa = Indicators.SimpleMovingAverage(_higherTimeframeBars.ClosePrices, _effectiveHigherTimeframeSlowMaPeriod);
            }
        }

        public override void Calculate(int index)
        {
            UpdateVolumeProfile(index);

            if (index < Math.Max(SwingLookback * 2 + 2, MomentumPeriod + 2))
            {
                Regime[index] = Bars.ClosePrices[index];
                _lastRegimeScore = 0;
                return;
            }

            var structure = AnalyzeStructure(index);
            GetCurrentHtfMaValues(index, out double htfFast, out double htfSlow);
            UpdateRegimeNormalization(index, out double volatilityPerBarPips, out double sessionActivityRatio);
            _regimeState = RegimeStateEngine.Evaluate(
                structure.Momentum,
                structure.BosUp,
                structure.BosDown,
                htfFast,
                htfSlow,
                _effectiveRegimeChopThreshold,
                _regimeState.Regime,
                volatilityPerBarPips,
                sessionActivityRatio,
                _effectiveRegimeHysteresis);

            _levelScore = LevelScoringEngine.Evaluate(
                Bars.ClosePrices[index],
                _volumeProfileSnapshot != null && _volumeProfileSnapshot.HasData ? _volumeProfileSnapshot.PocPrice : double.NaN,
                _volumeProfileSnapshot != null ? _volumeProfileSnapshot.HvnPrices : null,
                _volumeProfileSnapshot != null ? _volumeProfileSnapshot.LvnPrices : null,
                _srLevels,
                Symbol.PipSize);

            _signalLifecycleRule.MinBarsBetweenSameDirection = _effectiveSignalCooldownBars;
            var orderFlow = AnalyzeOrderFlow(index);

            if (EnableAnalytics)
            {
                _signalOutcomeTracker.Update(index, Bars.ClosePrices[index], OutcomeHorizonBars, SuccessMovePips, Symbol.PipSize);
            }

            if (ShouldRenderVisuals(index))
                AnalyzePatterns(index, structure);
            var localScore = structure.RegimeScore + orderFlow.PressureScore;
            var externalRequest = BuildExternalSignalRequest(index, localScore);
            _lastExternalSignal = _externalSignalProvider.GetSignal(externalRequest);
            _lastRegimeScore = HybridBlend.ComposeScore(localScore, _lastExternalSignal, _hybridBlendConfig, Server.TimeInUtc);
            Regime[index] = Bars.ClosePrices[index];

            Render(index, structure, orderFlow);
        }

        private ExternalSignalRequest BuildExternalSignalRequest(int index, double localScore)
        {
            return new ExternalSignalRequest
            {
                SymbolName = SymbolName,
                TimeFrame = TimeFrame.ToString(),
                BarTime = Bars.OpenTimes[index],
                Open = Bars.OpenPrices[index],
                High = Bars.HighPrices[index],
                Low = Bars.LowPrices[index],
                Close = Bars.ClosePrices[index],
                TickVolume = Bars.TickVolumes[index],
                LocalScore = localScore
            };
        }

        private StructureSignal AnalyzeStructure(int index)
        {
            bool isSwingHigh = IsSwingHigh(index - SwingLookback);
            bool isSwingLow = IsSwingLow(index - SwingLookback);

            if (isSwingHigh)
            {
                _lastSwingHigh = Bars.HighPrices[index - SwingLookback];
                PushSrLevel(_lastSwingHigh);
            }

            if (isSwingLow)
            {
                _lastSwingLow = Bars.LowPrices[index - SwingLookback];
                PushSrLevel(_lastSwingLow);
            }

            bool bosUp = !double.IsNaN(_lastSwingHigh) && Bars.ClosePrices[index] > _lastSwingHigh;
            bool bosDown = !double.IsNaN(_lastSwingLow) && Bars.ClosePrices[index] < _lastSwingLow;

            double momentum = (Bars.ClosePrices[index] - Bars.ClosePrices[index - MomentumPeriod]) / (MomentumPeriod * Symbol.PipSize);
            double regime = bosUp ? 1 : bosDown ? -1 : Math.Sign(momentum);

            return new StructureSignal
            {
                IsSwingHigh = isSwingHigh,
                IsSwingLow = isSwingLow,
                BosUp = bosUp,
                BosDown = bosDown,
                Momentum = momentum,
                RegimeScore = regime
            };
        }

        private OrderFlowSignal AnalyzeOrderFlow(int index)
        {
            double body = Bars.ClosePrices[index] - Bars.OpenPrices[index];
            double range = Math.Max(Bars.HighPrices[index] - Bars.LowPrices[index], Symbol.PipSize);
            double barPressure = body / range;
            _lastNodeContext = "None";

            double topBid = 0;
            double topAsk = 0;
            if (_marketDepth != null)
            {
                int maxBid = Math.Min(DepthLevels, _marketDepth.BidEntries.Count);
                int maxAsk = Math.Min(DepthLevels, _marketDepth.AskEntries.Count);

                for (int i = 0; i < maxBid; i++)
                    topBid += _marketDepth.BidEntries[i].VolumeInUnits;

                for (int i = 0; i < maxAsk; i++)
                    topAsk += _marketDepth.AskEntries[i].VolumeInUnits;
            }

            _isDepthAvailable = (topBid + topAsk) > 0;

            double depthImbalance = (topBid + topAsk) > 0 ? (topBid - topAsk) / (topBid + topAsk) : 0;

            double deltaProxy = (Bars.ClosePrices[index] - Bars.ClosePrices[index - 1]) / Symbol.PipSize * Bars.TickVolumes[index];
            double absDelta = Math.Abs(deltaProxy);
            _recentAbsDelta.Enqueue(absDelta);
            while (_recentAbsDelta.Count > 80)
                _recentAbsDelta.Dequeue();

            double avgAbsDelta = 0;
            foreach (var value in _recentAbsDelta)
                avgAbsDelta += value;
            avgAbsDelta = _recentAbsDelta.Count == 0 ? 0 : avgAbsDelta / _recentAbsDelta.Count;

            double bubbleMultiple = avgAbsDelta > 0 ? absDelta / avgAbsDelta : 0;
            bool hasBodyStrength = Math.Abs(barPressure) >= _effectiveMinBubbleBodyPressure;
            bool depthAligned = !_isDepthAvailable ||
                               Math.Abs(depthImbalance) < _effectiveMinDepthImbalanceForBubble ||
                               Math.Sign(depthImbalance) == Math.Sign(deltaProxy);
            bool passesBubbleCore = bubbleMultiple >= _effectiveBubbleSensitivity && hasBodyStrength && depthAligned;

            double closePrice = Bars.ClosePrices[index];
            double tolerancePrice = NodeTouchTolerancePips * Symbol.PipSize;
            bool nearHvn = EnableVolumeNodes && _volumeProfileSnapshot.HasData && _volumeProfileSnapshot.IsNearAnyNode(_volumeProfileSnapshot.HvnPrices, closePrice, tolerancePrice);
            bool nearLvn = EnableVolumeNodes && _volumeProfileSnapshot.HasData && _volumeProfileSnapshot.IsNearAnyNode(_volumeProfileSnapshot.LvnPrices, closePrice, tolerancePrice);

            _lastNodeParticipation = EnableVolumeNodes && _volumeProfileSnapshot.HasData
                ? _volumeProfileSnapshot.VolumeRatioAtPrice(closePrice)
                : 0;

            _lastPocDistancePips = EnableVolumeNodes
                ? _volumeProfileSnapshot.DistanceToPocInPips(closePrice, Symbol.PipSize)
                : double.NaN;

            bool nodeQualified = !EnableVolumeNodes || _lastNodeParticipation >= MinNodeParticipationRatio;
            bool nodeContextQualified = EvaluateNodeContextGate(nearHvn, nearLvn, bubbleMultiple, _lastNodeParticipation);

            if (nearLvn)
                _lastNodeContext = "LVN";
            else if (nearHvn)
                _lastNodeContext = "HVN";
            else if (EnableVolumeNodes && _volumeProfileSnapshot.HasData && !double.IsNaN(_lastPocDistancePips) && _lastPocDistancePips <= NodeTouchTolerancePips)
                _lastNodeContext = "POC";
            else if (EnableVolumeNodes && _volumeProfileSnapshot.HasData)
                _lastNodeContext = "Away";

            double previousClose = Bars.ClosePrices[index - 1];
            double nearestLvn = EnableVolumeNodes && _volumeProfileSnapshot.HasData
                ? SignalReliabilityEngine.FindNearestLevel(_volumeProfileSnapshot.LvnPrices, closePrice)
                : double.NaN;

            var strictEval = SignalReliabilityEngine.EvaluateStrictMode(
                StrictTrigger,
                previousClose,
                closePrice,
                _volumeProfileSnapshot.HasData ? _volumeProfileSnapshot.PocPrice : double.NaN,
                nearestLvn,
                EnableVolumeNodes && _volumeProfileSnapshot.HasData,
                tolerancePrice,
                _effectiveMinBreakMovePips,
                Symbol.PipSize);

            bool htfBuyAligned = IsHigherTimeframeAligned(index, 1);
            bool htfSellAligned = IsHigherTimeframeAligned(index, -1);

            int buyConfidence = SignalReliabilityEngine.ComputeConfidence(
                _lastNodeParticipation,
                bubbleMultiple,
                depthImbalance,
                _isDepthAvailable,
                htfBuyAligned,
                strictEval.PassBuy,
                nearHvn,
                nearLvn);

            int sellConfidence = SignalReliabilityEngine.ComputeConfidence(
                _lastNodeParticipation,
                bubbleMultiple,
                depthImbalance,
                _isDepthAvailable,
                htfSellAligned,
                strictEval.PassSell,
                nearHvn,
                nearLvn);

            int activeConfidence = Math.Max(buyConfidence, sellConfidence);

            bool isAggressiveBuy = passesBubbleCore &&
                                   deltaProxy > 0 &&
                                   nodeQualified &&
                                   nodeContextQualified &&
                                   htfBuyAligned &&
                                   strictEval.PassBuy &&
                                   buyConfidence >= _effectiveMinSignalConfidence;

            bool isAggressiveSell = passesBubbleCore &&
                                    deltaProxy < 0 &&
                                    nodeQualified &&
                                    nodeContextQualified &&
                                    htfSellAligned &&
                                    strictEval.PassSell &&
                                    sellConfidence >= _effectiveMinSignalConfidence;

            _lastDebugBuy = BuildSignalDebugLine(
                "BUY",
                deltaProxy > 0,
                passesBubbleCore,
                nodeQualified,
                nodeContextQualified,
                htfBuyAligned,
                strictEval.PassBuy,
                buyConfidence,
                _effectiveMinSignalConfidence,
                _regimeState != null ? _regimeState.Regime.ToString() : "Unknown",
                _regimeState != null && _regimeState.AllowLongContinuation);

            _lastDebugSell = BuildSignalDebugLine(
                "SELL",
                deltaProxy < 0,
                passesBubbleCore,
                nodeQualified,
                nodeContextQualified,
                htfSellAligned,
                strictEval.PassSell,
                sellConfidence,
                _effectiveMinSignalConfidence,
                _regimeState != null ? _regimeState.Regime.ToString() : "Unknown",
                _regimeState != null && _regimeState.AllowShortContinuation);

            double pressure = 0.55 * barPressure + 0.45 * depthImbalance;

            return new OrderFlowSignal
            {
                DepthImbalance = depthImbalance,
                DeltaProxy = deltaProxy,
                PressureScore = pressure,
                AggressiveBuy = isAggressiveBuy,
                AggressiveSell = isAggressiveSell,
                BubbleTier = GetBubbleTier(_lastNodeParticipation, bubbleMultiple),
                NearHvn = nearHvn,
                NearLvn = nearLvn,
                NodeParticipation = _lastNodeParticipation,
                BuyConfidence = buyConfidence,
                SellConfidence = sellConfidence,
                ActiveConfidence = activeConfidence,
                StrictContext = strictEval.Context
            };
        }

        private void AnalyzePatterns(int index, StructureSignal structure)
        {
            if (EnableFvg && index > 2)
            {
                bool bullishGap = Bars.LowPrices[index] > Bars.HighPrices[index - 2];
                bool bearishGap = Bars.HighPrices[index] < Bars.LowPrices[index - 2];

                if (bullishGap)
                {
                    var rect = Chart.DrawRectangle(
                        $"fvg_bull_{index}",
                        index - 2,
                        Bars.HighPrices[index - 2],
                        index,
                        Bars.LowPrices[index],
                        Color.FromArgb(50, Color.Lime));
                    rect.IsFilled = true;
                }
                else if (bearishGap)
                {
                    var rect = Chart.DrawRectangle(
                        $"fvg_bear_{index}",
                        index - 2,
                        Bars.LowPrices[index - 2],
                        index,
                        Bars.HighPrices[index],
                        Color.FromArgb(50, Color.OrangeRed));
                    rect.IsFilled = true;
                }
            }

            if (EnableContinuationPattern && index > MomentumPeriod)
            {
                bool shallowPullbackBull = structure.Momentum > 0 &&
                                           Math.Abs(structure.Momentum) >= _effectiveCpMinMomentum &&
                                           Bars.LowPrices[index] > Bars.LowPrices[index - 2] &&
                                           Bars.ClosePrices[index] > Bars.OpenPrices[index] &&
                                           IsHigherTimeframeAligned(index, 1) &&
                                           _regimeState.AllowLongContinuation &&
                                           (index - _lastCpSignalIndex >= _effectiveCpCooldownBars);

                bool shallowPullbackBear = structure.Momentum < 0 &&
                                           Math.Abs(structure.Momentum) >= _effectiveCpMinMomentum &&
                                           Bars.HighPrices[index] < Bars.HighPrices[index - 2] &&
                                           Bars.ClosePrices[index] < Bars.OpenPrices[index] &&
                                           IsHigherTimeframeAligned(index, -1) &&
                                           _regimeState.AllowShortContinuation &&
                                           (index - _lastCpSignalIndex >= _effectiveCpCooldownBars);

                if (shallowPullbackBull)
                {
                    Chart.DrawText($"cont_bull_{index}", "CP↑", index, Bars.LowPrices[index] - 2 * Symbol.PipSize, Color.Lime);
                    _lastCpSignalIndex = index;
                    if (EnableAnalytics)
                        _signalOutcomeTracker.TrackSignal(index, Bars.ClosePrices[index], SignalKind.CpBuy);
                }
                else if (shallowPullbackBear)
                {
                    Chart.DrawText($"cont_bear_{index}", "CP↓", index, Bars.HighPrices[index] + 2 * Symbol.PipSize, Color.OrangeRed);
                    _lastCpSignalIndex = index;
                    if (EnableAnalytics)
                        _signalOutcomeTracker.TrackSignal(index, Bars.ClosePrices[index], SignalKind.CpSell);
                }
            }
        }

        private void Render(int index, StructureSignal structure, OrderFlowSignal orderFlow)
        {
            if (!ShouldRenderVisuals(index))
                return;

            if (EnableStructureSignals)
            {
                if (structure.BosUp)
                    Chart.DrawIcon($"bos_up_{index}", ChartIconType.UpArrow, index, Bars.LowPrices[index] - 3 * Symbol.PipSize, Color.Lime);
                else if (structure.BosDown)
                    Chart.DrawIcon($"bos_down_{index}", ChartIconType.DownArrow, index, Bars.HighPrices[index] + 3 * Symbol.PipSize, Color.OrangeRed);
            }

            if (EnableSupportResistanceLines)
            {
                if (CleanChartMode)
                {
                    RenderPrimaryLevels();
                }
                else
                {
                    int lineIdx = 0;
                    foreach (var level in _srLevels)
                    {
                        Chart.DrawHorizontalLine($"sr_{lineIdx}", level, Color.SlateGray, 1, LineStyle.DotsRare);
                        lineIdx++;
                    }

                    RenderVolumeNodeLevels();
                }
            }

            if (EnableOrderFlowBubbles)
            {
                bool bubbleCooldownPassed = index - _lastBubbleSignalIndex >= _effectiveBubbleCooldownBars;
                bool lifecycleBuyOk = SignalLifecycleEngine.CanEmitBuy(index, _signalLifecycleState, _signalLifecycleRule);
                bool lifecycleSellOk = SignalLifecycleEngine.CanEmitSell(index, _signalLifecycleState, _signalLifecycleRule);

                if (DebugSignals)
                {
                    _lastDebugBuy += $" | LCY:{(lifecycleBuyOk ? "OK" : "BLK")}";
                    _lastDebugSell += $" | LCY:{(lifecycleSellOk ? "OK" : "BLK")}";
                }

                if (orderFlow.AggressiveBuy && bubbleCooldownPassed && lifecycleBuyOk)
                {
                    Chart.DrawText($"bubble_buy_{index}", BubbleGlyph(orderFlow.BubbleTier), index, Bars.LowPrices[index] - 1.7 * Symbol.PipSize, Color.DodgerBlue);
                    _lastBubbleSignalIndex = index;
                    SignalLifecycleEngine.MarkBuy(index, _signalLifecycleState);
                    if (EnableAnalytics)
                        _signalOutcomeTracker.TrackSignal(index, Bars.ClosePrices[index], SignalKind.BubbleBuy);
                }
                else if (orderFlow.AggressiveSell && bubbleCooldownPassed && lifecycleSellOk)
                {
                    Chart.DrawText($"bubble_sell_{index}", BubbleGlyph(orderFlow.BubbleTier), index, Bars.HighPrices[index] + 1.7 * Symbol.PipSize, Color.Magenta);
                    _lastBubbleSignalIndex = index;
                    SignalLifecycleEngine.MarkSell(index, _signalLifecycleState);
                    if (EnableAnalytics)
                        _signalOutcomeTracker.TrackSignal(index, Bars.ClosePrices[index], SignalKind.BubbleSell);
                }
            }

            if (EnableInfoPanel)
            {
                string panel = $"Regime: {_lastRegimeScore:0.00}\n" +
                               $"Momentum: {structure.Momentum:0.0}\n" +
                               $"Depth Imb: {orderFlow.DepthImbalance:0.00}\n" +
                               $"DeltaProxy: {orderFlow.DeltaProxy:0}\n" +
                               $"Node: {_lastNodeContext} ({_lastNodeParticipation:0.00})\n" +
                               $"POC Dist: {_lastPocDistancePips:0.0} pips\n" +
                               $"Sess: {_volumeProfileSnapshot.SessionLabel}\n" +
                               $"NodeGate: {BubbleNodeGate}\n" +
                               $"Conf: {orderFlow.ActiveConfidence}/100 (min {_effectiveMinSignalConfidence})\n" +
                               $"Strict: {StrictTrigger} [{orderFlow.StrictContext}]\n" +
                               $"Regime: {_regimeState.Regime} ({_regimeState.TrendStrength:0.00})\n" +
                               $"NormMom: {_regimeState.NormalizedMomentum:0.00} | Vol: {_regimeState.VolatilityPerBarPips:0.0}p | Sess: {_regimeState.SessionActivityRatio:0.00}\n" +
                               $"Levels: {_levelScore.Source} S:{_levelScore.SupportScore:0} R:{_levelScore.ResistanceScore:0}\n" +
                               $"Preset: {_presetSummary}\n" +
                               $"Eff: BSen {_effectiveBubbleSensitivity:0.00} | CPcd {_effectiveCpCooldownBars} | Bcd {_effectiveBubbleCooldownBars}\n" +
                               $"Eff HTF: {_effectiveHigherTimeframe} ({_effectiveHigherTimeframeFastMaPeriod}/{_effectiveHigherTimeframeSlowMaPeriod})\n" +
                               $"HTF: {_htfStateText}\n" +
                               $"Depth: {(_isDepthAvailable ? "Live" : "N/A (tick-proxy only)")}\n" +
                               $"Ext: {_lastExternalSignal.Status} ({_lastExternalSignal.Confidence:0.00})";

                if (EnableAnalytics)
                {
                    var outcomes = _signalOutcomeTracker.GetSnapshot();
                    panel += "\n\nPerf:\n" +
                             $"All: {outcomes.TotalWins}/{outcomes.TotalResolved} ({outcomes.WinRatePercent:0.0}%)\n" +
                             $"Bubble: {outcomes.BubbleWins}/{outcomes.BubbleResolved} ({outcomes.BubbleWinRatePercent:0.0}%)\n" +
                             $"CP: {outcomes.CpWins}/{outcomes.CpResolved} ({outcomes.CpWinRatePercent:0.0}%)";
                }

                if (DebugSignals)
                {
                    panel += "\n\nDebug:\n" +
                             _lastDebugBuy + "\n" +
                             _lastDebugSell;
                }

                if (ShowGuideOnChart)
                {
                    if (PanelTextMode == InfoTextMode.Detailed)
                    {
                        panel += "\n\nInfo:\n" +
                                 "- BOS arrows = break of recent swing structure\n" +
                                 "- Bubbles = aggressive buyer/seller pressure proxy\n" +
                                 "- FVG boxes = three-candle imbalance zones\n" +
                                 "- CP labels = shallow pullback continuation setups\n" +
                                 "- Ext = external model health/confidence";
                    }
                    else
                    {
                        panel += "\n\nInfo:\n" +
                                 "- BOS = structure breaks\n" +
                                 "- Bubbles = aggression proxy\n" +
                                 "- FVG = imbalance zone\n" +
                                 "- CP = continuation";
                    }
                }

                Chart.DrawStaticText("sp_panel", panel, VerticalAlignment.Top, HorizontalAlignment.Right, Color.White);
                Chart.DrawStaticText("sp_preset_badge", $"Preset Active: {_presetSummary}", VerticalAlignment.Bottom, HorizontalAlignment.Right, GetPresetColor(_activePreset));

                if (ShowGuideOnChart)
                {
                    string featureState =
                        $"Features\n" +
                        $"Structure: {(EnableStructureSignals ? "ON" : "OFF")}\n" +
                        $"S/R Lines: {(EnableSupportResistanceLines ? "ON" : "OFF")}\n" +
                        $"Bubbles: {(EnableOrderFlowBubbles ? "ON" : "OFF")}\n" +
                        $"FVG: {(EnableFvg ? "ON" : "OFF")}\n" +
                        $"Continuation: {(EnableContinuationPattern ? "ON" : "OFF")}\n" +
                        $"External: {(EnableExternalModel ? "ON" : "OFF")}";

                    Chart.DrawStaticText("sp_feature_state", featureState, VerticalAlignment.Top, HorizontalAlignment.Left, Color.LightGray);
                }
                else
                {
                    Chart.DrawStaticText("sp_feature_state", string.Empty, VerticalAlignment.Top, HorizontalAlignment.Left, Color.LightGray);
                }
            }
            else
            {
                Chart.DrawStaticText("sp_panel", string.Empty, VerticalAlignment.Top, HorizontalAlignment.Right, Color.White);
                Chart.DrawStaticText("sp_feature_state", string.Empty, VerticalAlignment.Top, HorizontalAlignment.Left, Color.LightGray);
                Chart.DrawStaticText("sp_preset_badge", string.Empty, VerticalAlignment.Bottom, HorizontalAlignment.Right, Color.White);
            }
        }

        private bool IsSwingHigh(int pivotIndex)
        {
            if (pivotIndex < SwingLookback || pivotIndex + SwingLookback >= Bars.Count)
                return false;

            double pivot = Bars.HighPrices[pivotIndex];
            for (int i = pivotIndex - SwingLookback; i <= pivotIndex + SwingLookback; i++)
            {
                if (i == pivotIndex)
                    continue;
                if (Bars.HighPrices[i] >= pivot)
                    return false;
            }

            return true;
        }

        private bool IsSwingLow(int pivotIndex)
        {
            if (pivotIndex < SwingLookback || pivotIndex + SwingLookback >= Bars.Count)
                return false;

            double pivot = Bars.LowPrices[pivotIndex];
            for (int i = pivotIndex - SwingLookback; i <= pivotIndex + SwingLookback; i++)
            {
                if (i == pivotIndex)
                    continue;
                if (Bars.LowPrices[i] <= pivot)
                    return false;
            }

            return true;
        }

        private void PushSrLevel(double level)
        {
            _srLevels.Enqueue(level);
            while (_srLevels.Count > MaxSrLines)
                _srLevels.Dequeue();
        }

        private bool ShouldRenderVisuals(int index)
        {
            int firstVisibleIndex = Math.Max(0, Bars.Count - _effectiveMaxVisualBars);
            return index >= firstVisibleIndex;
        }

        private void UpdateRegimeNormalization(int index, out double volatilityPerBarPips, out double sessionActivityRatio)
        {
            double rangePips = Math.Max(Bars.HighPrices[index] - Bars.LowPrices[index], Symbol.PipSize) / Symbol.PipSize;
            _recentRangePips.Enqueue(rangePips);
            while (_recentRangePips.Count > _effectiveRegimeVolatilityWindow)
                _recentRangePips.Dequeue();

            double rangeSum = 0;
            foreach (var value in _recentRangePips)
                rangeSum += value;
            volatilityPerBarPips = _recentRangePips.Count > 0 ? rangeSum / _recentRangePips.Count : 1.0;

            double tickVolume = Bars.TickVolumes[index];
            _recentTickVolumes.Enqueue(tickVolume);
            while (_recentTickVolumes.Count > _effectiveRegimeSessionWindow)
                _recentTickVolumes.Dequeue();

            double volumeSum = 0;
            foreach (var value in _recentTickVolumes)
                volumeSum += value;

            double averageVolume = _recentTickVolumes.Count > 0 ? volumeSum / _recentTickVolumes.Count : Math.Max(1.0, tickVolume);
            sessionActivityRatio = averageVolume > 0 ? tickVolume / averageVolume : 1.0;
            sessionActivityRatio = Math.Max(0.25, Math.Min(4.0, sessionActivityRatio));
        }

        private bool IsHigherTimeframeAligned(int index, int direction)
        {
            if (!_effectiveUseHigherTimeframeFilter || _higherTimeframeBars == null || _higherTimeframeFastMa == null || _higherTimeframeSlowMa == null)
            {
                _htfStateText = "Disabled";
                return true;
            }

            int htfIndex = _higherTimeframeBars.OpenTimes.GetIndexByTime(Bars.OpenTimes[index]);
            if (htfIndex < 0 || htfIndex < _effectiveHigherTimeframeSlowMaPeriod)
            {
                _htfStateText = "Warming";
                return false;
            }

            double fast = _higherTimeframeFastMa.Result[htfIndex];
            double slow = _higherTimeframeSlowMa.Result[htfIndex];

            if (fast > slow)
            {
                _htfStateText = "Bull";
                return direction > 0;
            }

            if (fast < slow)
            {
                _htfStateText = "Bear";
                return direction < 0;
            }

            _htfStateText = "Flat";
            return false;
        }

        private void UpdateVolumeProfile(int index)
        {
            if (!EnableVolumeNodes || _volumeProfileEngine == null)
            {
                _volumeProfileSnapshot = new VolumeProfileSnapshot { HasData = false, SessionLabel = "Disabled" };
                return;
            }

            _volumeProfileEngine.Update(
                Bars.OpenTimes[index],
                Bars.LowPrices[index],
                Bars.HighPrices[index],
                Bars.ClosePrices[index],
                Bars.TickVolumes[index]);

            _volumeProfileSnapshot = _volumeProfileEngine.BuildSnapshot(Bars.OpenTimes[index], HvnPercentile, LvnPercentile);
        }

        private void RenderVolumeNodeLevels()
        {
            if (!EnableVolumeNodes || _volumeProfileSnapshot == null || !_volumeProfileSnapshot.HasData)
                return;

            Chart.DrawHorizontalLine("vp_poc", _volumeProfileSnapshot.PocPrice, Color.Gold, 2, LineStyle.Solid);

            int currentHvnCount = 0;
            for (int i = 0; i < _volumeProfileSnapshot.HvnPrices.Count && i < 8; i++)
            {
                Chart.DrawHorizontalLine($"vp_hvn_{i}", _volumeProfileSnapshot.HvnPrices[i], Color.CornflowerBlue, 1, LineStyle.Solid);
                currentHvnCount++;
            }

            for (int i = currentHvnCount; i < _lastRenderedHvnCount; i++)
                Chart.RemoveObject($"vp_hvn_{i}");

            _lastRenderedHvnCount = currentHvnCount;

            int currentLvnCount = 0;
            for (int i = 0; i < _volumeProfileSnapshot.LvnPrices.Count && i < 8; i++)
            {
                Chart.DrawHorizontalLine($"vp_lvn_{i}", _volumeProfileSnapshot.LvnPrices[i], Color.IndianRed, 1, LineStyle.Dots);
                currentLvnCount++;
            }

            for (int i = currentLvnCount; i < _lastRenderedLvnCount; i++)
                Chart.RemoveObject($"vp_lvn_{i}");

            _lastRenderedLvnCount = currentLvnCount;
        }

        private void RenderPrimaryLevels()
        {
            if (_levelScore == null)
                return;

            if (!double.IsNaN(_levelScore.PrimarySupport))
                Chart.DrawHorizontalLine("primary_support", _levelScore.PrimarySupport, Color.MediumSeaGreen, 2, LineStyle.Solid);

            if (!double.IsNaN(_levelScore.PrimaryResistance))
                Chart.DrawHorizontalLine("primary_resistance", _levelScore.PrimaryResistance, Color.OrangeRed, 2, LineStyle.Solid);

            if (EnableVolumeNodes && _volumeProfileSnapshot != null && _volumeProfileSnapshot.HasData)
                Chart.DrawHorizontalLine("primary_poc", _volumeProfileSnapshot.PocPrice, Color.Gold, 2, LineStyle.Solid);

            if (ShowSecondaryLevels)
                RenderVolumeNodeLevels();
        }

        private void GetCurrentHtfMaValues(int index, out double fast, out double slow)
        {
            fast = Bars.ClosePrices[index];
            slow = Bars.ClosePrices[index];

            if (!_effectiveUseHigherTimeframeFilter || _higherTimeframeBars == null || _higherTimeframeFastMa == null || _higherTimeframeSlowMa == null)
                return;

            int htfIndex = _higherTimeframeBars.OpenTimes.GetIndexByTime(Bars.OpenTimes[index]);
            if (htfIndex < 0)
                return;

            fast = _higherTimeframeFastMa.Result[htfIndex];
            slow = _higherTimeframeSlowMa.Result[htfIndex];
        }

        private int GetBubbleTier(double nodeParticipation, double bubbleMultiple)
        {
            double normalizedMultiple = Math.Min(1.5, bubbleMultiple) / 1.5;
            double composite = 0.6 * nodeParticipation + 0.4 * normalizedMultiple;

            if (composite >= 0.85)
                return 3;
            if (composite >= 0.55)
                return 2;
            return 1;
        }

        private string BuildSignalDebugLine(
            string side,
            bool directionPass,
            bool corePass,
            bool nodePass,
            bool nodeContextPass,
            bool htfPass,
            bool strictPass,
            int confidence,
            int minConfidence,
            string regime,
            bool regimeContinuationAllow)
        {
            return $"{side}: Dir:{(directionPass ? "OK" : "BLK")}" +
                   $" Core:{(corePass ? "OK" : "BLK")}" +
                   $" Node:{(nodePass ? "OK" : "BLK")}" +
                   $" Ctx:{(nodeContextPass ? "OK" : "BLK")}" +
                   $" HTF:{(htfPass ? "OK" : "BLK")}" +
                   $" Strict:{(strictPass ? "OK" : "BLK")}" +
                   $" Conf:{confidence}/{minConfidence}" +
                   $" Reg:{regime}:{(regimeContinuationAllow ? "OK" : "BLK")}";
        }

        private bool EvaluateNodeContextGate(bool nearHvn, bool nearLvn, double bubbleMultiple, double nodeParticipation)
        {
            if (!EnableVolumeNodes || BubbleNodeGate == BubbleNodeGateMode.Off)
                return true;

            if (nearHvn || nearLvn)
                return true;

            if (BubbleNodeGate == BubbleNodeGateMode.PreferNodes)
            {
                bool strongImpulseAway = bubbleMultiple >= (_effectiveBubbleSensitivity + 0.35) &&
                                         nodeParticipation >= Math.Max(0.05, MinNodeParticipationRatio * 0.5);
                return strongImpulseAway;
            }

            return false;
        }

        private string BubbleGlyph(int tier)
        {
            if (tier >= 3)
                return "●●●";
            if (tier == 2)
                return "●●";
            return "●";
        }

        private void ApplyPresetSettings()
        {
            var selectedPreset = SelectedPreset;
            _activePreset = selectedPreset;

            _effectiveMaxVisualBars = MaxVisualBars;
            _effectiveBubbleCooldownBars = BubbleCooldownBars;
            _effectiveCpCooldownBars = CpCooldownBars;
            _effectiveHigherTimeframeFastMaPeriod = HigherTimeframeFastMaPeriod;
            _effectiveHigherTimeframeSlowMaPeriod = HigherTimeframeSlowMaPeriod;
            _effectiveBubbleSensitivity = BubbleSensitivity;
            _effectiveMinBubbleBodyPressure = MinBubbleBodyPressure;
            _effectiveMinDepthImbalanceForBubble = MinDepthImbalanceForBubble;
            _effectiveCpMinMomentum = CpMinMomentum;
            _effectiveMinSignalConfidence = MinSignalConfidence;
            _effectiveMinBreakMovePips = MinBreakMovePips;
            _effectiveSignalCooldownBars = SignalCooldownBars;
            _effectiveRegimeChopThreshold = RegimeChopThreshold;
            _effectiveRegimeHysteresis = RegimeHysteresis;
            _effectiveRegimeVolatilityWindow = RegimeVolatilityWindow;
            _effectiveRegimeSessionWindow = RegimeSessionWindow;
            _effectiveUseHigherTimeframeFilter = UseHigherTimeframeFilter;
            _effectiveHigherTimeframe = HigherTimeframe;

            var profile = GetPresetProfile(selectedPreset);
            if (profile == null)
            {
                _presetSummary = "Custom";
                return;
            }

            ApplyProfile(profile);
            _presetSummary = profile.Name;
        }

        private PresetProfile GetPresetProfile(SignalPreset preset)
        {
            if (preset == SignalPreset.Custom)
                return null;

            if (preset == SignalPreset.Scalp)
            {
                return new PresetProfile
                {
                    Name = "Scalp",
                    BubbleSensitivity = 2.4,
                    MinBubbleBodyPressure = 0.56,
                    MinDepthImbalanceForBubble = 0.14,
                    CpMinMomentum = 2.0,
                    CpCooldownBars = 12,
                    BubbleCooldownBars = 8,
                    MaxVisualBars = 280,
                    MinSignalConfidence = 85,
                    MinBreakMovePips = 2.8,
                    SignalCooldownBars = 10,
                    RegimeChopThreshold = 1.3,
                    RegimeHysteresis = 0.22,
                    RegimeVolatilityWindow = 35,
                    RegimeSessionWindow = 55,
                    UseHigherTimeframeFilter = true,
                    HigherTimeframe = TimeFrame.Minute30,
                    HigherTimeframeFastMaPeriod = 34,
                    HigherTimeframeSlowMaPeriod = 89
                };
            }

            if (preset == SignalPreset.Intraday)
            {
                return new PresetProfile
                {
                    Name = "Intraday",
                    BubbleSensitivity = 1.95,
                    MinBubbleBodyPressure = 0.48,
                    MinDepthImbalanceForBubble = 0.09,
                    CpMinMomentum = 1.35,
                    CpCooldownBars = 8,
                    BubbleCooldownBars = 6,
                    MaxVisualBars = 450,
                    MinSignalConfidence = 79,
                    MinBreakMovePips = 2.2,
                    SignalCooldownBars = 7,
                    RegimeChopThreshold = 0.95,
                    RegimeHysteresis = 0.17,
                    RegimeVolatilityWindow = 55,
                    RegimeSessionWindow = 90,
                    UseHigherTimeframeFilter = true,
                    HigherTimeframe = TimeFrame.Hour,
                    HigherTimeframeFastMaPeriod = 34,
                    HigherTimeframeSlowMaPeriod = 89
                };
            }

            if (preset == SignalPreset.Swing)
            {
                return new PresetProfile
                {
                    Name = "Swing",
                    BubbleSensitivity = 1.6,
                    MinBubbleBodyPressure = 0.40,
                    MinDepthImbalanceForBubble = 0.05,
                    CpMinMomentum = 0.9,
                    CpCooldownBars = 14,
                    BubbleCooldownBars = 9,
                    MaxVisualBars = 1200,
                    MinSignalConfidence = 72,
                    MinBreakMovePips = 3.1,
                    SignalCooldownBars = 11,
                    RegimeChopThreshold = 0.75,
                    RegimeHysteresis = 0.14,
                    RegimeVolatilityWindow = 100,
                    RegimeSessionWindow = 140,
                    UseHigherTimeframeFilter = true,
                    HigherTimeframe = TimeFrame.Hour4,
                    HigherTimeframeFastMaPeriod = 50,
                    HigherTimeframeSlowMaPeriod = 200
                };
            }

            return null;
        }

        private void ApplyProfile(PresetProfile profile)
        {
            _effectiveBubbleSensitivity = profile.BubbleSensitivity;
            _effectiveMinBubbleBodyPressure = profile.MinBubbleBodyPressure;
            _effectiveMinDepthImbalanceForBubble = profile.MinDepthImbalanceForBubble;
            _effectiveCpMinMomentum = profile.CpMinMomentum;
            _effectiveCpCooldownBars = profile.CpCooldownBars;
            _effectiveBubbleCooldownBars = profile.BubbleCooldownBars;
            _effectiveMaxVisualBars = profile.MaxVisualBars;
            _effectiveMinSignalConfidence = profile.MinSignalConfidence;
            _effectiveMinBreakMovePips = profile.MinBreakMovePips;
            _effectiveSignalCooldownBars = profile.SignalCooldownBars;
            _effectiveRegimeChopThreshold = profile.RegimeChopThreshold;
            _effectiveRegimeHysteresis = profile.RegimeHysteresis;
            _effectiveRegimeVolatilityWindow = profile.RegimeVolatilityWindow;
            _effectiveRegimeSessionWindow = profile.RegimeSessionWindow;
            _effectiveUseHigherTimeframeFilter = profile.UseHigherTimeframeFilter;
            _effectiveHigherTimeframe = profile.HigherTimeframe;
            _effectiveHigherTimeframeFastMaPeriod = profile.HigherTimeframeFastMaPeriod;
            _effectiveHigherTimeframeSlowMaPeriod = profile.HigherTimeframeSlowMaPeriod;
        }

        private Color GetPresetColor(SignalPreset preset)
        {
            if (preset == SignalPreset.Scalp)
                return Color.OrangeRed;
            if (preset == SignalPreset.Intraday)
                return Color.DeepSkyBlue;
            if (preset == SignalPreset.Swing)
                return Color.MediumSeaGreen;
            return Color.Gold;
        }

        public enum SignalPreset
        {
            Custom,
            Scalp,
            Intraday,
            Swing
        }

        public enum InfoTextMode
        {
            Compact,
            Detailed
        }

        public enum BubbleNodeGateMode
        {
            StrictNodes,
            PreferNodes,
            Off
        }

        private sealed class PresetProfile
        {
            public string Name { get; set; }
            public double BubbleSensitivity { get; set; }
            public double MinBubbleBodyPressure { get; set; }
            public double MinDepthImbalanceForBubble { get; set; }
            public double CpMinMomentum { get; set; }
            public int CpCooldownBars { get; set; }
            public int BubbleCooldownBars { get; set; }
            public int MaxVisualBars { get; set; }
            public int MinSignalConfidence { get; set; }
            public double MinBreakMovePips { get; set; }
            public int SignalCooldownBars { get; set; }
            public double RegimeChopThreshold { get; set; }
            public double RegimeHysteresis { get; set; }
            public int RegimeVolatilityWindow { get; set; }
            public int RegimeSessionWindow { get; set; }
            public bool UseHigherTimeframeFilter { get; set; }
            public TimeFrame HigherTimeframe { get; set; }
            public int HigherTimeframeFastMaPeriod { get; set; }
            public int HigherTimeframeSlowMaPeriod { get; set; }
        }

        private sealed class StructureSignal
        {
            public bool IsSwingHigh { get; set; }
            public bool IsSwingLow { get; set; }
            public bool BosUp { get; set; }
            public bool BosDown { get; set; }
            public double Momentum { get; set; }
            public double RegimeScore { get; set; }
        }

        private sealed class OrderFlowSignal
        {
            public double DepthImbalance { get; set; }
            public double DeltaProxy { get; set; }
            public double PressureScore { get; set; }
            public bool AggressiveBuy { get; set; }
            public bool AggressiveSell { get; set; }
            public int BubbleTier { get; set; }
            public bool NearHvn { get; set; }
            public bool NearLvn { get; set; }
            public double NodeParticipation { get; set; }
            public int BuyConfidence { get; set; }
            public int SellConfidence { get; set; }
            public int ActiveConfidence { get; set; }
            public string StrictContext { get; set; }
        }
    }
}
