namespace ChillZone.Core.Events
{
    /// <summary>Raised whenever the running total score changes.</summary>
    public struct ScoreUpdatedEvent
    {
        public int TotalScore;
        public int ThrowCount;
    }

    /// <summary>Raised when a run ends (miss or manual reset).</summary>
    public struct RunEndedEvent
    {
        public int FinalScore;
        public int ThrowCount;
        public float ElapsedSeconds;
    }
}
