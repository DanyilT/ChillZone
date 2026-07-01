namespace ChillZone.Player.Achievements
{
    /// <summary>
    /// Immutable snapshot of run/progress stats that <see cref="AchievementEvaluator"/> checks
    /// against. Built by AchievementService from gameplay events.
    /// </summary>
    public readonly struct RunStats
    {
        /// <summary>Points this run (running total mid-run, final score at run end).</summary>
        public readonly int Score;
        /// <summary>Elapsed run time in seconds (only meaningful when <see cref="IsRunEnd"/>).</summary>
        public readonly float TimeSeconds;
        /// <summary>Throws taken so far this run.</summary>
        public readonly int Throws;
        /// <summary>Consecutive baskets this run.</summary>
        public readonly int Combo;
        /// <summary>True only when this snapshot is taken at the end of a run.</summary>
        public readonly bool IsRunEnd;

        public RunStats(int score, float timeSeconds, int throws, int combo, bool isRunEnd)
        {
            Score = score;
            TimeSeconds = timeSeconds;
            Throws = throws;
            Combo = combo;
            IsRunEnd = isRunEnd;
        }
    }
}
