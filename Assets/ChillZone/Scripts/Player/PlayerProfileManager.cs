using System;
using System.IO;
using System.Linq;
using ChillZone.Content;
using ChillZone.Player.Achievements;
using UnityEngine;

namespace ChillZone.Player
{
    public class PlayerProfileManager : MonoBehaviour
    {
        [SerializeField, Tooltip("File name for saving the player profile data. Will be stored in Application.persistentDataPath.")]
        private string saveFileName = "player_profile.json";
        [SerializeField, Tooltip("If true, the player profile will be loaded automatically on Awake.")]
        private bool loadOnAwake = true;
        [SerializeField, Tooltip("Reference to the achievement catalog ScriptableObject.")]
        private AchievementCatalog achievementCatalog;

        public static PlayerProfileManager Instance { get; private set; }
        private PlayerProfileData Profile { get; set; }

        private string SavePath => Path.Combine(Application.persistentDataPath, saveFileName);

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (loadOnAwake) Load();
        }

        private void OnApplicationPause(bool pause) { if (pause) Save(); }

        private void OnApplicationQuit() => Save();

        private void Load()
        {
            Profile = LoadFromDisk() ?? CreateDefaultProfile();
            Profile.Normalize();
            Save();
            // ContentManager reads the selected id per content type from the profile (via EnsureProfile),
            // so the Player layer no longer pushes selection down to the content managers.
        }

        private void Save()
        {
            if (Profile == null) return;

            Profile.Normalize();
            File.WriteAllText(SavePath, JsonUtility.ToJson(Profile, true));
        }

        #region public api

        public PlayerProfileData EnsureProfile()
        {
            if (Profile == null) Load();
            return Profile;
        }

        public void ResetProfile()
        {
            Profile = CreateDefaultProfile();
            Save();
        }

        public void RegisterScore(string contextId, int score, float timeSeconds)
        {
            var profile = EnsureProfile();
            profile.RegisterScore(contextId, score, timeSeconds);
            Save();
            // Score/time/throw/combo achievements are evaluated by AchievementService via gameplay events.
        }

        /// <summary>Persist the in-progress run (score/throws/time) so it survives an app exit and feeds the live BestScore (score-based unlocks). Committed to bestScores separately on a miss.</summary>
        public void UpdateActiveRun(string contextId, int score, int throws, float timeSeconds)
        {
            var profile = EnsureProfile();
            profile.UpdateActiveRun(contextId, score, throws, timeSeconds);
            Save();
        }

        /// <summary>Clear the in-progress run (on run end / reset).</summary>
        public void ClearActiveRun()
        {
            var profile = EnsureProfile();
            profile.ClearActiveRun();
            Save();
        }

        public void UnlockBall(string ballId)
        {
            var changed = EnsureProfile().UnlockBall(ballId);
            if (changed) Save();
        }

        public void UnlockBasket(string basketId)
        {
            var changed = EnsureProfile().UnlockBasket(basketId);
            if (changed) Save();
        }

        public bool UnlockAchievement(string achievementId)
        {
            var profile = EnsureProfile();
            var changed = profile.UnlockAchievement(achievementId);
            if (changed) ApplyAchievementRewards(profile, achievementId);

            if (changed) Save();
            return changed;
        }

        /// <summary>Redeem a code. Returns what it unlocked (for a confirmation dialog); rejected if empty or already redeemed.</summary>
        public CodeRedeemResult RegisterCode(string code)
        {
            var profile = EnsureProfile();
            if (!profile.RegisterCode(code)) return CodeRedeemResult.Rejected;

            var unlocked = EvaluateCodeAchievements(profile, code);
            Save();
            return CodeRedeemResult.Success(unlocked);
        }

        /// <summary>Persist the selected content id for a type (generic; used by ContentManager). SelectBall/SelectBasket delegate here.</summary>
        public void SelectContent(ContentTypes contentType, string contentId)
        {
            var profile = EnsureProfile();
            profile.SetSelectedContentId(contentType, contentId);
            profile.updatedAtUtc = DateTime.UtcNow.ToString("o");
            Save();
        }

        public void SelectBall(string ballId) => SelectContent(ContentTypes.Ball, ballId);
        public void SelectBasket(string basketId) => SelectContent(ContentTypes.Basket, basketId);

        public bool HasBallUnlocked(string ballId) => EnsureProfile().HasBallUnlocked(ballId);
        public bool HasBasketUnlocked(string basketId) => EnsureProfile().HasBasketUnlocked(basketId);
        public bool HasAchievement(string achievementId) => EnsureProfile().HasAchievement(achievementId);
        public bool HasEnteredCode(string code) => EnsureProfile().HasEnteredCode(code);

        #endregion

        #region helpers (private)

        private PlayerProfileData LoadFromDisk()
        {
            if (!File.Exists(SavePath)) return null;

            var json = File.ReadAllText(SavePath);
            var profile = JsonUtility.FromJson<PlayerProfileData>(json);
            profile?.Normalize();
            return profile;
        }

        private static PlayerProfileData CreateDefaultProfile()
        {
            var profile = new PlayerProfileData();
            profile.Normalize();
            return profile;
        }

        // Unlocks any Code-type achievements whose relatedCode matches, applying their rewards. Returns the
        // first one newly unlocked (for the confirmation dialog), or null if the code maps to no achievement.
        private AchievementDefinition EvaluateCodeAchievements(PlayerProfileData profile, string code)
        {
            if (!achievementCatalog) return null;

            AchievementDefinition firstUnlocked = null;
            foreach (var achievement in achievementCatalog.achievements
                         .Where(achievement => achievement != null && !profile.HasAchievement(achievement.achievementId))
                         .Where(achievement => achievement.unlockType == AchievementUnlockType.Code && string.Equals(achievement.relatedCode, code, StringComparison.OrdinalIgnoreCase)))
            {
                profile.UnlockAchievement(achievement.achievementId);
                ApplyAchievementRewards(profile, achievement.achievementId);
                firstUnlocked ??= achievement;
            }

            return firstUnlocked;
        }

        private void ApplyAchievementRewards(PlayerProfileData profile, string achievementId)
        {
            if (!achievementCatalog) return;

            var achievement = achievementCatalog.GetById(achievementId);
            if (achievement == null) return;

            if (!string.IsNullOrWhiteSpace(achievement.relatedBallId))   profile.UnlockBall(achievement.relatedBallId);
            if (!string.IsNullOrWhiteSpace(achievement.relatedBasketId)) profile.UnlockBasket(achievement.relatedBasketId);
            if (achievement.currencyReward > 0)                          profile.softCurrency += achievement.currencyReward;
        }

        #endregion
    }
}
