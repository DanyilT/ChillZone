using ChillZone.Player.Achievements;

namespace ChillZone.Player
{
    /// <summary>
    /// Outcome of redeeming a code via <see cref="PlayerProfileManager.RegisterCode"/>. Carries what the
    /// code unlocked so the UI can show a meaningful confirmation — an achievement's title/description, or a
    /// ball/basket unlock with a picker hint. Codes grant balls/baskets through an achievement's rewards, so
    /// the unlocked content ids are read off that achievement. <see cref="Accepted"/> is false when the code
    /// was empty or had already been redeemed (nothing new to announce).
    /// </summary>
    public readonly struct CodeRedeemResult
    {
        private readonly AchievementDefinition _achievement;

        public bool Accepted { get; }
        public bool HasAchievement => _achievement != null;
        public string AchievementTitle => _achievement?.title;
        public string AchievementDescription => _achievement?.description;
        public string UnlockedBallId => NullIfBlank(_achievement?.relatedBallId);
        public string UnlockedBasketId => NullIfBlank(_achievement?.relatedBasketId);

        private CodeRedeemResult(bool accepted, AchievementDefinition achievement)
        {
            Accepted = accepted;
            _achievement = achievement;
        }

        public static CodeRedeemResult Rejected => new(false, null);

        /// <summary>Code accepted; <paramref name="achievement"/> is the achievement it unlocked (or null).</summary>
        public static CodeRedeemResult Success(AchievementDefinition achievement) => new(true, achievement);

        private static string NullIfBlank(string s) => string.IsNullOrWhiteSpace(s) ? null : s;
    }
}
