using UnityEngine;
using UnityEngine.EventSystems;
using ChillZone.Basket;
using ChillZone.Core;
using ChillZone.Game;
using ChillZone.Gameplay;

namespace ChillZone.Ball
{
    [RequireComponent(typeof(Rigidbody), typeof(SphereCollider))]
    public class BallBehaviour : MonoBehaviour
    {
        private static float MaxWorldAxisMagnitude => BallSpawnManager.Instance != null ? BallSpawnManager.Instance.MaxWorldAxisMagnitude : 1000f;
        private BallData _data;
        private Rigidbody _rb;
        private Collider _collider;
        private PhysicMaterial _physicMaterial;
        private TrailRenderer _trail;
        private bool _isReleased;
        private float _releasedAtTime;
        private Vector3 _releasedPosition;
        private float _scoreZoneY;
        private bool _hasScoreZoneY;
        private bool _pendingCollisionMiss;
        private Vector3 _pendingMissPoint;
        private float _gravityScale = 1f;

        // How far (metres) the ball must fall below the hoop while descending before it counts as missing
        // the basket. The margin keeps a clean shot from being flagged as it drops through the score zone.
        private const float BelowBasketMargin = 0.15f;

        public bool HasResolved { get; private set; }

        public Vector3 ReleasedPosition => _releasedPosition;
        /// <summary>True while the ball is still resting at the spawn point (not yet thrown).</summary>
        public bool IsHeld => !_isReleased;
        /// <summary>How many virtual-environment walls this ball has bounced off since being thrown (always 0 in AR). A scored shot with a bounce earns the wall-bounce multiplier.</summary>
        public int WallBounceCount { get; private set; }

        #region init

        public void Init(BallData data)
        {
            _data = data;
            ApplyPhysics();
            ApplyPhysicsMaterial();
            BuildTrail();

            ResetAtSpawnPoint();
        }

        private void ApplyPhysics()
        {
            _rb = GetComponent<Rigidbody>() ?? gameObject.AddComponent<Rigidbody>();
            _rb.mass = _data.mass;
            _rb.drag = _data.drag;
            _gravityScale = _data.gravityScale;
        }

        private void ApplyPhysicsMaterial()
        {
            _collider = GetComponent<Collider>() ?? gameObject.AddComponent<SphereCollider>();
            _collider.material = new PhysicMaterial("BallMat")
            {
                bounciness = _data.bounciness,
                frictionCombine = PhysicMaterialCombine.Minimum,
                bounceCombine = PhysicMaterialCombine.Maximum
            };
        }

        // The trail is built in code from BallData.trail and only emits while the ball is flying.
        private void BuildTrail()
        {
            if (_data?.trail is not { enabled: true }) return;

            var trailObject = new GameObject("BallTrail");
            trailObject.transform.SetParent(transform, false);
            trailObject.transform.localPosition = Vector3.zero;

            _trail = trailObject.AddComponent<TrailRenderer>();
            _data.trail.ApplyTo(_trail, _data.uiAccentColor);
            _trail.emitting = false;
        }

        public void ResetAtSpawnPoint()
        {
            _isReleased  = false;
            HasResolved = false;
            _pendingCollisionMiss = false;
            WallBounceCount = 0;
            if (_trail)
            {
                _trail.emitting = false;
                _trail.Clear();
            }

            _rb ??= GetComponent<Rigidbody>();
            if (_rb)
            {
                _rb.velocity = Vector3.zero;
                _rb.angularVelocity = Vector3.zero;
                _rb.isKinematic = true;
                _rb.useGravity = false;
            }
        }

        public void Release()
        {
            _isReleased = true;
            _releasedAtTime = Time.time;
            _releasedPosition = transform.position;
            if (_trail != null) _trail.emitting = true;
            CacheScoreZoneHeight();
            _rb ??= GetComponent<Rigidbody>();
            if (_rb)
            {
                _rb.isKinematic = false;
                _rb.useGravity  = false;  // gravity is applied in FixedUpdate via BallData.gravityScale
            }
        }

        // Cache the hoop height at throw time so the ball can detect dropping below the basket. Re-read each
        // throw because the basket can be moved between shots.
        private void CacheScoreZoneHeight()
        {
            _hasScoreZoneY = false;
            var zone = GameObject.FindGameObjectWithTag(GameTags.ScoreZone);
            if (zone == null) return;
            _scoreZoneY = zone.transform.position.y;
            _hasScoreZoneY = true;
        }

        #endregion

        #region update

        private void Update()
        {
            // A collision miss is deferred one frame so that a score landing in the same physics step (the
            // ball passing through the ScoreZone trigger) takes priority over touching the solid basket body.
            if (_pendingCollisionMiss)
            {
                _pendingCollisionMiss = false;
                if (!HasResolved) { ResolveMiss(_pendingMissPoint); return; }
            }

            if (!_isReleased || HasResolved) return;

            var pos = transform.position;
            if (Mathf.Abs(pos.x) > MaxWorldAxisMagnitude ||
                Mathf.Abs(pos.y) > MaxWorldAxisMagnitude ||
                Mathf.Abs(pos.z) > MaxWorldAxisMagnitude)
            {
                ResolveMiss(pos);
                return;
            }

            // Fell below the basket while descending without scoring → it can no longer go in.
            if (_hasScoreZoneY && _rb && _rb.velocity.y < 0f && pos.y < _scoreZoneY - BelowBasketMargin)
            {
                ResolveMiss(pos);
                return;
            }

            if (Time.time - _releasedAtTime < 2f) return;
            if (_rb && !_rb.IsSleeping()) return;
            if (pos.y > _releasedPosition.y - 0.05f && Time.time - _releasedAtTime < 8f) return;

            ResolveMiss(pos);
        }

        // Custom gravity so BallData.gravityScale can make a ball fall faster/slower, hover (0) or rise like a
        // balloon (negative). Acceleration mode is mass-independent — matching real gravity, which mass can't change.
        // Runs while the ball is a live dynamic body (in flight and after it resolves, until it's pooled).
        private void FixedUpdate()
        {
            if (!_isReleased || !_rb || _rb.isKinematic) return;
            _rb.AddForce(Physics.gravity * _gravityScale, ForceMode.Acceleration);
        }

        #endregion

        #region scoring

        // The basket carries a thin trigger collider tagged "ScoreZone" at the hoop opening. A live ball
        // passing through it counts as a basket.
        private void OnTriggerEnter(Collider other)
        {
            if (!_isReleased || HasResolved) return;
            if (other.CompareTag(GameTags.ScoreZone))
                ScoringSystem.Instance?.HandleScore(this, transform.position);
        }

        // A miss is only a collision with a real surface (floor/wall) or another ball — NOT the basket
        // itself, whose solid body surrounds the score zone (so the ball must reach the basket to score).
        // Deferred to the next Update (see Update) so a simultaneous ScoreZone trigger still wins.
        private void OnCollisionEnter(Collision collision)
        {
            if (!_isReleased || HasResolved || _pendingCollisionMiss) return;

            // Touching the basket is never a miss.
            if (collision.collider.GetComponentInParent<BasketController>() != null) return;

            // Bouncing off a virtual-environment wall/ceiling is a FEATURE, not a miss: count it (a basket scored
            // after a bounce earns the wall-bounce multiplier) and let physics rebound the ball back into play.
            if (collision.collider.GetComponentInParent<BounceWall>() != null)
            {
                WallBounceCount++;
                return;
            }

            // Hitting another ball misses this one and plays the miss effects on the other at the same time.
            // A live other ball misses itself via its own collision; an already-resolved (resting) one won't,
            // so trigger its cosmetic effects here.
            var otherBall = collision.collider.GetComponentInParent<BallBehaviour>();
            if (otherBall != null && otherBall != this && otherBall.HasResolved)
                otherBall.PlayMissEffects();

            _pendingMissPoint = collision.contactCount > 0 ? collision.GetContact(0).point : transform.position;
            _pendingCollisionMiss = true;
        }

        #endregion

        #region public api

        public void OnPointerDown(PointerEventData eventData) => _isReleased = false;

        public void SnapToSpawnPoint(Transform spawnPoint)
        {
            if (spawnPoint == null) return;
            transform.SetParent(spawnPoint, false);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            ResetAtSpawnPoint();
        }

        public void MoveToPool(Transform poolParent)
        {
            if (poolParent != null) transform.SetParent(poolParent, true);
        }

        // All ball SFX route through the central AudioService source (fixed at the world origin) rather
        // than an AudioSource on the ball, so the sound isn't cut off when the ball is pooled/destroyed.
        public void PlayThrowEffects()
        {
            if (_data != null) AudioService.PlaySfx(_data.throwSound);
        }

        public void OnBasketHit(Vector3 hitPoint)
        {
            if (_data != null) AudioService.PlaySfx(_data.hitSound);
            if (_data?.hitVFXPrefab) SpawnVfx(_data.hitVFXPrefab, hitPoint, _data.hitVFXSize);
        }

        public void OnMiss(Vector3 hitPoint)
        {
            if (_data != null) AudioService.PlaySfx(_data.missSound);
            if (_data?.missVFXPrefab) SpawnVfx(_data.missVFXPrefab, hitPoint, _data.missVFXSize);
        }

        // Spawn a VFX prefab and let BallData drive its real-world size (the prefab's RealWorldScaler default
        // is overridden here) so the same prefab can be reused at different sizes per ball.
        private static void SpawnVfx(GameObject prefab, Vector3 position, float sizeMeters)
        {
            var vfx = Instantiate(prefab, position, Quaternion.identity);
            if (vfx.TryGetComponent<RealWorldScaler>(out var scaler)) scaler.SetTargetSize(sizeMeters);
        }

        /// <summary>Play the miss SFX/VFX only (no run/score change) — used when this already-resolved ball is knocked by another.</summary>
        public void PlayMissEffects() => OnMiss(transform.position);

        public void MarkResolved() => HasResolved = true;

        #endregion

        #region private helpers

        // Delegate to ScoringSystem.HandleMiss, which marks the ball resolved (via MarkResolved) and plays
        // the miss SFX/VFX. Do NOT set _hasResolved here first — HandleMiss early-returns when the ball is
        // already resolved, which would swallow the miss entirely.
        private void ResolveMiss(Vector3 hitPoint)
        {
            if (HasResolved) return;
            ScoringSystem.Instance?.HandleMiss(this, hitPoint);
        }

        #endregion
    }
}
