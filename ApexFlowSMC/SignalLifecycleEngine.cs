namespace cAlgo.Indicators
{
    public sealed class SignalLifecycleState
    {
        public int LastBuyIndex { get; set; } = -100000;
        public int LastSellIndex { get; set; } = -100000;
    }

    public sealed class SignalLifecycleRule
    {
        public int MinBarsBetweenSameDirection { get; set; }
        public int MaxSignalAgeBars { get; set; }
    }

    public static class SignalLifecycleEngine
    {
        public static bool CanEmitBuy(int index, SignalLifecycleState state, SignalLifecycleRule rule)
        {
            return index - state.LastBuyIndex >= rule.MinBarsBetweenSameDirection;
        }

        public static bool CanEmitSell(int index, SignalLifecycleState state, SignalLifecycleRule rule)
        {
            return index - state.LastSellIndex >= rule.MinBarsBetweenSameDirection;
        }

        public static void MarkBuy(int index, SignalLifecycleState state)
        {
            state.LastBuyIndex = index;
        }

        public static void MarkSell(int index, SignalLifecycleState state)
        {
            state.LastSellIndex = index;
        }
    }
}
