namespace ChillZone.Content
{
    /// <summary>
    /// Read-only view of player progression that <see cref="UnlockCriteria"/> evaluates
    /// against. Implemented by the player profile. Letting the Content layer depend on
    /// this interface (instead of the concrete profile) keeps the content/progression
    /// dependency acyclic so they can live in separate assemblies.
    /// </summary>
    public interface IPlayerProgress
    {
        /// <summary>Highest score the player has recorded across all runs.</summary>
        int BestScore { get; }

        /// <summary>Soft (earned) currency balance.</summary>
        int SoftCurrency { get; }

        bool HasContentUnlocked(ContentTypes contentType, string contentId);

        bool HasAchievement(string achievementId);

        bool HasEnteredCode(string code);
    }
}
