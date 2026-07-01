using ChillZone.Content;
using UnityEngine;

namespace ChillZone.Player.Achievements
{
    /// <summary>
    /// Pure check of whether an achievement's condition is currently met. Centralizes the
    /// per-type logic in one place. Unknown / not-yet-supported types simply never auto-unlock
    /// (no exceptions thrown), so new <see cref="AchievementUnlockType"/> values are safe to add.
    /// </summary>
    public static class AchievementEvaluator
    {
        public static bool IsMet(AchievementDefinition def, in RunStats stats, IPlayerProgress progress)
        {
            if (def == null) return false;

            switch (def.unlockType)
            {
                case AchievementUnlockType.Score:
                    var best = progress?.BestScore ?? 0;
                    return Mathf.Max(stats.Score, best) >= def.targetValue;

                case AchievementUnlockType.Time:
                    // Completing a run (scored at least once) within the target time.
                    return stats is { IsRunEnd: true, Score: > 0 } && stats.TimeSeconds <= def.targetValue;

                case AchievementUnlockType.Throws:
                    return stats.Throws >= def.targetValue;

                case AchievementUnlockType.Combo:
                    return stats.Combo >= def.targetValue;

                case AchievementUnlockType.Code:    // unlocked when the matching code is entered
                case AchievementUnlockType.Custom:  // unlocked explicitly via PlayerProfileManager.UnlockAchievement
                    return false;

                default:
                    return false; // Unknown/new type: never auto-unlock (safe — no throw).
            }
        }
    }
}
