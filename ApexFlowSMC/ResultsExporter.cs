using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace cAlgo.Indicators
{
    /// <summary>
    /// Exports trade results to CSV + JSON summary after backtest or live session.
    /// Files are written to Documents/ApexFlow/Results/ by default.
    /// </summary>
    public static class ResultsExporter
    {
        private static string DefaultOutputDir =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "ApexFlow", "Results");

        /// <summary>
        /// Export individual trades as CSV.
        /// </summary>
        public static string ExportTrades(
            IEnumerable<TradeRecord> trades,
            double accountBalance,
            double accountEquity,
            string label,
            string outputDir = null)
        {
            outputDir ??= DefaultOutputDir;
            Directory.CreateDirectory(outputDir);

            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            var filePath = Path.Combine(outputDir, $"trades_{label}_{timestamp}.csv");

            var sb = new StringBuilder();
            sb.AppendLine("EntryTime,ExitTime,Symbol,Direction,Lots,EntryPrice,ExitPrice,SL,TP,Pips,NetProfit,GrossProfit,Commissions,Swap,BalanceAfter,Label,DurationMins");

            foreach (var t in trades)
            {
                var duration = (t.ExitTime - t.EntryTime).TotalMinutes;
                sb.AppendLine(string.Join(",",
                    t.EntryTime.ToString("yyyy-MM-dd HH:mm:ss"),
                    t.ExitTime.ToString("yyyy-MM-dd HH:mm:ss"),
                    t.Symbol,
                    t.Direction,
                    t.Lots.ToString("F4"),
                    t.EntryPrice.ToString("F5"),
                    t.ExitPrice.ToString("F5"),
                    FormatNullable(t.StopLoss, "F5"),
                    FormatNullable(t.TakeProfit, "F5"),
                    t.Pips.ToString("F1"),
                    t.NetProfit.ToString("F2"),
                    t.GrossProfit.ToString("F2"),
                    t.Commissions.ToString("F2"),
                    t.Swap.ToString("F2"),
                    t.BalanceAfter.ToString("F2"),
                    t.Label ?? "",
                    duration.ToString("F0")));
            }

            File.WriteAllText(filePath, sb.ToString());
            return filePath;
        }

        /// <summary>
        /// Export a JSON summary with key performance metrics.
        /// </summary>
        public static string ExportSummary(
            IEnumerable<TradeRecord> trades,
            double accountBalance,
            double accountEquity,
            double startingBalance,
            string label,
            string outputDir = null)
        {
            outputDir ??= DefaultOutputDir;
            Directory.CreateDirectory(outputDir);

            var tradeList = trades.ToList();
            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            var filePath = Path.Combine(outputDir, $"summary_{label}_{timestamp}.json");

            int total = tradeList.Count;
            int wins = tradeList.Count(t => t.NetProfit > 0);
            int losses = tradeList.Count(t => t.NetProfit <= 0);
            double totalNetProfit = tradeList.Sum(t => t.NetProfit);
            double grossWins = tradeList.Where(t => t.NetProfit > 0).Sum(t => t.NetProfit);
            double grossLosses = tradeList.Where(t => t.NetProfit < 0).Sum(t => Math.Abs(t.NetProfit));
            double profitFactor = grossLosses > 0 ? grossWins / grossLosses : 0;
            double avgWin = wins > 0 ? grossWins / wins : 0;
            double avgLoss = losses > 0 ? grossLosses / losses : 0;
            double expectancy = total > 0 ? totalNetProfit / total : 0;
            double totalPips = tradeList.Sum(t => t.Pips);
            double avgPips = total > 0 ? totalPips / total : 0;

            // Max drawdown from balance curve
            double peak = startingBalance;
            double maxDrawdown = 0;
            double maxDrawdownPct = 0;
            double runningBalance = startingBalance;
            foreach (var t in tradeList.OrderBy(t => t.ExitTime))
            {
                runningBalance += t.NetProfit;
                if (runningBalance > peak) peak = runningBalance;
                double dd = peak - runningBalance;
                if (dd > maxDrawdown)
                {
                    maxDrawdown = dd;
                    maxDrawdownPct = peak > 0 ? dd / peak * 100 : 0;
                }
            }

            // Trade duration stats
            var durations = tradeList.Select(t => (t.ExitTime - t.EntryTime).TotalMinutes).ToList();
            double avgDurationMins = durations.Count > 0 ? durations.Average() : 0;

            // Symbols traded
            var symbols = tradeList.Select(t => t.Symbol).Distinct().OrderBy(s => s).ToArray();

            // Per-symbol breakdown
            var symbolBreakdown = new StringBuilder();
            foreach (var sym in symbols)
            {
                var symTrades = tradeList.Where(t => t.Symbol == sym).ToList();
                int symWins = symTrades.Count(t => t.NetProfit > 0);
                double symPnl = symTrades.Sum(t => t.NetProfit);
                symbolBreakdown.AppendLine($"    \"{sym}\": {{ \"trades\": {symTrades.Count}, \"wins\": {symWins}, \"winRate\": {(symTrades.Count > 0 ? (double)symWins / symTrades.Count * 100 : 0):F1}, \"netProfit\": {symPnl:F2} }},");
            }
            // Remove trailing comma
            string symbolJson = symbolBreakdown.Length > 3
                ? symbolBreakdown.ToString().TrimEnd('\r', '\n', ',') + "\n"
                : "";

            // Date range
            DateTime? firstTrade = tradeList.Count > 0 ? tradeList.Min(t => t.EntryTime) : (DateTime?)null;
            DateTime? lastTrade = tradeList.Count > 0 ? tradeList.Max(t => t.ExitTime) : (DateTime?)null;
            int tradingDays = firstTrade.HasValue && lastTrade.HasValue
                ? (int)(lastTrade.Value.Date - firstTrade.Value.Date).TotalDays + 1
                : 0;

            var json = $@"{{
  ""exportTime"": ""{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC"",
  ""label"": ""{label}"",
  ""dateRange"": {{
    ""from"": ""{firstTrade?.ToString("yyyy-MM-dd") ?? "N/A"}"",
    ""to"": ""{lastTrade?.ToString("yyyy-MM-dd") ?? "N/A"}"",
    ""tradingDays"": {tradingDays}
  }},
  ""account"": {{
    ""startingBalance"": {startingBalance:F2},
    ""endingBalance"": {accountBalance:F2},
    ""endingEquity"": {accountEquity:F2},
    ""returnPct"": {(startingBalance > 0 ? (accountBalance - startingBalance) / startingBalance * 100 : 0):F2}
  }},
  ""performance"": {{
    ""totalTrades"": {total},
    ""wins"": {wins},
    ""losses"": {losses},
    ""winRate"": {(total > 0 ? (double)wins / total * 100 : 0):F2},
    ""netProfit"": {totalNetProfit:F2},
    ""grossWins"": {grossWins:F2},
    ""grossLosses"": {grossLosses:F2},
    ""profitFactor"": {profitFactor:F4},
    ""expectancy"": {expectancy:F2},
    ""avgWin"": {avgWin:F2},
    ""avgLoss"": {avgLoss:F2},
    ""totalPips"": {totalPips:F1},
    ""avgPipsPerTrade"": {avgPips:F1},
    ""maxDrawdown"": {maxDrawdown:F2},
    ""maxDrawdownPct"": {maxDrawdownPct:F2},
    ""avgTradeDurationMins"": {avgDurationMins:F0}
  }},
  ""symbols"": [{string.Join(", ", symbols.Select(s => $"\"{s}\""))}],
  ""perSymbol"": {{
{symbolJson}  }}
}}";

            File.WriteAllText(filePath, json);
            return filePath;
        }

        private static string FormatNullable(double? value, string format)
        {
            return value.HasValue ? value.Value.ToString(format) : "";
        }
    }

    /// <summary>
    /// Simple POCO for trade data — maps from cTrader HistoricalTrade.
    /// </summary>
    public sealed class TradeRecord
    {
        public DateTime EntryTime { get; set; }
        public DateTime ExitTime { get; set; }
        public string Symbol { get; set; }
        public string Direction { get; set; }
        public double Lots { get; set; }
        public double EntryPrice { get; set; }
        public double ExitPrice { get; set; }
        public double? StopLoss { get; set; }
        public double? TakeProfit { get; set; }
        public double Pips { get; set; }
        public double NetProfit { get; set; }
        public double GrossProfit { get; set; }
        public double Commissions { get; set; }
        public double Swap { get; set; }
        public double BalanceAfter { get; set; }
        public string Label { get; set; }
    }
}
