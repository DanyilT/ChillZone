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
            RestoreOrResetRun();
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

            // Horizontal (ground-plane) distance only — a throw from (almost) directly above the basket is trivial
            // regardless of the height drop, so vertical separation must not count toward the distance multiplier.
            var horizontal = hitPoint - _lastReleasePosition;
            horizontal.y = 0f;
            var distMult = ComputeDistanceMultiplier(horizontal.magnitude);
            var diffResult = ThrowDifficultyEvaluator.Evaluate(_lastThrowMode, _lastCurvature, scoringConfig);
            var basketMult = ResolveBasketMultiplier();
            var wallBounceMult = ResolveWallBounceMultiplier(ball);
            var totalMult = distMult * diffResult.Multiplier * basketMult * wallBounceMult;
            var finalPoints = Mathf.RoundToInt(basePoints * totalMult);

            if (totalMult > _peakMultiplier) _peakMultiplier = totalMult;

            ball.MarkResolved();
            ball.OnBasketHit(hitPoint);

            CurrentScore += finalPoints;

            // Persist the in-progress run immediately so it survives an app exit and the record / score-based
            // unlock criteria reflect it live — not only after the run commits on a miss.
            PlayerProfileManager.Instance?.UpdateActiveRun(ResolveContextId(), CurrentScore, ThrowCount, ElapsedSeconds);

            EventBus<BallScoredEvent>.Raise(new BallScoredEvent
            {
                FinalPoints = finalPoints,
                DistanceMultiplier = distMult,
                DifficultyMultiplier = diffResult.Multiplier,
                BasketMultiplier = basketMult,
                WallBounceMultiplier = wallBounceMult,
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
            PlayerProfileManager.Instance?.ClearActiveRun();

            EventBus<ScoreUpdatedEvent>.Raise(new ScoreUpdatedEvent
            {
                TotalScore = 0,
                ThrowCount = 0,
            });
        }

        #endregion

        #region internals

        // On scene load, resume a run that was still in progress when the app last closed (so the score isn't lost
        // and shows as the current score); otherwise start fresh.
        private void RestoreOrResetRun()
        {
            var run = PlayerProfileManager.Instance ? PlayerProfileManager.Instance.EnsureProfile().activeRun : null;
            if (run is { inProgress: true })
            {
                CurrentScore = run.score;
                ThrowCount = Mathf.Max(1, run.throws); // >0 so HasActiveRun is true → StartPlaySession keeps the score
                RunStartTime = Time.time - run.elapsedSeconds;
                _peakMultiplier = 1f;
            }
            else
            {
                ResetRun();
            }
        }

        // Score context id for the current throw mode (the value RegisterScore, the active run, and RunEnded use).
        private string ResolveContextId() => gameConfig ? gameConfig.ResolveScoreContextId(_lastThrowMode) : "basket-run";

        private void CommitRun()
        {
            PlayerProfileManager.Instance?.RegisterScore(ResolveContextId(), CurrentScore, ElapsedSeconds);

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

            // Below the baseline distance, ramp DOWN from 1× toward nearDistanceMultiplier (≈0) so a trivial
            // point-blank shot — e.g. a basket right under the spawn point — scores little to nothing.
            if (distance < scoringConfig.minMultiplierDistance)
                return Mathf.Lerp(scoringConfig.nearDistanceMultiplier, 1f,
                    Mathf.InverseLerp(0f, scoringConfig.minMultiplierDistance, distance));

            // Beyond the baseline, ramp UP from 1× to the max for longer shots.
            return Mathf.Lerp(1f, scoringConfig.maxDistanceMultiplier,
                Mathf.InverseLerp(scoringConfig.minMultiplierDistance, scoringConfig.maxMultiplierDistance, distance));
        }

        // Per-basket score multiplier from the currently selected BasketData (1× if none / unset).
        private static float ResolveBasketMultiplier()
        {
            var basket = ContentManager.Instance != null ? ContentManager.Instance.GetSelected<BasketData>(ContentTypes.Basket) : null;
            return basket != null && basket.scoreMultiplier > 0f ? basket.scoreMultiplier : 1f;
        }

        // Trick-shot bonus: a basket scored after the ball bounced off a virtual-environment wall (1× if it didn't
        // bounce or the config is unset). WallBounceCount is always 0 in AR, so this only ever applies in virtual mode.
        private float ResolveWallBounceMultiplier(BallBehaviour ball)
        {
            if (!scoringConfig || ball == null || ball.WallBounceCount <= 0) return 1f;
            return scoringConfig.wallBounceMultiplier > 0f ? scoringConfig.wallBounceMultiplier : 1f;
        }

        #endregion
    }
}
