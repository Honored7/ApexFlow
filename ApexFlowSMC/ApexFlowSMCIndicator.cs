// ═══════════════════════════════════════════════════════════════════════════
//  ApexFlow Smart Money Indicator — Manual Trading Edition
//  Independent chart overlay with Market Structure, Order Flow Bubbles,
//  Volume Profile, Regime Detection, and Confluence Signal Arrows.
//
//  Uses the same engines as the bot so chart visuals match execution logic.
// ═══════════════════════════════════════════════════════════════════════════

using System;
using System.Collections.Generic;
using System.Linq;
using cAlgo.API;
using cAlgo.API.Indicators;
using cAlgo.API.Internals;

namespace cAlgo.Indicators
{
    [Indicator(IsOverlay = true, AccessRights = AccessRights.None, TimeZone = TimeZones.UTC)]
    public class ApexFlowSmartMoneyIndicator : Indicator
    {
        // ═══════════════════════════════════════════════════════════════
        //  PARAMETERS
        // ═══════════════════════════════════════════════════════════════

        // ── Market Structure ──
        [Parameter("Swing Lookback", Group = "Market Structure", DefaultValue = 5, MinValue = 2, MaxValue = 20)]
        public int SwingLookback { get; set; }

        [Parameter("Show Order Blocks", Group = "Market Structure", DefaultValue = true)]
        public bool ShowOrderBlocks { get; set; }

        [Parameter("Show FVG Zones", Group = "Market Structure", DefaultValue = true)]
        public bool ShowFvgZones { get; set; }

        [Parameter("Show BOS / ChoCH", Group = "Market Structure", DefaultValue = true)]
        public bool ShowBosChoch { get; set; }

        [Parameter("Show Liquidity Sweeps", Group = "Market Structure", DefaultValue = true)]
        public bool ShowLiquiditySweeps { get; set; }

        [Parameter("Min Sweep Pips", Group = "Market Structure", DefaultValue = 2.0, MinValue = 0.1, MaxValue = 100)]
        public double MinSweepPips { get; set; }

        [Parameter("Max OB Zones", Group = "Market Structure", DefaultValue = 6, MinValue = 1, MaxValue = 20)]
        public int MaxObZones { get; set; }

        [Parameter("Max FVG Zones", Group = "Market Structure", DefaultValue = 6, MinValue = 1, MaxValue = 20)]
        public int MaxFvgZones { get; set; }

        // ── Order Flow Bubbles ──
        [Parameter("Show Flow Bubbles", Group = "Order Flow", DefaultValue = true)]
        public bool ShowFlowBubbles { get; set; }

        [Parameter("Bubble Lookback", Group = "Order Flow", DefaultValue = 20, MinValue = 5, MaxValue = 100)]
        public int BubbleLookback { get; set; }

        [Parameter("Aggression Threshold", Group = "Order Flow", DefaultValue = 1.2, MinValue = 0.5, MaxValue = 5.0)]
        public double AggressionThreshold { get; set; }

        [Parameter("Min Volume Mult", Group = "Order Flow", DefaultValue = 1.2, MinValue = 1.0, MaxValue = 5.0)]
        public double MinBubbleVolumeMult { get; set; }

        [Parameter("Max Bubbles Visible", Group = "Order Flow", DefaultValue = 40, MinValue = 5, MaxValue = 200)]
        public int MaxBubblesVisible { get; set; }

        [Parameter("Show Absorption", Group = "Order Flow", DefaultValue = true)]
        public bool ShowAbsorption { get; set; }

        [Parameter("Show Exhaustion", Group = "Order Flow", DefaultValue = true)]
        public bool ShowExhaustion { get; set; }

        // ── Volume Profile ──
        [Parameter("Show Volume Profile", Group = "Volume Profile", DefaultValue = true)]
        public bool ShowVolumeProfile { get; set; }

        [Parameter("Bin Size (price)", Group = "Volume Profile", DefaultValue = 0.5, MinValue = 0.01, MaxValue = 100)]
        public double VpBinSize { get; set; }

        [Parameter("HVN Percentile", Group = "Volume Profile", DefaultValue = 0.75, MinValue = 0.5, MaxValue = 0.99)]
        public double HvnPercentile { get; set; }

        [Parameter("LVN Percentile", Group = "Volume Profile", DefaultValue = 0.25, MinValue = 0.01, MaxValue = 0.45)]
        public double LvnPercentile { get; set; }

        [Parameter("Profile London", Group = "Volume Profile", DefaultValue = true)]
        public bool VpLondon { get; set; }

        [Parameter("Profile New York", Group = "Volume Profile", DefaultValue = true)]
        public bool VpNewYork { get; set; }

        [Parameter("Profile Asia", Group = "Volume Profile", DefaultValue = true)]
        public bool VpAsia { get; set; }

        // ── Regime Detection ──
        [Parameter("Show Regime Label", Group = "Regime", DefaultValue = true)]
        public bool ShowRegimeLabel { get; set; }

        [Parameter("Show Donchian Channel", Group = "Regime", DefaultValue = true)]
        public bool ShowDonchian { get; set; }

        [Parameter("Show Bollinger Bands", Group = "Regime", DefaultValue = false)]
        public bool ShowBollinger { get; set; }

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

        // ── Signal Arrows ──
        [Parameter("Show Signal Arrows", Group = "Signals", DefaultValue = true)]
        public bool ShowSignalArrows { get; set; }

        [Parameter("Min Confluence Score", Group = "Signals", DefaultValue = 2.5, MinValue = 1.0, MaxValue = 8.0)]
        public double MinConfluence { get; set; }

        [Parameter("Signal Cooldown (bars)", Group = "Signals", DefaultValue = 6, MinValue = 1, MaxValue = 50)]
        public int SignalCooldownBars { get; set; }

        // ── Key Levels ──
        [Parameter("Show Prev Day H/L", Group = "Key Levels", DefaultValue = true)]
        public bool ShowPrevDayHL { get; set; }

        [Parameter("Show Session Kill Zones", Group = "Key Levels", DefaultValue = true)]
        public bool ShowSessionKillZones { get; set; }

        // ── Alerts ──
        [Parameter("Sound Alert on Signal", Group = "Alerts", DefaultValue = true)]
        public bool AlertOnSignal { get; set; }

        // ── Info Panel ──
        [Parameter("Show Info Panel", Group = "Display", DefaultValue = true)]
        public bool ShowInfoPanel { get; set; }

        // ═══════════════════════════════════════════════════════════════
        //  OUTPUTS — Donchian + Bollinger
        // ═══════════════════════════════════════════════════════════════

        [Output("Donchian Upper", LineColor = "5500AAFF", LineStyle = LineStyle.Dots, Thickness = 1)]
        public IndicatorDataSeries DonchianUpper { get; set; }

        [Output("Donchian Lower", LineColor = "5500AAFF", LineStyle = LineStyle.Dots, Thickness = 1)]
        public IndicatorDataSeries DonchianLower { get; set; }

        [Output("Donchian Mid", LineColor = "33888888", LineStyle = LineStyle.DotsRare, Thickness = 1)]
        public IndicatorDataSeries DonchianMid { get; set; }

        [Output("BB Upper", LineColor = "33FF8800", LineStyle = LineStyle.Dots, Thickness = 1)]
        public IndicatorDataSeries BbUpper { get; set; }

        [Output("BB Lower", LineColor = "33FF8800", LineStyle = LineStyle.Dots, Thickness = 1)]
        public IndicatorDataSeries BbLower { get; set; }

        [Output("BB Mid", LineColor = "22FF8800", LineStyle = LineStyle.DotsRare, Thickness = 1)]
        public IndicatorDataSeries BbMid { get; set; }

        // ═══════════════════════════════════════════════════════════════
        //  INTERNAL STATE
        // ═══════════════════════════════════════════════════════════════

        // Engines
        private MarketStructureState _msState;
        private AdxCalculator _adxCalc;
        private DonchianCalculator _donchianCalc;
        private BollingerCalculator _bollingerCalc;
        private RsiCalculator _rsiCalc;
        private SessionVolumeProfileEngine _vpEngine;

        // Chart drawing tracking
        private int _drawnObCount;
        private int _drawnFvgCount;
        private int _drawnSweepCount;
        private readonly List<string> _obNames = new List<string>();
        private readonly List<string> _fvgNames = new List<string>();
        private readonly List<string> _bosNames = new List<string>();
        private readonly List<string> _sweepNames = new List<string>();
        private readonly List<string> _bubbleNames = new List<string>();
        private readonly List<string> _vpNames = new List<string>();
        private readonly List<string> _signalNames = new List<string>();

        // Order flow
        private readonly List<FlowBubbleData> _activeBubbles = new List<FlowBubbleData>();

        // Volume profile
        private VolumeProfileSnapshot _lastSnapshot;
        private string _lastSessionKey = "";

        // Regime
        private string _regimeLabelName;
        private string _regimeDirLabelName;

        // Signal cooldown
        private int _lastSignalIndex = -100;

        // Last structure update (per-bar)
        private StructureUpdate _lastUpdate;

        // Info panel
        private string _infoPanelName;

        // ATR for adaptive Y offsets
        private double _atr;

        // Previous day H/L
        private double _prevDayHigh = double.NaN;
        private double _prevDayLow = double.NaN;
        private DateTime _lastDayDate = DateTime.MinValue;
        private double _todayHigh;
        private double _todayLow = double.MaxValue;
        private string _prevDayHighName;
        private string _prevDayLowName;

        // ═══════════════════════════════════════════════════════════════
        //  LIFECYCLE
        // ═══════════════════════════════════════════════════════════════

        protected override void Initialize()
        {
            _msState = new MarketStructureState();
            _adxCalc = new AdxCalculator(AdxPeriod);
            _donchianCalc = new DonchianCalculator(DonchianPeriod);
            _bollingerCalc = new BollingerCalculator(BollingerPeriod, BollingerStdDev);
            _rsiCalc = new RsiCalculator(RsiPeriod);
            _vpEngine = new SessionVolumeProfileEngine(VpBinSize, VpLondon, VpNewYork, VpAsia);
        }

        public override void Calculate(int index)
        {
            int minBars = Math.Max(SwingLookback * 2 + 5, DonchianPeriod + 2);
            if (index < minBars)
                return;

            double high = Bars.HighPrices[index];
            double low = Bars.LowPrices[index];
            double close = Bars.ClosePrices[index];
            double open = Bars.OpenPrices[index];

            // ── Update calculators ──
            _adxCalc.Update(high, low, close);
            _donchianCalc.Update(high, low);
            _bollingerCalc.Update(close);
            _rsiCalc.Update(close);

            // ── ATR (14-period simple) for adaptive offsets ──
            UpdateAtr(index, 14);

            // ── Previous Day High/Low ──
            if (ShowPrevDayHL)
                UpdatePrevDayHL(index);

            // ── Session Kill Zone markers ──
            if (ShowSessionKillZones)
                UpdateSessionKillZones(index);

            // ── Donchian output ──
            if (ShowDonchian && _donchianCalc.IsReady)
            {
                DonchianUpper[index] = _donchianCalc.UpperBand;
                DonchianLower[index] = _donchianCalc.LowerBand;
                DonchianMid[index] = _donchianCalc.MidBand;
            }

            // ── Bollinger output ──
            if (ShowBollinger && _bollingerCalc.IsReady)
            {
                BbUpper[index] = _bollingerCalc.UpperBand;
                BbLower[index] = _bollingerCalc.LowerBand;
                BbMid[index] = _bollingerCalc.MiddleBand;
            }

            // ── Market Structure ──
            _lastUpdate = MarketStructureEngine.Update(
                _msState, index,
                i => Bars.OpenPrices[i],
                i => Bars.HighPrices[i],
                i => Bars.LowPrices[i],
                i => Bars.ClosePrices[i],
                SwingLookback, Symbol.PipSize, MinSweepPips);

            if (ShowOrderBlocks) DrawOrderBlocks(index);
            if (ShowFvgZones) DrawFvgZones(index);
            if (ShowBosChoch) DrawBosChoch(index);
            if (ShowLiquiditySweeps) DrawLiquiditySweeps(index);

            // ── Order Flow Bubbles ──
            if (ShowFlowBubbles)
                UpdateFlowBubbles(index);

            // ── Volume Profile ──
            if (ShowVolumeProfile)
                UpdateVolumeProfile(index);

            // ── Signal Arrows ──
            if (ShowSignalArrows)
                UpdateSignals(index);

            // ── Regime label + info panel (last bar only) ──
            if (IsLastBar)
            {
                if (ShowRegimeLabel) DrawRegimeLabel(index);
                if (ShowInfoPanel) DrawInfoPanel(index);
            }
        }

        // ═══════════════════════════════════════════════════════════════
        //  MARKET STRUCTURE DRAWING
        // ═══════════════════════════════════════════════════════════════

        private void DrawOrderBlocks(int index)
        {
            if (_msState.ActiveOrderBlocks.Count == _drawnObCount)
                return;
            _drawnObCount = _msState.ActiveOrderBlocks.Count;

            // Prune old drawings
            while (_obNames.Count > MaxObZones * 2)
            {
                Chart.RemoveObject(_obNames[0]);
                _obNames.RemoveAt(0);
            }

            // Draw latest OB
            if (_lastUpdate?.NewOrderBlock != null)
            {
                var ob = _lastUpdate.NewOrderBlock;
                bool isBull = ob.Direction == StructureDirection.Bullish;
                string name = "OB_" + ob.BarIndex + "_" + (isBull ? "B" : "S");

                int safeBarIdx = Math.Max(0, Math.Min(ob.BarIndex, Bars.Count - 1));
                var startTime = Bars.OpenTimes[safeBarIdx];
                var endTime = Bars.OpenTimes[Math.Min(index + 30, Bars.Count - 1)];

                Color color = isBull
                    ? Color.FromArgb(45, 0, 200, 0)
                    : Color.FromArgb(45, 200, 0, 0);

                var rect = Chart.DrawRectangle(name, startTime, ob.Low, endTime, ob.High, color);
                rect.IsFilled = true;
                rect.IsInteractive = false;
                _obNames.Add(name);

                // Label
                string lblName = name + "_L";
                string lblText = isBull ? "▲ OB" : "▼ OB";
                double lblY = isBull ? ob.Low : ob.High;
                var lbl = Chart.DrawText(lblName, lblText, startTime, lblY,
                    isBull ? Color.FromArgb(200, 0, 220, 0) : Color.FromArgb(200, 220, 0, 0));
                lbl.FontSize = 8;
                lbl.IsBold = true;
                _obNames.Add(lblName);
            }
        }

        private void DrawFvgZones(int index)
        {
            if (_msState.ActiveFvgs.Count == _drawnFvgCount)
                return;
            _drawnFvgCount = _msState.ActiveFvgs.Count;

            while (_fvgNames.Count > MaxFvgZones * 2)
            {
                Chart.RemoveObject(_fvgNames[0]);
                _fvgNames.RemoveAt(0);
            }

            if (_lastUpdate?.NewFvg != null)
            {
                var fvg = _lastUpdate.NewFvg;
                bool isBull = fvg.Direction == StructureDirection.Bullish;
                string name = "FVG_" + fvg.BarIndex + "_" + (isBull ? "B" : "S");

                int safeBarIdx = Math.Max(0, Math.Min(fvg.BarIndex, Bars.Count - 1));
                var startTime = Bars.OpenTimes[safeBarIdx];
                var endTime = Bars.OpenTimes[Math.Min(index + 20, Bars.Count - 1)];

                Color color = isBull
                    ? Color.FromArgb(30, 0, 140, 255)
                    : Color.FromArgb(30, 255, 140, 0);

                var rect = Chart.DrawRectangle(name, startTime, fvg.Low, endTime, fvg.High, color);
                rect.IsFilled = true;
                rect.IsInteractive = false;
                _fvgNames.Add(name);

                string lblName = name + "_L";
                double mid = (fvg.Low + fvg.High) / 2.0;
                var lbl = Chart.DrawText(lblName, isBull ? "FVG ▲" : "FVG ▼", startTime, mid,
                    isBull ? Color.DeepSkyBlue : Color.Orange);
                lbl.FontSize = 7;
                _fvgNames.Add(lblName);
            }
        }

        private void DrawBosChoch(int index)
        {
            if (_lastUpdate == null) return;

            double pipSize = Symbol.PipSize;
            if (pipSize <= 0) return;

            if (_lastUpdate.BosUp && !_lastUpdate.IsChoCH)
            {
                string name = "BOS_U_" + index;
                Chart.DrawIcon(name, ChartIconType.UpArrow,
                    Bars.OpenTimes[index], Bars.LowPrices[index] - pipSize * 5, Color.Lime);
                _bosNames.Add(name);

                string lblName = name + "_L";
                var lbl = Chart.DrawText(lblName, "BOS ▲", Bars.OpenTimes[index],
                    Bars.LowPrices[index] - pipSize * 12, Color.Lime);
                lbl.FontSize = 7;
                lbl.IsBold = true;
                _bosNames.Add(lblName);
            }
            else if (_lastUpdate.BosDown && !_lastUpdate.IsChoCH)
            {
                string name = "BOS_D_" + index;
                Chart.DrawIcon(name, ChartIconType.DownArrow,
                    Bars.OpenTimes[index], Bars.HighPrices[index] + pipSize * 5, Color.Red);
                _bosNames.Add(name);

                string lblName = name + "_L";
                var lbl = Chart.DrawText(lblName, "BOS ▼", Bars.OpenTimes[index],
                    Bars.HighPrices[index] + pipSize * 12, Color.Red);
                lbl.FontSize = 7;
                lbl.IsBold = true;
                _bosNames.Add(lblName);
            }

            // ChoCH — stronger emphasis
            if (_lastUpdate.IsChoCH)
            {
                bool up = _lastUpdate.BosUp;
                string name = "ChoCH_" + (up ? "U_" : "D_") + index;
                double y = up
                    ? Bars.LowPrices[index] - pipSize * 18
                    : Bars.HighPrices[index] + pipSize * 18;

                Chart.DrawIcon(name, up ? ChartIconType.UpArrow : ChartIconType.DownArrow,
                    Bars.OpenTimes[index], y, Color.Gold);
                _bosNames.Add(name);

                string lblName = name + "_L";
                double lblY = up ? y - pipSize * 8 : y + pipSize * 8;
                var lbl = Chart.DrawText(lblName, up ? "ChoCH ▲" : "ChoCH ▼",
                    Bars.OpenTimes[index], lblY, Color.Gold);
                lbl.FontSize = 9;
                lbl.IsBold = true;
                _bosNames.Add(lblName);
            }

            // Cleanup old
            while (_bosNames.Count > 80)
            {
                Chart.RemoveObject(_bosNames[0]);
                _bosNames.RemoveAt(0);
            }
        }

        private void DrawLiquiditySweeps(int index)
        {
            if (_msState.RecentSweeps.Count == _drawnSweepCount)
                return;
            _drawnSweepCount = _msState.RecentSweeps.Count;

            if (_lastUpdate?.NewSweep == null) return;

            var sweep = _lastUpdate.NewSweep;
            // Bearish direction = swept buyside (high), Bullish direction = swept sellside (low)
            bool sweptHigh = sweep.Direction == StructureDirection.Bearish;
            int barIdx = Math.Max(0, Math.Min(sweep.BarIndex, Bars.Count - 1));
            double pipSize = Symbol.PipSize;
            if (pipSize <= 0) return;

            string name = "Sweep_" + sweep.BarIndex + "_" + (sweptHigh ? "H" : "L");
            double y = sweptHigh
                ? Bars.HighPrices[barIdx] + pipSize * 4
                : Bars.LowPrices[barIdx] - pipSize * 4;

            Color color = sweptHigh
                ? Color.FromArgb(255, 255, 100, 100)
                : Color.FromArgb(255, 100, 255, 100);
            string text = sweptHigh ? "Sweep High" : "Sweep Low";

            var lbl = Chart.DrawText(name, text, Bars.OpenTimes[barIdx], y, color);
            lbl.FontSize = 8;
            lbl.IsBold = true;
            _sweepNames.Add(name);

            // Swept level line
            string lineName = name + "_LVL";
            Chart.DrawHorizontalLine(lineName, sweep.SweptLevel, color, 1, LineStyle.Dots);
            _sweepNames.Add(lineName);

            while (_sweepNames.Count > 24)
            {
                Chart.RemoveObject(_sweepNames[0]);
                _sweepNames.RemoveAt(0);
            }
        }

        // ═══════════════════════════════════════════════════════════════
        //  ORDER FLOW BUBBLES
        //  Estimates aggressive buyers/sellers from candle anatomy + volume.
        //  Large body + small opposing wick + high volume = aggression.
        //  Detects: Aggressive Buying, Aggressive Selling, Absorption, Exhaustion.
        // ═══════════════════════════════════════════════════════════════

        private void UpdateFlowBubbles(int index)
        {
            if (index < BubbleLookback + 2)
                return;

            double close = Bars.ClosePrices[index];
            double open = Bars.OpenPrices[index];
            double high = Bars.HighPrices[index];
            double low = Bars.LowPrices[index];
            double volume = Bars.TickVolumes[index];
            double range = high - low;
            double pipSize = Symbol.PipSize;

            if (range <= 0 || volume <= 0 || pipSize <= 0)
                return;

            // ── Average volume baseline ──
            double avgVolume = 0;
            for (int i = index - BubbleLookback; i < index; i++)
                avgVolume += Bars.TickVolumes[i];
            avgVolume /= BubbleLookback;

            if (avgVolume <= 0) return;

            double volumeRatio = volume / avgVolume;

            // ── Candle anatomy ──
            double body = Math.Abs(close - open);
            double bodyRatio = body / range;
            double upperWick = high - Math.Max(open, close);
            double lowerWick = Math.Min(open, close) - low;
            bool isBullish = close > open;

            // ── Aggression Score ──
            // Strong body + small opposing wick = aggressive directional flow
            double aggressionScore;
            if (isBullish)
            {
                double upperWickRatio = range > 0 ? upperWick / range : 0;
                aggressionScore = bodyRatio * (1.0 + (1.0 - upperWickRatio) * 0.5) * volumeRatio;
            }
            else
            {
                double lowerWickRatio = range > 0 ? lowerWick / range : 0;
                aggressionScore = bodyRatio * (1.0 + (1.0 - lowerWickRatio) * 0.5) * volumeRatio;
            }

            // ── Absorption: price barely moved but volume spiked ──
            bool isAbsorption = ShowAbsorption && volumeRatio > 1.8 && bodyRatio < 0.35;

            // ── Exhaustion: long wick against direction + high volume ──
            bool isExhaustionTop = ShowExhaustion && isBullish
                && (upperWick / range > 0.45) && volumeRatio > 1.3;
            bool isExhaustionBottom = ShowExhaustion && !isBullish
                && (lowerWick / range > 0.45) && volumeRatio > 1.3;

            // ── Gate: show if any condition met AND volume is above min ──
            bool isAggressive = aggressionScore >= AggressionThreshold && volumeRatio >= MinBubbleVolumeMult;
            bool showBubble = isAggressive || isAbsorption || isExhaustionTop || isExhaustionBottom;

            if (!showBubble)
                return;

            // ── Classify bubble ──
            FlowBubbleType bubbleType;
            Color bubbleColor;
            string bubbleText;
            double bubbleY;
            int fontSize;

            // Use ATR for adaptive Y spacing (works across all instruments)
            double offset = _atr > 0 ? _atr * 0.3 : range * 2;
            double offsetSmall = _atr > 0 ? _atr * 0.15 : range;

            if (isAbsorption)
            {
                bubbleType = FlowBubbleType.Absorption;
                bubbleColor = Color.FromArgb(220, 255, 215, 0);
                bubbleText = "◉ ABS";
                bubbleY = (high + low) / 2.0;
                fontSize = Math.Min(14, 8 + (int)(volumeRatio * 1.5));
            }
            else if (isExhaustionTop)
            {
                bubbleType = FlowBubbleType.Exhaustion;
                bubbleColor = Color.FromArgb(220, 255, 69, 0);
                bubbleText = "◉ EXH";
                bubbleY = high + offset;
                fontSize = Math.Min(13, 8 + (int)(volumeRatio));
            }
            else if (isExhaustionBottom)
            {
                bubbleType = FlowBubbleType.Exhaustion;
                bubbleColor = Color.FromArgb(220, 50, 205, 50);
                bubbleText = "◉ EXH";
                bubbleY = low - offset;
                fontSize = Math.Min(13, 8 + (int)(volumeRatio));
            }
            else if (isBullish)
            {
                bubbleType = FlowBubbleType.AggressiveBuy;
                int g = Math.Min(255, 100 + (int)(aggressionScore * 30));
                int a = Math.Min(255, 150 + (int)(aggressionScore * 20));
                bubbleColor = Color.FromArgb(a, 0, g, 0);
                bubbleText = GetBubbleDot(aggressionScore);
                bubbleY = low - offsetSmall;
                fontSize = Math.Min(16, 8 + (int)(aggressionScore * 2));
            }
            else
            {
                bubbleType = FlowBubbleType.AggressiveSell;
                int r = Math.Min(255, 100 + (int)(aggressionScore * 30));
                int a = Math.Min(255, 150 + (int)(aggressionScore * 20));
                bubbleColor = Color.FromArgb(a, r, 0, 0);
                bubbleText = GetBubbleDot(aggressionScore);
                bubbleY = high + offsetSmall;
                fontSize = Math.Min(16, 8 + (int)(aggressionScore * 2));
            }

            // ── Draw bubble ──
            string name = "Bub_" + index + "_" + (int)bubbleType;
            var textObj = Chart.DrawText(name, bubbleText, Bars.OpenTimes[index], bubbleY, bubbleColor);
            textObj.FontSize = fontSize;
            textObj.IsBold = true;
            textObj.HorizontalAlignment = HorizontalAlignment.Center;
            _bubbleNames.Add(name);

            // Volume ratio label
            string volName = name + "_V";
            string volText = volumeRatio.ToString("F1") + "x";
            double volLabelOffset = _atr > 0 ? _atr * 0.2 : range;
            double volY = isBullish || isExhaustionBottom
                ? bubbleY - volLabelOffset
                : bubbleY + volLabelOffset;
            var volLbl = Chart.DrawText(volName, volText, Bars.OpenTimes[index], volY,
                Color.FromArgb(160, 180, 180, 180));
            volLbl.FontSize = 6;
            volLbl.HorizontalAlignment = HorizontalAlignment.Center;
            _bubbleNames.Add(volName);

            // Track for signal confluence
            _activeBubbles.Add(new FlowBubbleData
            {
                Index = index,
                Type = bubbleType,
                Score = aggressionScore,
                VolumeRatio = volumeRatio,
                IsBullish = isBullish
            });

            // Cleanup
            while (_bubbleNames.Count > MaxBubblesVisible * 2)
            {
                Chart.RemoveObject(_bubbleNames[0]);
                _bubbleNames.RemoveAt(0);
            }
            while (_activeBubbles.Count > MaxBubblesVisible)
                _activeBubbles.RemoveAt(0);
        }

        private static string GetBubbleDot(double score)
        {
            if (score >= 4.0) return "●●";
            if (score >= 3.0) return "●";
            if (score >= 2.5) return "◉";
            return "•";
        }

        // ═══════════════════════════════════════════════════════════════
        //  VOLUME PROFILE (POC / HVN / LVN)
        // ═══════════════════════════════════════════════════════════════

        private void UpdateVolumeProfile(int index)
        {
            var barTime = Bars.OpenTimes[index];
            _vpEngine.Update(barTime, Bars.LowPrices[index], Bars.HighPrices[index],
                Bars.ClosePrices[index], Bars.TickVolumes[index]);

            string sessionKey = GetSessionKey(barTime);
            if (sessionKey == _lastSessionKey)
                return;

            _lastSessionKey = sessionKey;
            _lastSnapshot = _vpEngine.BuildSnapshot(barTime, HvnPercentile, LvnPercentile);

            if (_lastSnapshot != null && _lastSnapshot.HasData)
                DrawVolumeProfileLevels(index);
        }

        private void DrawVolumeProfileLevels(int index)
        {
            // Remove old VP objects
            foreach (var n in _vpNames)
                Chart.RemoveObject(n);
            _vpNames.Clear();

            if (_lastSnapshot == null || !_lastSnapshot.HasData)
                return;

            // POC
            string pocName = "VP_POC";
            Color pocColor = Color.FromArgb(200, 255, 255, 0);
            Chart.DrawHorizontalLine(pocName, _lastSnapshot.PocPrice, pocColor, 2, LineStyle.Solid);
            _vpNames.Add(pocName);

            string pocLbl = pocName + "_L";
            var lbl = Chart.DrawText(pocLbl,
                "POC " + _lastSnapshot.SessionLabel + " (" + _lastSnapshot.PocPrice.ToString("F2") + ")",
                Bars.OpenTimes[index], _lastSnapshot.PocPrice, pocColor);
            lbl.FontSize = 7;
            _vpNames.Add(pocLbl);

            // HVN levels (support/resistance — high traded volume)
            int hvnCount = 0;
            foreach (var hvn in _lastSnapshot.HvnPrices)
            {
                if (++hvnCount > 4) break;
                string hvnName = "VP_HVN_" + hvnCount;
                Chart.DrawHorizontalLine(hvnName, hvn,
                    Color.FromArgb(100, 0, 200, 255), 1, LineStyle.Dots);
                _vpNames.Add(hvnName);
            }

            // LVN levels (fast move zones — low traded volume)
            int lvnCount = 0;
            foreach (var lvn in _lastSnapshot.LvnPrices)
            {
                if (++lvnCount > 4) break;
                string lvnName = "VP_LVN_" + lvnCount;
                Chart.DrawHorizontalLine(lvnName, lvn,
                    Color.FromArgb(70, 255, 100, 100), 1, LineStyle.DotsVeryRare);
                _vpNames.Add(lvnName);
            }
        }

        private static string GetSessionKey(DateTime timeUtc)
        {
            int hour = timeUtc.Hour;
            string session;
            if (hour >= 7 && hour < 13) session = "London";
            else if (hour >= 13 && hour < 22) session = "NewYork";
            else session = "Asia";
            return session + "_" + timeUtc.Date.ToString("yyyyMMdd");
        }

        // ═══════════════════════════════════════════════════════════════
        //  SIGNAL ARROWS — Multi-factor confluence for manual entries
        //
        //  Factors: BOS/ChoCH, OB proximity, FVG fill, Liquidity sweep,
        //           RSI extremes, Volume Profile POC, ADX trend, Flow
        //           bubbles, Candle strength — 9 total factors.
        // ═══════════════════════════════════════════════════════════════

        private void UpdateSignals(int index)
        {
            if (index <= _lastSignalIndex + SignalCooldownBars)
                return;

            if (_lastUpdate == null) return;

            double close = Bars.ClosePrices[index];
            double high = Bars.HighPrices[index];
            double low = Bars.LowPrices[index];
            double pipSize = Symbol.PipSize;
            if (pipSize <= 0) return;

            double adx = _adxCalc.Value;
            double rsi = _rsiCalc.Value;

            double bullScore = 0;
            double bearScore = 0;

            // ── Factor 1: BOS / ChoCH (strongest structural signal) ──
            if (_lastUpdate.BosUp && !_lastUpdate.IsChoCH) bullScore += 1.0;
            if (_lastUpdate.BosDown && !_lastUpdate.IsChoCH) bearScore += 1.0;
            if (_lastUpdate.IsChoCH && _lastUpdate.BosUp) bullScore += 2.0;
            if (_lastUpdate.IsChoCH && _lastUpdate.BosDown) bearScore += 2.0;

            // ── Factor 2: Order Block proximity ──
            foreach (var ob in _msState.ActiveOrderBlocks)
            {
                if (ob.Mitigated) continue;
                if (close >= ob.Low && close <= ob.High)
                {
                    if (ob.Direction == StructureDirection.Bullish) bullScore += 1.0;
                    else bearScore += 1.0;
                }
            }

            // ── Factor 3: FVG confluence ──
            foreach (var fvg in _msState.ActiveFvgs)
            {
                if (fvg.Filled) continue;
                if (close >= fvg.Low && close <= fvg.High)
                {
                    if (fvg.Direction == StructureDirection.Bullish) bullScore += 0.75;
                    else bearScore += 0.75;
                }
            }

            // ── Factor 4: Liquidity sweep (very strong reversal signal) ──
            foreach (var sweep in _msState.RecentSweeps)
            {
                if (index - sweep.BarIndex > 5) continue;
                // Bullish direction sweep = swept sellside = reversal up
                if (sweep.Direction == StructureDirection.Bullish) bullScore += 1.5;
                // Bearish direction sweep = swept buyside = reversal down
                if (sweep.Direction == StructureDirection.Bearish) bearScore += 1.5;
            }

            // ── Factor 5: RSI extremes ──
            if (rsi < 30) bullScore += 0.5;
            else if (rsi < 35) bullScore += 0.25;
            if (rsi > 70) bearScore += 0.5;
            else if (rsi > 65) bearScore += 0.25;

            // ── Factor 6: Volume Profile POC proximity ──
            if (_lastSnapshot != null && _lastSnapshot.HasData)
            {
                double distToPoc = Math.Abs(close - _lastSnapshot.PocPrice) / pipSize;
                if (distToPoc <= 15)
                {
                    bullScore += 0.3;
                    bearScore += 0.3;
                }
            }

            // ── Factor 7: ADX / Regime alignment ──
            if (adx >= 20)
            {
                if (_adxCalc.PlusDI > _adxCalc.MinusDI) bullScore += 0.5;
                else bearScore += 0.5;
            }

            // ── Factor 8: Flow bubble confluence (recent 5 bars) ──
            foreach (var bubble in _activeBubbles)
            {
                if (index - bubble.Index > 5) continue;

                switch (bubble.Type)
                {
                    case FlowBubbleType.AggressiveBuy:
                        bullScore += 0.5;
                        break;
                    case FlowBubbleType.AggressiveSell:
                        bearScore += 0.5;
                        break;
                    case FlowBubbleType.Absorption:
                        // Absorption at highs after buying → bearish; at lows after selling → bullish
                        if (bubble.IsBullish) bearScore += 0.75;
                        else bullScore += 0.75;
                        break;
                    case FlowBubbleType.Exhaustion:
                        if (bubble.IsBullish) bearScore += 1.0;
                        else bullScore += 1.0;
                        break;
                }
            }

            // ── Factor 9: Candle strength ──
            double range = high - low;
            if (range > 0)
            {
                double bodyRatio = Math.Abs(close - Bars.OpenPrices[index]) / range;
                if (close > Bars.OpenPrices[index] && bodyRatio > 0.65) bullScore += 0.3;
                if (close < Bars.OpenPrices[index] && bodyRatio > 0.65) bearScore += 0.3;
            }

            // ── Generate signal arrow if confluence met ──
            if (bullScore >= MinConfluence && bullScore > bearScore + 0.5)
            {
                _lastSignalIndex = index;
                DrawSignalArrow(index, true, bullScore);
            }
            else if (bearScore >= MinConfluence && bearScore > bullScore + 0.5)
            {
                _lastSignalIndex = index;
                DrawSignalArrow(index, false, bearScore);
            }
        }

        private void DrawSignalArrow(int index, bool isBuy, double score)
        {
            string name = "Sig_" + index + "_" + (isBuy ? "B" : "S");
            double offset = _atr > 0 ? _atr * 0.8 : Symbol.PipSize * 30;
            double labelOffset = _atr > 0 ? _atr * 0.35 : Symbol.PipSize * 10;
            double y;
            Color color;
            ChartIconType icon;

            if (isBuy)
            {
                y = Bars.LowPrices[index] - offset;
                color = Color.FromArgb(255, 0, 230, 120);
                icon = ChartIconType.UpTriangle;
            }
            else
            {
                y = Bars.HighPrices[index] + offset;
                color = Color.FromArgb(255, 230, 50, 50);
                icon = ChartIconType.DownTriangle;
            }

            Chart.DrawIcon(name, icon, Bars.OpenTimes[index], y, color);
            _signalNames.Add(name);

            // Score label
            string lblName = name + "_SC";
            double lblY = isBuy ? y - labelOffset : y + labelOffset;
            var lbl = Chart.DrawText(lblName, score.ToString("F1") + "★",
                Bars.OpenTimes[index], lblY, color);
            lbl.FontSize = 8;
            lbl.IsBold = true;
            lbl.HorizontalAlignment = HorizontalAlignment.Center;
            _signalNames.Add(lblName);

            // Sound alert on live bar
            if (AlertOnSignal && IsLastBar)
            {
                string direction = isBuy ? "BUY" : "SELL";
                Notifications.PlaySound("C:\\Windows\\Media\\notify.wav");
                Print("[ApexFlow Signal] " + direction + " | Score: " + score.ToString("F1")
                    + " | Price: " + Bars.ClosePrices[index].ToString("F5"));
            }

            // Cleanup old signals
            while (_signalNames.Count > 60)
            {
                Chart.RemoveObject(_signalNames[0]);
                _signalNames.RemoveAt(0);
            }
        }

        // ═══════════════════════════════════════════════════════════════
        //  REGIME LABEL (top-right corner)
        // ═══════════════════════════════════════════════════════════════

        private void DrawRegimeLabel(int index)
        {
            if (_regimeLabelName != null)
                Chart.RemoveObject(_regimeLabelName);
            if (_regimeDirLabelName != null)
                Chart.RemoveObject(_regimeDirLabelName);

            double adx = _adxCalc.Value;
            double rsi = _rsiCalc.Value;
            bool isBullDir = _adxCalc.PlusDI > _adxCalc.MinusDI;

            // Classify regime + pick color that reflects BOTH regime AND direction
            string regimeText;
            Color regimeColor;

            if (adx >= 30)
            {
                regimeText = "STRONG TREND";
                regimeColor = isBullDir ? Color.Lime : Color.FromArgb(255, 255, 60, 60);
            }
            else if (adx >= 20)
            {
                regimeText = "TREND";
                regimeColor = isBullDir
                    ? Color.FromArgb(255, 0, 200, 120)
                    : Color.FromArgb(255, 230, 100, 80);
            }
            else if (adx >= 15)
            {
                regimeText = "RANGE";
                regimeColor = Color.Orange;
            }
            else
            {
                regimeText = "CHOPPY - NO TRADE";
                regimeColor = Color.Gray;
            }

            // Direction arrow + color
            string dirText;
            Color dirColor;
            if (adx >= 20)
            {
                dirText = isBullDir ? " ▲ BULL" : " ▼ BEAR";
                dirColor = isBullDir ? Color.Lime : Color.FromArgb(255, 255, 60, 60);
            }
            else
            {
                dirText = " ─ NEUTRAL";
                dirColor = Color.Gray;
            }

            // Line 1: regime + ADX (regime color)
            string line1 = regimeText + " | ADX " + adx.ToString("F1");
            // Line 2: direction + RSI (direction color)
            string line2 = dirText + " | RSI " + rsi.ToString("F1");

            _regimeLabelName = "RegimeLabel";
            var lbl = Chart.DrawStaticText(_regimeLabelName, line1,
                VerticalAlignment.Top, HorizontalAlignment.Right, regimeColor);
            lbl.FontSize = 10;
            lbl.IsBold = true;

            _regimeDirLabelName = "RegimeDirLabel";
            var dirLbl = Chart.DrawStaticText(_regimeDirLabelName, "\n" + line2,
                VerticalAlignment.Top, HorizontalAlignment.Right, dirColor);
            dirLbl.FontSize = 10;
            dirLbl.IsBold = true;
        }

        // ═══════════════════════════════════════════════════════════════
        //  INFO PANEL (top-left corner)
        // ═══════════════════════════════════════════════════════════════

        private void DrawInfoPanel(int index)
        {
            if (_infoPanelName != null)
                Chart.RemoveObject(_infoPanelName);

            double adx = _adxCalc.Value;
            double rsi = _rsiCalc.Value;
            double close = Bars.ClosePrices[index];

            var lines = new List<string>();
            lines.Add("═══ ApexFlow SMC ═══");
            lines.Add("");

            // Regime
            string regime = adx >= 30 ? "Strong Trend" : adx >= 20 ? "Trend" : adx >= 15 ? "Range" : "Choppy";
            string dir = _adxCalc.PlusDI > _adxCalc.MinusDI ? "Bullish" : "Bearish";
            lines.Add("Regime: " + regime + " (" + dir + ")");
            lines.Add("ADX: " + adx.ToString("F1") + "  +DI: " + _adxCalc.PlusDI.ToString("F1")
                + "  -DI: " + _adxCalc.MinusDI.ToString("F1"));
            lines.Add("RSI: " + rsi.ToString("F1"));
            lines.Add("");

            // Donchian
            if (_donchianCalc.IsReady)
            {
                lines.Add("Donchian H: " + _donchianCalc.UpperBand.ToString("F2"));
                lines.Add("Donchian L: " + _donchianCalc.LowerBand.ToString("F2"));
            }

            // Bollinger
            if (_bollingerCalc.IsReady)
            {
                lines.Add("BB Upper: " + _bollingerCalc.UpperBand.ToString("F2"));
                lines.Add("BB Mid:   " + _bollingerCalc.MiddleBand.ToString("F2"));
                lines.Add("BB Lower: " + _bollingerCalc.LowerBand.ToString("F2"));
            }
            lines.Add("");

            // Structure
            lines.Add("Trend: " + _msState.PrevailingTrend);
            lines.Add("Active OBs: " + _msState.ActiveOrderBlocks.Count(ob => !ob.Mitigated));
            lines.Add("Active FVGs: " + _msState.ActiveFvgs.Count(f => !f.Filled));
            lines.Add("Recent Sweeps: " + _msState.RecentSweeps.Count);
            lines.Add("");

            // Volume Profile
            if (_lastSnapshot != null && _lastSnapshot.HasData)
            {
                lines.Add("VP Session: " + _lastSnapshot.SessionLabel);
                lines.Add("POC: " + _lastSnapshot.PocPrice.ToString("F2")
                    + " (dist: " + _lastSnapshot.DistanceToPocInPips(close, Symbol.PipSize).ToString("F1") + " pips)");
                lines.Add("HVN nodes: " + _lastSnapshot.HvnPrices.Count);
                lines.Add("LVN nodes: " + _lastSnapshot.LvnPrices.Count);
            }
            lines.Add("");

            // Flow summary
            int recentBuyBubbles = 0, recentSellBubbles = 0, recentAbs = 0, recentExh = 0;
            foreach (var b in _activeBubbles)
            {
                if (index - b.Index > 20) continue;
                switch (b.Type)
                {
                    case FlowBubbleType.AggressiveBuy: recentBuyBubbles++; break;
                    case FlowBubbleType.AggressiveSell: recentSellBubbles++; break;
                    case FlowBubbleType.Absorption: recentAbs++; break;
                    case FlowBubbleType.Exhaustion: recentExh++; break;
                }
            }
            lines.Add("Flow (20 bars):");
            lines.Add("  Aggr Buy: " + recentBuyBubbles + "  Aggr Sell: " + recentSellBubbles);
            lines.Add("  Absorption: " + recentAbs + "  Exhaustion: " + recentExh);

            string panelText = string.Join("\n", lines);

            _infoPanelName = "InfoPanel";
            var panel = Chart.DrawStaticText(_infoPanelName, panelText,
                VerticalAlignment.Top, HorizontalAlignment.Left,
                Color.FromArgb(200, 220, 220, 220));
            panel.FontSize = 9;
        }

        // ═══════════════════════════════════════════════════════════════
        //  ATR CALCULATOR (simple 14-period for adaptive offsets)
        // ═══════════════════════════════════════════════════════════════

        private void UpdateAtr(int index, int period)
        {
            if (index < period + 1) return;

            double sum = 0;
            for (int i = index - period + 1; i <= index; i++)
            {
                double h = Bars.HighPrices[i];
                double l = Bars.LowPrices[i];
                double pc = Bars.ClosePrices[i - 1];
                double tr = Math.Max(h - l, Math.Max(Math.Abs(h - pc), Math.Abs(l - pc)));
                sum += tr;
            }
            _atr = sum / period;
        }

        // ═══════════════════════════════════════════════════════════════
        //  PREVIOUS DAY HIGH / LOW
        // ═══════════════════════════════════════════════════════════════

        private void UpdatePrevDayHL(int index)
        {
            var barDate = Bars.OpenTimes[index].Date;

            if (barDate != _lastDayDate)
            {
                // New day — archive yesterday's range
                if (_lastDayDate != DateTime.MinValue && _todayHigh > 0)
                {
                    _prevDayHigh = _todayHigh;
                    _prevDayLow = _todayLow;
                }
                _lastDayDate = barDate;
                _todayHigh = Bars.HighPrices[index];
                _todayLow = Bars.LowPrices[index];
            }
            else
            {
                if (Bars.HighPrices[index] > _todayHigh) _todayHigh = Bars.HighPrices[index];
                if (Bars.LowPrices[index] < _todayLow) _todayLow = Bars.LowPrices[index];
            }

            // Draw only on last bar
            if (IsLastBar && !double.IsNaN(_prevDayHigh))
            {
                if (_prevDayHighName != null) Chart.RemoveObject(_prevDayHighName);
                if (_prevDayLowName != null) Chart.RemoveObject(_prevDayLowName);

                _prevDayHighName = "PDH";
                _prevDayLowName = "PDL";

                var pdh = Chart.DrawHorizontalLine(_prevDayHighName, _prevDayHigh,
                    Color.FromArgb(180, 255, 165, 0), 2, LineStyle.Lines);
                var pdl = Chart.DrawHorizontalLine(_prevDayLowName, _prevDayLow,
                    Color.FromArgb(180, 100, 149, 237), 2, LineStyle.Lines);

                // Labels
                string pdhLbl = _prevDayHighName + "_L";
                string pdlLbl = _prevDayLowName + "_L";
                Chart.RemoveObject(pdhLbl);
                Chart.RemoveObject(pdlLbl);

                var hl = Chart.DrawText(pdhLbl, "PDH " + _prevDayHigh.ToString("F2"),
                    Bars.OpenTimes[index], _prevDayHigh, Color.FromArgb(220, 255, 165, 0));
                hl.FontSize = 7;
                hl.IsBold = true;

                var ll = Chart.DrawText(pdlLbl, "PDL " + _prevDayLow.ToString("F2"),
                    Bars.OpenTimes[index], _prevDayLow, Color.FromArgb(220, 100, 149, 237));
                ll.FontSize = 7;
                ll.IsBold = true;
            }
        }

        // ═══════════════════════════════════════════════════════════════
        //  SESSION KILL ZONE MARKERS
        // ═══════════════════════════════════════════════════════════════

        private void UpdateSessionKillZones(int index)
        {
            if (index < 2) return;

            var prevTime = Bars.OpenTimes[index - 1];
            var curTime = Bars.OpenTimes[index];
            int prevHour = prevTime.Hour;
            int curHour = curTime.Hour;

            // Detect session open crossings (UTC hours)
            // London Open: 07:00, NY Open: 13:00, Asia Open: 22:00
            TryDrawSessionLine(prevHour, curHour, 7, curTime, index, "London Open",
                Color.FromArgb(60, 0, 200, 255));
            TryDrawSessionLine(prevHour, curHour, 13, curTime, index, "NY Open",
                Color.FromArgb(60, 255, 165, 0));
            TryDrawSessionLine(prevHour, curHour, 22, curTime, index, "Asia Open",
                Color.FromArgb(60, 180, 0, 255));
        }

        private void TryDrawSessionLine(int prevHour, int curHour, int targetHour,
            DateTime curTime, int index, string label, Color color)
        {
            // Check if we crossed the target hour boundary
            bool crossed = (prevHour < targetHour && curHour >= targetHour)
                        || (targetHour == 22 && prevHour < 22 && curHour >= 22);
            if (!crossed) return;

            string name = "KZ_" + label.Replace(" ", "") + "_" + curTime.Date.ToString("yyyyMMdd");
            var line = Chart.DrawVerticalLine(name, curTime,
                color, 1, LineStyle.DotsRare);

            string lblName = name + "_L";
            double lblY = Bars.HighPrices[index] + (_atr > 0 ? _atr * 0.5 : Symbol.PipSize * 20);
            var lbl = Chart.DrawText(lblName, label, curTime, lblY, color);
            lbl.FontSize = 7;
        }

        // ═══════════════════════════════════════════════════════════════
        //  INTERNAL TYPES
        // ═══════════════════════════════════════════════════════════════

        private enum FlowBubbleType
        {
            AggressiveBuy,
            AggressiveSell,
            Absorption,
            Exhaustion
        }

        private sealed class FlowBubbleData
        {
            public int Index { get; set; }
            public FlowBubbleType Type { get; set; }
            public double Score { get; set; }
            public double VolumeRatio { get; set; }
            public bool IsBullish { get; set; }
        }
    }
}
