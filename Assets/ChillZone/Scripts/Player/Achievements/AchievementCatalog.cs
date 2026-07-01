using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ChillZone.Player.Achievements
{
    [CreateAssetMenu(fileName = "AchievementCatalog", menuName = "ChillZone/Profiles/Achievement Catalog")]
    public class AchievementCatalog : ScriptableObject
    {
        public List<AchievementDefinition> achievements = new();

        public AchievementDefinition GetById(string achievementId) =>
            string.IsNullOrWhiteSpace(achievementId) ? null : achievements.FirstOrDefault(achievement =>
                    achievement != null && string.Equals(achievement.achievementId, achievementId, StringComparison.OrdinalIgnoreCase));
    }

    [Serializable]
    public class AchievementDefinition
    {
        public string achievementId;
        public string title;
        [TextArea] public string description;
        public Sprite icon;
        public AchievementUnlockType unlockType;
        public int targetValue;
        public string relatedBallId;
        public string relatedBasketId;
        [Tooltip("Soft currency granted when this achievement unlocks (0 = none).")]
        public int currencyReward;
        public string relatedCode;
    }

    public enum AchievementUnlockType
    {
        Score,
        Time,
        Throws,
        Code,
        Combo,
        Custom
    }
}
