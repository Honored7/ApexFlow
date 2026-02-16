using System;

namespace cAlgo.Indicators
{
    public enum ExternalProviderStatus
    {
        Disabled,
        Healthy,
        Stale,
        Timeout,
        Error
    }

    public sealed class ExternalSignalRequest
    {
        public string SymbolName { get; set; }
        public string TimeFrame { get; set; }
        public DateTime BarTime { get; set; }
        public double Open { get; set; }
        public double High { get; set; }
        public double Low { get; set; }
        public double Close { get; set; }
        public double TickVolume { get; set; }
        public double LocalScore { get; set; }
    }

    public sealed class ExternalSignalResponse
    {
        public ExternalProviderStatus Status { get; set; }
        public double ExternalScore { get; set; }
        public double Confidence { get; set; }
        public string ModelVersion { get; set; }
        public DateTime EventTime { get; set; }

        public static ExternalSignalResponse Disabled()
        {
            return new ExternalSignalResponse
            {
                Status = ExternalProviderStatus.Disabled,
                ExternalScore = 0,
                Confidence = 0,
                ModelVersion = "disabled",
                EventTime = DateTime.MinValue
            };
        }
    }

    public interface IExternalSignalProvider
    {
        ExternalSignalResponse GetSignal(ExternalSignalRequest request);
    }

    public sealed class DisabledExternalSignalProvider : IExternalSignalProvider
    {
        public ExternalSignalResponse GetSignal(ExternalSignalRequest request)
        {
            return ExternalSignalResponse.Disabled();
        }
    }
}
