using UnityEngine;

namespace ChillZone.Content
{
    /// <summary>
    /// Declarative unlock rule attached to a piece of <see cref="UnlockableContent"/>.
    /// Evaluated against the player's progression via <see cref="IPlayerProgress"/>.
    /// </summary>
    [System.Serializable]
    public class UnlockCriteria
    {
        public UnlockType type;

        [Tooltip("Required for MinScore type")]
        public int requiredScore;
        [Tooltip("Required for Currency type")]
        public int requiredCurrency;
        [Tooltip("Required for Achievement type")]
        public string achievementId;
        [Tooltip("Required for Code type")]
        public string requiredCode;

        public bool IsUnlocked(IPlayerProgress progress)
        {
            if (type == UnlockType.Free) return true;
            if (progress == null) return false;

            return type switch
            {
                UnlockType.MinScore    => progress.BestScore >= requiredScore,
                UnlockType.Currency    => progress.SoftCurrency >= requiredCurrency,
                UnlockType.Achievement => progress.HasAchievement(achievementId),
                UnlockType.Code        => progress.HasEnteredCode(requiredCode),
                _                      => false
            };
        }

        /// <summary>Short label of what's required to unlock — shown on a locked item. Code unlocks just read "Special".</summary>
        public string Describe() => type switch
        {
            UnlockType.MinScore    => $"Reach {requiredScore}",
            UnlockType.Currency    => $"{requiredCurrency} coins",
            UnlockType.Achievement => string.IsNullOrWhiteSpace(achievementId) ? "Achievement" : achievementId,
            UnlockType.Code        => "Special",
            _                      => ""
        };
    }

    public enum UnlockType { Free, MinScore, Currency, Achievement, Code }
}
