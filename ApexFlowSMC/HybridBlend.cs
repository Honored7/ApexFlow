using System;

namespace cAlgo.Indicators
{
    public sealed class HybridBlendConfig
    {
        public bool EnableExternalModel { get; set; }
        public double ExternalWeight { get; set; }
        public int MaxExternalSignalAgeSeconds { get; set; }
        public double MinimumExternalConfidence { get; set; }

        public double ClampWeight()
        {
            if (ExternalWeight < 0)
                return 0;
            if (ExternalWeight > 1)
                return 1;
            return ExternalWeight;
        }
    }

    public static class HybridBlend
    {
        public static double ComposeScore(
            double localScore,
            ExternalSignalResponse external,
            HybridBlendConfig config,
            DateTime nowUtc)
        {
            if (!config.EnableExternalModel || external == null)
                return localScore;

            if (external.Status != ExternalProviderStatus.Healthy)
                return localScore;

            if (external.Confidence < config.MinimumExternalConfidence)
                return localScore;

            if ((nowUtc - external.EventTime).TotalSeconds > config.MaxExternalSignalAgeSeconds)
                return localScore;

            double weight = config.ClampWeight();
            return localScore * (1 - weight) + external.ExternalScore * weight;
        }
    }
}
