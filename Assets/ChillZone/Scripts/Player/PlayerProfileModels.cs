using System;
using System.Collections.Generic;
using System.Linq;
using ChillZone.Content;

namespace ChillZone.Player
{
    [Serializable]
    public class PlayerProfileData : IPlayerProgress
    {
        private const int CurrentVersion = 1;
        private const int DefaultBestScoreSavedLimit = 20;

        public int version = CurrentVersion;
        public string profileId;
        public string createdAtUtc;
        public string updatedAtUtc;
        public string lastSavedAtUtc;
        public string selectedBallId;
        public string selectedBasketId;
        public int softCurrency;
        public List<PlayerBestScoreEntry> bestScores = new();
        public List<PlayerAchievementEntry> achievements = new();
        public List<PlayerCodeEntry> enteredCodes = new();
        public List<PlayerContentUnlockEntry> unlockedContent = new();
        public List<PlayerActionEntry> actions = new();
        public PlayerActiveRun activeRun = new();

        #region IPlayerProgress

        /// <summary>Highest score — the best COMMITTED run OR the in-progress run, whichever is higher, so the record
        /// (and score-based unlock criteria) reflect a live run immediately instead of only after it commits on a miss.</summary>
        public int BestScore => Math.Max(
            bestScores is { Count: > 0 } ? bestScores[0].score : 0,
            activeRun is { inProgress: true } ? activeRun.score : 0);
        public int SoftCurrency => softCurrency;

        #endregion

        public void Normalize()
        {
            if (string.IsNullOrWhiteSpace(profileId))
                profileId = Guid.NewGuid().ToString("N");

            if (string.IsNullOrWhiteSpace(createdAtUtc))
                createdAtUtc = DateTime.UtcNow.ToString("o");

            if (string.IsNullOrWhiteSpace(updatedAtUtc))
                updatedAtUtc = createdAtUtc;

            bestScores ??= new List<PlayerBestScoreEntry>();
            achievements ??= new List<PlayerAchievementEntry>();
            enteredCodes ??= new List<PlayerCodeEntry>();
            unlockedContent ??= new List<PlayerContentUnlockEntry>();
            actions ??= new List<PlayerActionEntry>();
            activeRun ??= new PlayerActiveRun();

            version = CurrentVersion;
        }

        // Adds an unlock entry if absent. No action is logged
        private bool AddContentUnlock(ContentTypes contentType, string contentId, string unlockedAtUtc)
        {
            if (string.IsNullOrWhiteSpace(contentId) || HasContentUnlocked(contentType, contentId)) return false;
            unlockedContent.Add(new PlayerContentUnlockEntry
            {
                contentType = contentType,
                contentId = contentId.Trim(),
                unlockedAtUtc = string.IsNullOrWhiteSpace(unlockedAtUtc) ? DateTime.UtcNow.ToString("o") : unlockedAtUtc
            });
            return true;
        }

        #region content unlocks (generic)

        public bool HasContentUnlocked(ContentTypes contentType, string contentId) => !string.IsNullOrWhiteSpace(contentId) && unlockedContent.Any(e => e.contentType == contentType && string.Equals(e.contentId, contentId, StringComparison.OrdinalIgnoreCase));

        public bool UnlockContent(ContentTypes contentType, string contentId)
        {
            if (!AddContentUnlock(contentType, contentId, DateTime.UtcNow.ToString("o"))) return false;
            RecordAction(ProfileActions.ContentUnlock, contentId, contentType.ToString());
            return true;
        }

        // Convenience wrappers so call sites read naturally; both delegate to the generic store.
        public bool HasBallUnlocked(string ballId) => HasContentUnlocked(ContentTypes.Ball, ballId);
        public bool UnlockBall(string ballId) => UnlockContent(ContentTypes.Ball, ballId);
        public bool HasBasketUnlocked(string basketId) => HasContentUnlocked(ContentTypes.Basket, basketId);
        public bool UnlockBasket(string basketId) => UnlockContent(ContentTypes.Basket, basketId);

        #endregion

        #region content selection (generic)

        /// <summary>Selected content id for a type, or null. Storage stays the named fields (selectedBallId/selectedBasketId) for save back-compat.</summary>
        public string GetSelectedContentId(ContentTypes contentType) => contentType switch
        {
            ContentTypes.Ball => selectedBallId,
            ContentTypes.Basket => selectedBasketId,
            _ => null
        };

        public void SetSelectedContentId(ContentTypes contentType, string contentId)
        {
            var value = string.IsNullOrWhiteSpace(contentId) ? null : contentId.Trim();
            switch (contentType)
            {
                case ContentTypes.Ball: selectedBallId = value; break;
                case ContentTypes.Basket: selectedBasketId = value; break;
            }
        }

        #endregion

        public bool HasAchievement(string achievementId) =>
            !string.IsNullOrWhiteSpace(achievementId) && achievements.Any(t => string.Equals(t.achievementId, achievementId, StringComparison.OrdinalIgnoreCase));

        public bool UnlockAchievement(string achievementId)
        {
            if (HasAchievement(achievementId)) return false;

            achievements.Add(new PlayerAchievementEntry
            {
                achievementId = achievementId,
                unlockedAtUtc = DateTime.UtcNow.ToString("o")
            });

            RecordAction(ProfileActions.AchievementUnlock, achievementId);
            return true;
        }

        public bool HasEnteredCode(string code) =>
            !string.IsNullOrWhiteSpace(code) && enteredCodes.Any(t => string.Equals(t.code, code, StringComparison.OrdinalIgnoreCase));

        public bool RegisterCode(string code)
        {
            if (HasEnteredCode(code)) return false;

            enteredCodes.Add(new PlayerCodeEntry
            {
                code = code.Trim(),
                enteredAtUtc = DateTime.UtcNow.ToString("o")
            });

            RecordAction(ProfileActions.CodeEntered, code);
            return true;
        }

        public void RegisterScore(string contextId, int score, float timeSeconds)
        {
            bestScores ??= new List<PlayerBestScoreEntry>();
            bestScores.Add(new PlayerBestScoreEntry
            {
                contextId = contextId,
                score = score,
                timeSeconds = timeSeconds,
                recordedAtUtc = DateTime.UtcNow.ToString("o")
            });

            bestScores.Sort(PlayerBestScoreEntryComparer.Instance);
            // only `DefaultBestScoreSavedLimit` best scores are saved to prevent profile bloat, but all scores are still recorded in actions for analytics purposes
            if (bestScores.Count > DefaultBestScoreSavedLimit)
                bestScores.RemoveRange(DefaultBestScoreSavedLimit, bestScores.Count - DefaultBestScoreSavedLimit);

            RecordAction(ProfileActions.ScoreRecorded, contextId, score.ToString(), timeSeconds.ToString("0.###"));
        }

        #region active run (in-progress, cross-session)

        /// <summary>Persist the in-progress run so it survives an app exit (restored as the current score on reopen)
        /// and feeds the live BestScore. It's committed to bestScores separately on a miss.</summary>
        public void UpdateActiveRun(string contextId, int score, int throws, float elapsedSeconds)
        {
            activeRun ??= new PlayerActiveRun();
            activeRun.inProgress = true;
            activeRun.contextId = contextId;
            activeRun.score = score;
            activeRun.throws = throws;
            activeRun.elapsedSeconds = elapsedSeconds;
            activeRun.updatedAtUtc = DateTime.UtcNow.ToString("o");
            updatedAtUtc = activeRun.updatedAtUtc;
        }

        /// <summary>Clear the in-progress run (on run end / reset). BestScore then reflects only committed runs.</summary>
        public void ClearActiveRun()
        {
            activeRun ??= new PlayerActiveRun();
            activeRun.inProgress = false;
            activeRun.score = 0;
            activeRun.throws = 0;
            activeRun.elapsedSeconds = 0f;
        }

        #endregion

        public void RecordAction(string actionType, string subjectId = null, string value = null, string value2 = null)
        {
            actions ??= new List<PlayerActionEntry>();
            actions.Add(new PlayerActionEntry
            {
                actionType = actionType,
                subjectId = subjectId,
                value = value,
                value2 = value2,
                occurredAtUtc = DateTime.UtcNow.ToString("o")
            });

            updatedAtUtc = DateTime.UtcNow.ToString("o");
        }
    }

    [Serializable]
    public class PlayerBestScoreEntry
    {
        public string contextId;
        public int score;
        public float timeSeconds;
        public string recordedAtUtc;
    }

    internal sealed class PlayerBestScoreEntryComparer : IComparer<PlayerBestScoreEntry>
    {
        public static readonly PlayerBestScoreEntryComparer Instance = new();

        public int Compare(PlayerBestScoreEntry left, PlayerBestScoreEntry right)
        {
            if (ReferenceEquals(left, right)) return 0;
            if (left == null) return 1;
            if (right == null) return -1;

            var scoreCompare = right.score.CompareTo(left.score);
            if (scoreCompare != 0) return scoreCompare;

            var timeCompare = left.timeSeconds.CompareTo(right.timeSeconds);
            if (timeCompare != 0) return timeCompare;

            return string.CompareOrdinal(right.recordedAtUtc, left.recordedAtUtc);
        }
    }

    [Serializable]
    public class PlayerActiveRun
    {
        public bool inProgress;
        public string contextId;
        public int score;
        public int throws;
        public float elapsedSeconds;
        public string updatedAtUtc;
    }

    [Serializable]
    public class PlayerAchievementEntry
    {
        public string achievementId;
        public string unlockedAtUtc;
    }

    [Serializable]
    public class PlayerCodeEntry
    {
        public string code;
        public string enteredAtUtc;
    }

    [Serializable]
    public class PlayerBallUnlockEntry
    {
        public string ballId;
        public string unlockedAtUtc;
    }

    [Serializable]
    public class PlayerContentUnlockEntry
    {
        public ContentTypes contentType;
        public string contentId;
        public string unlockedAtUtc;
    }

    [Serializable]
    public class PlayerActionEntry
    {
        public string actionType;
        public string subjectId;
        public string value;
        public string value2;
        public string occurredAtUtc;
    }
}
