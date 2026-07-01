using ChillZone.Config;
using ChillZone.Core;
using ChillZone.Core.Events;
using ChillZone.Player;
using UnityEngine;
using ChillZone.Ball;
using ChillZone.Basket;
using ChillZone.Content;

namespace ChillZone.Gameplay
{
    /// <summary>
    /// Scene-local singleton. Owns all scoring logic: run state, multiplier
    /// calculation, committing runs to the player profile, and raising events
    /// for the HUD and other listeners. Has no UI code and no AR references.
    /// </summary>
    public class ScoringSystem : MonoBehaviour
    {
        public static ScoringSystem Instance { get; private set; }

        [SerializeField] private ScoringConfig scoringConfig;
        [SerializeField] private GameConfig gameConfig;

        #region run state

        private int  CurrentScore  { get; set; }

        /// <summary>Score accumulated in the current (uncommitted) run. The best-score display maxes this
        /// with the saved best so a fresh high shows immediately, before the run ends and commits.</summary>
        public int CurrentRunScore => CurrentScore;

        private int  ThrowCount { get; set; }
        private float RunStartTime { get; set; }
        private float ElapsedSeconds => Time.time - RunStartTime;

        /// <summary>True while a run is in progress (the player has thrown at least once and not yet missed).
        /// False for a fresh or just-finished run — lets the game flow keep the score when re-entering play
        /// mid-run, or start fresh otherwise.</summary>
        public bool HasActiveRun => ThrowCount > 0;

        // Last throw data, cached until a score/miss resolves it.
        private ThrowMode _lastThrowMode;
        private float _lastCurvature;
        private Vector3 _lastReleasePosition;
        private float _peakMultiplier;

        #endregion

        #region lifecycle

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            ResetRun();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void OnEnable() => EventBus<BallThrownEvent>.Subscribe(OnBallThrown);
        private void OnDisable() => EventBus<BallThrownEvent>.Unsubscribe(OnBallThrown);

        #endregion

        #region event listeners

        private void OnBallThrown(BallThrownEvent evt)
        {
            ThrowCount++;
            _lastThrowMode = evt.Mode;
            _lastCurvature = evt.TotalCurvature;
            _lastReleasePosition = evt.ReleasePosition;
        }

        #endregion

        #region public api

        /// <summary>Called by BallBehaviour when the live ball enters a "ScoreZone" trigger.</summary>
        public void HandleScore(BallBehaviour ball, Vector3 hitPoint)
        {
            if (!ball || ball.HasResolved) return;

            var basePoints = scoringConfig.basePoints;

            var distance = Vector3.Distance(_lastReleasePosition, hitPoint);
            var distMult = ComputeDistanceMultiplier(distance);
            var diffResult = ThrowDifficultyEvaluator.Evaluate(_lastThrowMode, _lastCurvature, scoringConfig);
            var basketMult = ResolveBasketMultiplier();
            var totalMult = distMult * diffResult.Multiplier * basketMult;
            var finalPoints = Mathf.RoundToInt(basePoints * totalMult);

            if (totalMult > _peakMultiplier) _peakMultiplier = totalMult;

            ball.MarkResolved();
            ball.OnBasketHit(hitPoint);

            CurrentScore += finalPoints;

            EventBus<BallScoredEvent>.Raise(new BallScoredEvent
            {
                FinalPoints = finalPoints,
                DistanceMultiplier = distMult,
                DifficultyMultiplier = diffResult.Multiplier,
                BasketMultiplier = basketMult,
                DifficultyLabel = diffResult.Label,
                HitPoint = hitPoint,
            });

            EventBus<ScoreUpdatedEvent>.Raise(new ScoreUpdatedEvent
            {
                TotalScore = CurrentScore,
                ThrowCount = ThrowCount,
            });

            // Scored ball goes back to the pool (deactivated, reusable); then bring out the next one.
            BallSpawnManager.Instance?.RecycleBallToPool(ball.gameObject);
            BallSpawnManager.Instance?.SpawnSelectedBallAtSpawnPoint();
        }

        /// <summary>Called by BallBehaviour when the ball leaves bounds or stops without scoring.</summary>
        public void HandleMiss(BallBehaviour ball, Vector3 hitPoint)
        {
            if (!ball || ball.HasResolved) return;

            ball.MarkResolved();
            ball.OnMiss(hitPoint);

            CommitRun();

            EventBus<BallMissedEvent>.Raise(new BallMissedEvent { HitPoint = hitPoint });

            ResetRun();

            // Leave the missed ball where it landed (it stays interactable — you can hit it with another
            // ball — until the pool recycles it); just bring out the next ball.
            BallSpawnManager.Instance?.SpawnSelectedBallAtSpawnPoint();
        }

        public void ResetRun()
        {
            CurrentScore = 0;
            ThrowCount = 0;
            RunStartTime = Time.time;
            _peakMultiplier = 1f;

            EventBus<ScoreUpdatedEvent>.Raise(new ScoreUpdatedEvent
            {
                TotalScore = 0,
                ThrowCount = 0,
            });
        }

        #endregion

        #region internals

        private void CommitRun()
        {
            var contextId = gameConfig ? gameConfig.ResolveScoreContextId(_lastThrowMode) : "basket-run";
            PlayerProfileManager.Instance?.RegisterScore(contextId, CurrentScore, ElapsedSeconds);

            EventBus<RunEndedEvent>.Raise(new RunEndedEvent
            {
                FinalScore = CurrentScore,
                ThrowCount = ThrowCount,
                ElapsedSeconds = ElapsedSeconds,
            });
        }

        private float ComputeDistanceMultiplier(float distance)
        {
            if (!scoringConfig) return 1f;
            return Mathf.Lerp(1f, scoringConfig.maxDistanceMultiplier,
                Mathf.InverseLerp(scoringConfig.minMultiplierDistance, scoringConfig.maxMultiplierDistance, distance));
        }

        // Per-basket score multiplier from the currently selected BasketData (1× if none / unset).
        private static float ResolveBasketMultiplier()
        {
            var basket = ContentManager.Instance != null ? ContentManager.Instance.GetSelected<BasketData>(ContentTypes.Basket) : null;
            return basket != null && basket.scoreMultiplier > 0f ? basket.scoreMultiplier : 1f;
        }

        #endregion
    }
}
