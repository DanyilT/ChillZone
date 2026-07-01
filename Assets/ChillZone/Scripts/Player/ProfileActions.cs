namespace ChillZone.Player
{
    /// <summary>
    /// Canonical action-type identifiers recorded in the player profile's analytics
    /// action log (<see cref="PlayerProfileData.RecordAction"/>). Centralized so the
    /// strings used for analytics/telemetry stay consistent and greppable.
    /// </summary>
    public static class ProfileActions
    {
        public const string ContentUnlock     = "content-unlock";
        public const string AchievementUnlock = "achievement-unlock";
        public const string CodeEntered       = "code-entered";
        public const string ScoreRecorded     = "score-recorded";
    }
}
