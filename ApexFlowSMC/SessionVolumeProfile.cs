using System;
using System.Collections.Generic;
using System.Linq;

namespace cAlgo.Indicators
{
    public sealed class VolumeProfileSnapshot
    {
        public bool HasData { get; set; }
        public double PocPrice { get; set; }
        public double PocVolume { get; set; }
        public double TotalVolume { get; set; }
        public string SessionLabel { get; set; }
        public List<double> HvnPrices { get; } = new List<double>();
        public List<double> LvnPrices { get; } = new List<double>();
        public Dictionary<int, double> BinVolumes { get; } = new Dictionary<int, double>();
        public double BinSize { get; set; }

        public double VolumeRatioAtPrice(double price)
        {
            if (!HasData || PocVolume <= 0 || BinSize <= 0)
                return 0;

            int bin = (int)Math.Floor(price / BinSize);
            if (!BinVolumes.TryGetValue(bin, out double volumeAtPrice))
                return 0;

            return volumeAtPrice / PocVolume;
        }

        public bool IsNearAnyNode(IReadOnlyList<double> nodes, double price, double tolerancePrice)
        {
            for (int i = 0; i < nodes.Count; i++)
            {
                if (Math.Abs(nodes[i] - price) <= tolerancePrice)
                    return true;
            }

            return false;
        }

        public double DistanceToPocInPips(double price, double pipSize)
        {
            if (!HasData || pipSize <= 0)
                return double.NaN;

            return Math.Abs(price - PocPrice) / pipSize;
        }
    }

    public sealed class SessionVolumeProfileEngine
    {
        private readonly Dictionary<string, Dictionary<int, double>> _sessionBins = new Dictionary<string, Dictionary<int, double>>();
        private readonly Queue<string> _sessionOrder = new Queue<string>();

        private readonly double _binSize;
        private readonly bool _enableLondon;
        private readonly bool _enableNewYork;
        private readonly bool _enableAsia;
        private readonly int _maxSessionsToKeep;

        public SessionVolumeProfileEngine(double binSize, bool enableLondon, bool enableNewYork, bool enableAsia, int maxSessionsToKeep = 18)
        {
            _binSize = Math.Max(binSize, 0.0000001);
            _enableLondon = enableLondon;
            _enableNewYork = enableNewYork;
            _enableAsia = enableAsia;
            _maxSessionsToKeep = Math.Max(maxSessionsToKeep, 3);
        }

        public void Update(DateTime barTimeUtc, double low, double high, double close, double tickVolume)
        {
            string sessionName = ResolveSessionName(barTimeUtc);
            if (sessionName == null)
                return;

            string key = BuildSessionKey(barTimeUtc, sessionName);
            var bins = GetOrCreateBins(key);

            int startBin = (int)Math.Floor(Math.Min(low, high) / _binSize);
            int endBin = (int)Math.Floor(Math.Max(low, high) / _binSize);
            int binCount = Math.Max(1, endBin - startBin + 1);

            double safeVolume = Math.Max(1, tickVolume);
            double distributed = safeVolume * 0.7;
            double closeFocus = safeVolume - distributed;
            double perBin = distributed / binCount;

            for (int bin = startBin; bin <= endBin; bin++)
                bins[bin] = bins.TryGetValue(bin, out double current) ? current + perBin : perBin;

            int closeBin = (int)Math.Floor(close / _binSize);
            bins[closeBin] = bins.TryGetValue(closeBin, out double closeCurrent) ? closeCurrent + closeFocus : closeFocus;
        }

        public VolumeProfileSnapshot BuildSnapshot(DateTime barTimeUtc, double hvnPercentile, double lvnPercentile)
        {
            string sessionName = ResolveSessionName(barTimeUtc);
            if (sessionName == null)
                return new VolumeProfileSnapshot { HasData = false, BinSize = _binSize, SessionLabel = "Disabled" };

            string key = BuildSessionKey(barTimeUtc, sessionName);
            if (!_sessionBins.TryGetValue(key, out var bins) || bins.Count < 6)
            {
                return new VolumeProfileSnapshot { HasData = false, BinSize = _binSize, SessionLabel = sessionName + " (warming)" };
            }

            var snapshot = new VolumeProfileSnapshot
            {
                HasData = true,
                BinSize = _binSize,
                SessionLabel = sessionName
            };

            foreach (var pair in bins)
                snapshot.BinVolumes[pair.Key] = pair.Value;

            var orderedByBin = bins.OrderBy(x => x.Key).ToArray();
            var volumes = orderedByBin.Select(x => x.Value).ToArray();

            int pocBin = orderedByBin.OrderByDescending(x => x.Value).First().Key;
            snapshot.PocPrice = BinCenterPrice(pocBin);
            snapshot.PocVolume = orderedByBin.Max(x => x.Value);
            snapshot.TotalVolume = volumes.Sum();

            double hvnThreshold = Quantile(volumes, Math.Clamp(hvnPercentile, 0.5, 0.99));
            double lvnThreshold = Quantile(volumes, Math.Clamp(lvnPercentile, 0.01, 0.45));

            for (int i = 1; i < orderedByBin.Length - 1; i++)
            {
                var previous = orderedByBin[i - 1];
                var current = orderedByBin[i];
                var next = orderedByBin[i + 1];

                if (current.Value >= previous.Value && current.Value >= next.Value && current.Value >= hvnThreshold)
                    snapshot.HvnPrices.Add(BinCenterPrice(current.Key));

                if (current.Value <= previous.Value && current.Value <= next.Value && current.Value <= lvnThreshold)
                    snapshot.LvnPrices.Add(BinCenterPrice(current.Key));
            }

            return snapshot;
        }

        private Dictionary<int, double> GetOrCreateBins(string key)
        {
            if (_sessionBins.TryGetValue(key, out var bins))
                return bins;

            bins = new Dictionary<int, double>();
            _sessionBins[key] = bins;
            _sessionOrder.Enqueue(key);

            while (_sessionOrder.Count > _maxSessionsToKeep)
            {
                string oldKey = _sessionOrder.Dequeue();
                _sessionBins.Remove(oldKey);
            }

            return bins;
        }

        private string ResolveSessionName(DateTime timeUtc)
        {
            int hour = timeUtc.Hour;

            bool inLondon = hour >= 7 && hour < 16;
            bool inNewYork = hour >= 13 && hour < 22;
            bool inAsia = hour >= 22 || hour < 7;

            if (_enableLondon && inLondon)
                return "London";

            if (_enableNewYork && inNewYork)
                return "NewYork";

            if (_enableAsia && inAsia)
                return "Asia";

            return null;
        }

        private string BuildSessionKey(DateTime timeUtc, string sessionName)
        {
            DateTime sessionDate = timeUtc.Date;
            if (sessionName == "Asia" && timeUtc.Hour < 7)
                sessionDate = sessionDate.AddDays(-1);

            return sessionName + "_" + sessionDate.ToString("yyyyMMdd");
        }

        private double BinCenterPrice(int bin)
        {
            return (bin + 0.5) * _binSize;
        }

        private static double Quantile(double[] sortedOrUnsorted, double q)
        {
            if (sortedOrUnsorted.Length == 0)
                return 0;

            var arr = sortedOrUnsorted.OrderBy(x => x).ToArray();
            double pos = (arr.Length - 1) * q;
            int lower = (int)Math.Floor(pos);
            int upper = (int)Math.Ceiling(pos);
            if (lower == upper)
                return arr[lower];

            double fraction = pos - lower;
            return arr[lower] + (arr[upper] - arr[lower]) * fraction;
        }
    }
}
