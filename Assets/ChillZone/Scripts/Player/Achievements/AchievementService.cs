using ChillZone.Core;
using ChillZone.Core.Events;
using UnityEngine;

namespace ChillZone.Player.Achievements
{
    /// <summary>
    /// Event-driven achievement engine. Subscribes to gameplay events, tracks per-run stats
    /// (throws, combo, running score, time), and unlocks any catalog achievement whose condition
    /// is met (via <see cref="AchievementEvaluator"/> → <see cref="PlayerProfileManager"/>).
    /// </summary>
    public class AchievementService : MonoBehaviour
    {
        [SerializeField, Tooltip("Catalog of achievement definitions to evaluate. Wire the same AchievementCatalog asset used by PlayerProfileManager.")]
        private AchievementCatalog catalog;

        private int _runThrows;
        private int _runCombo;
        private int _runScore;

        #region lifecycle

        private void OnEnable()
        {
            EventBus<BallThrownEvent>.Subscribe(OnBallThrown);
            EventBus<BallScoredEvent>.Subscribe(OnBallScored);
            EventBus<ScoreUpdatedEvent>.Subscribe(OnScoreUpdated);
            EventBus<RunEndedEvent>.Subscribe(OnRunEnded);
        }

        private void OnDisable()
        {
            EventBus<BallThrownEvent>.Unsubscribe(OnBallThrown);
            EventBus<BallScoredEvent>.Unsubscribe(OnBallScored);
            EventBus<ScoreUpdatedEvent>.Unsubscribe(OnScoreUpdated);
            EventBus<RunEndedEvent>.Unsubscribe(OnRunEnded);
        }

        #endregion

        #region event handlers

        private void OnBallThrown(BallThrownEvent evt) => _runThrows++;

        private void OnScoreUpdated(ScoreUpdatedEvent evt) => _runScore = evt.TotalScore;

        private void OnBallScored(BallScoredEvent evt)
        {
            _runCombo++;
            Evaluate(new RunStats(_runScore, 0f, _runThrows, _runCombo, isRunEnd: false));
        }

        private void OnRunEnded(RunEndedEvent evt)
        {
            Evaluate(new RunStats(evt.FinalScore, evt.ElapsedSeconds, evt.ThrowCount, _runCombo, isRunEnd: true));
            _runThrows = 0;
            _runCombo = 0;
            _runScore = 0;
        }

        #endregion

        private void Evaluate(in RunStats stats)
        {
            if (!catalog) return;
            var manager = PlayerProfileManager.Instance;
            if (!manager) return;

            var progress = manager.EnsureProfile();
            foreach (var def in catalog.achievements)
            {
                if (def == null || progress.HasAchievement(def.achievementId)) continue;
                if (AchievementEvaluator.IsMet(def, stats, progress))
                    manager.UnlockAchievement(def.achievementId); // records + applies rewards + saves
            }
        }
    }
}
