using System.Collections.Generic;
using System.Linq;
using ChillZone.Config;
using ChillZone.Content;
using ChillZone.Core;
using ChillZone.Core.Events;
using ChillZone.Game;
using ChillZone.Gameplay;
using UnityEngine;

namespace ChillZone.Ball
{
    /// <summary>
    /// Scene-local singleton that owns the ball's spawn lifecycle: an object pool of ball instances,
    /// the currently held/active ball, and the release/recycle plumbing. Reads which ball to spawn
    /// from <see cref="ContentManager"/> (selection/persistence, keyed by content type) — this class only handles instancing.
    /// Not DontDestroyOnLoad — a fresh instance is created each time the Game scene loads, ensuring
    /// BallSpawnPoint refs are always valid.
    /// </summary>
    public class BallSpawnManager : MonoBehaviour
    {
        public static BallSpawnManager Instance { get; private set; }

        [SerializeField] private ThrowConfig throwConfig;
        [SerializeField] private GameConfig  gameConfig;
        [SerializeField, Tooltip("Real-world size (m, longest axis) applied to a spawned ball that has no RealWorldScaler.")]
        private float ballRealWorldSize = 0.24f;
        [SerializeField, Tooltip("Maximum ball objects kept alive at once. Balls are pooled and recycled (oldest first) instead of destroyed — Destroy causes a frame hitch. Missed balls linger in the world until recycled.")]
        private int maxBalls = 10;

        public float MaxWorldAxisMagnitude => gameConfig != null ? gameConfig.maxWorldAxisMagnitude : 1000f;

        private BallSpawnPoint _spawnPoint;

        // Object pool: every ball instance this manager has created, oldest first. Includes the held ball,
        // balls in flight, balls resting after a miss, and inactive (recycled/scored) balls. Capped at
        // maxBalls — acquiring past the cap recycles the oldest instead of instantiating.
        private readonly List<GameObject> _pool = new();
        private BallData _pooledBallData; // the BallData the pooled instances were created for

        public GameObject ActiveBall { get; private set; }

        #region lifecycle

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            _spawnPoint = FindObjectOfType<BallSpawnPoint>();
            ThrowSettingsStore.ApplyTo(throwConfig);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void OnEnable()  => EventBus<ResetBallRequestedEvent>.Subscribe(OnResetBallRequested);
        private void OnDisable() => EventBus<ResetBallRequestedEvent>.Unsubscribe(OnResetBallRequested);
        private void OnResetBallRequested(ResetBallRequestedEvent evt) => ResetActiveBall();

        #endregion

        #region reset

        /// <summary>
        /// Reset/replace the active ball. The score is reset ONLY when the previous ball wasn't
        /// resting at the spawn point (i.e. it had already been thrown, or there was none) — swapping
        /// a still-held ball just changes the model and keeps the run going.
        /// </summary>
        public void ResetActiveBall()
        {
            if (!IsActiveBallAtSpawn()) ScoringSystem.Instance?.ResetRun();
            RespawnSelectedBallAtSpawnPoint();
        }

        private bool IsActiveBallAtSpawn()
        {
            if (!ActiveBall) return false; // thrown (ActiveBall cleared on release) or none
            var behaviour = ActiveBall.GetComponent<BallBehaviour>();
            return behaviour && behaviour.IsHeld;
        }

        #endregion

        #region spawning

        public GameObject SpawnBall(Vector3 position, Transform parent = null)
        {
            var selected = ResolveSelectedBall();
            if (selected == null || selected.prefab == null)
            {
                Debug.LogError("BallSpawnManager: no selected ball prefab.", this);
                return null;
            }

            var ball = Instantiate(selected.prefab, position, selected.prefab.transform.rotation, parent);
            var behaviour = ball.GetComponent<BallBehaviour>() ?? ball.AddComponent<BallBehaviour>();
            behaviour.Init(selected);
            EnsureThrowController(ball);
            EnsureRealWorldScaler(ball, selected);
            ActiveBall = ball;
            return ball;
        }

        public GameObject SpawnSelectedBallAtSpawnPoint()
        {
            EnsureSpawnPoint();
            if (!_spawnPoint)
            {
                Debug.LogError("BallSpawnManager: no BallSpawnPoint found.", this);
                return null;
            }

            var selected = ResolveSelectedBall();
            if (!selected || !selected.prefab)
            {
                Debug.LogError("BallSpawnManager: no selected ball prefab.", this);
                return null;
            }

            // Pooled instances belong to one prefab; if the selected ball changed, drop them.
            if (_pooledBallData != selected) ClearPool();
            _pooledBallData = selected;

            // Recycle a still-held ball so this spawn replaces it (it then becomes reusable).
            if (ActiveBall) RecycleBallToPool(ActiveBall);

            var ball = AcquireBall(selected, out bool isNew);
            if (!ball) return null;

            if (ball.transform.parent != _spawnPoint.SpawnParent)
                ball.transform.SetParent(_spawnPoint.SpawnParent, false);
            if (!ball.activeSelf) ball.SetActive(true);
            ball.transform.position      = _spawnPoint.Position; // camera-relative spawn pos (also syncs the spawn transform)
            ball.transform.localRotation = selected.prefab.transform.localRotation; // keep the prefab's authored orientation

            var behaviour = ball.GetComponent<BallBehaviour>() ?? ball.AddComponent<BallBehaviour>();
            if (isNew) behaviour.Init(selected); // one-time setup (physics, trail); reused balls keep theirs
            behaviour.ResetAtSpawnPoint();
            EnsureThrowController(ball);
            EnsureRealWorldScaler(ball, selected);

            ActiveBall = ball;
            return ball;
        }

        // Pick a ball to spawn: reuse an inactive pooled one, else instantiate while under the cap, else
        // recycle the oldest in-world ball (so the pool never grows past maxBalls).
        private GameObject AcquireBall(BallData selected, out bool isNew)
        {
            isNew = false;
            _pool.RemoveAll(b => !b);

            foreach (var pooled in _pool.Where(pooled => !pooled.activeSelf)) return pooled;

            if (_pool.Count < Mathf.Max(1, maxBalls))
            {
                var fresh = Instantiate(selected.prefab, _spawnPoint.SpawnParent);
                _pool.Add(fresh);
                isNew = true;
                return fresh;
            }

            var oldest = _pool[0];
            _pool.RemoveAt(0);
            _pool.Add(oldest); // moved to the end — it's now the newest
            return oldest;
        }

        public void ReleaseActiveBall(Vector3 launchVelocity, Vector3 angularVelocity = default)
        {
            if (ActiveBall == null) return;
            EnsureSpawnPoint();

            var behaviour = ActiveBall.GetComponent<BallBehaviour>();
            var rb = ActiveBall.GetComponent<Rigidbody>();

            if (_spawnPoint != null)
                ActiveBall.transform.SetParent(_spawnPoint.PoolParent, true);

            behaviour?.Release();

            if (rb != null)
            {
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.AddForce(launchVelocity, ForceMode.VelocityChange);
                if (angularVelocity != Vector3.zero)
                    rb.AddTorque(angularVelocity, ForceMode.VelocityChange);
            }

            ActiveBall = null;
        }

        // SpawnSelectedBallAtSpawnPoint already recycles the held ball into the pool, so this is just an alias
        // kept for existing callers (ball picker / reset-ball button).
        public GameObject RespawnSelectedBallAtSpawnPoint() => SpawnSelectedBallAtSpawnPoint();

        /// <summary>Return a ball to the pool (deactivated, parked under the pool root) so it can be reused.
        /// Used for scored balls; missed balls are left in the world until the pool recycles them.</summary>
        public void RecycleBallToPool(GameObject ball)
        {
            if (!ball) return;
            if (!_pool.Contains(ball)) _pool.Add(ball);
            EnsureSpawnPoint();
            if (_spawnPoint) ball.transform.SetParent(_spawnPoint.PoolParent, true);
            ball.SetActive(false);
            if (ball == ActiveBall) ActiveBall = null;
        }

        #endregion

        #region internals

        // The ball to spawn is owned by ContentManager (selection/persistence), keyed by content type.
        private static BallData ResolveSelectedBall() =>
            ContentManager.Instance ? ContentManager.Instance.GetSelected<BallData>(ContentTypes.Ball) : null;

        // Destroy every pooled instance (only when the selected ball changes — pooled balls are one prefab).
        private void ClearPool()
        {
            foreach (var ball in _pool.Where(ball => ball != null)) Destroy(ball);
            _pool.Clear();
            ActiveBall = null;
        }

        private void EnsureSpawnPoint()
        {
            if (!_spawnPoint) _spawnPoint = FindObjectOfType<BallSpawnPoint>();
        }

        private void EnsureThrowController(GameObject ball)
        {
            var controller = ball.GetComponent<BallController>() ?? ball.AddComponent<BallController>();
            // Assign config when the component is created dynamically at runtime.
            // (Prefab-based balls should already have the config wired in the prefab.)
            controller.SetConfig(throwConfig);
        }

        // Sizes the ball to its own realWorldSizeMeters override (when set) or the shared default. The scale
        // is RE-APPLIED on every spawn (not just the first): RealWorldScaler.SetTargetSize resets localScale
        // to 1 and recomputes from the mesh bounds, so the size is deterministic and immune to scale drift
        // from recycle/reuse reparenting (worldPositionStays toggling), which otherwise shrank reused balls.
        private void EnsureRealWorldScaler(GameObject ball, BallData selected)
        {
            var scaler = ball.GetComponent<RealWorldScaler>() ?? ball.AddComponent<RealWorldScaler>();
            var size = selected && selected.realWorldSizeMeters > 0f ? selected.realWorldSizeMeters : ballRealWorldSize;
            scaler.SetTargetSize(size);
        }

        #endregion
    }
}
