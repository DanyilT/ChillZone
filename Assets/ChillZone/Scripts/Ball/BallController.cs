using System.Collections.Generic;
using ChillZone.Config;
using ChillZone.Core;
using ChillZone.Core.Events;
using ChillZone.Gameplay;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ChillZone.Ball
{
    [RequireComponent(typeof(BallBehaviour))]
    public class BallController : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        [SerializeField] private ThrowConfig config;

        /// <summary>Called by BallSpawnManager when the component is added at runtime (not from prefab).</summary>
        public void SetConfig(ThrowConfig cfg) => config = cfg;

        /// <summary>True while the player is actively dragging the ready ball. BallSpawnPoint reads this to suspend its screen-follow so the finger owns the ball mid-aim.</summary>
        public bool IsDragging { get; private set; }

        /// <summary>True while ANY ready ball is being dragged to throw. BasketController reads this to ignore basket move/delete input during a throw drag — so a basket behind the ball can't be dragged along with it.</summary>
        public static bool IsAnyDragging { get; private set; }

        private Vector2 _dragStartScreen;
        private Vector2 _dragCurrentScreen;
        private BallBehaviour _ball;
        private float _dragDepth;

        private readonly List<Vector2> _dragHistory = new();
        private readonly List<float> _dragTimes = new();

        private void Awake()
        {
            _ball = GetComponent<BallBehaviour>();
            ResolveCamera();
        }

        // Safety net: if the ball is pooled/disabled mid-drag, clear the shared flag so basket input isn't locked out.
        private void OnDisable()
        {
            if (IsDragging) { IsDragging = false; IsAnyDragging = false; }
        }

        // Camera.main can be null mid-session in AR; CameraProvider resolves + caches a fallback.
        private static Camera ResolveCamera() => CameraProvider.Current;

        #region pointer (touch / ui raycast)

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!IsActiveSpawnedBall()) return;
            // Don't grab the ball through the HUD (picker sheet / buttons) sitting in front of it.
            if (PointerOverUI.At(eventData.position)) return;
            StartDrag(eventData.position);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!IsDragging) return;
            MoveBallTo(eventData.position);
            UpdateDrag(eventData.position);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!IsDragging) return;
            UpdateDrag(eventData.position);
            EndDrag();
        }

        #endregion

        #region mouse (editor / standalone fallback)

        private void OnMouseDown()
        {
            if (!IsActiveSpawnedBall()) return;
            if (PointerOverUI.At(Input.mousePosition)) return;
            StartDrag(Input.mousePosition);
        }

        private void OnMouseDrag()
        {
            if (!IsDragging) return;
            MoveBallTo(Input.mousePosition);
            UpdateDrag(Input.mousePosition);
        }

        private void OnMouseUp()
        {
            if (!IsDragging) return;
            UpdateDrag(Input.mousePosition);
            EndDrag();
        }

        #endregion

        #region drag state

        private void StartDrag(Vector2 position)
        {
            var cam = ResolveCamera();
            // Lock the ball's distance from the camera so dragging only slides it across the view.
            _dragDepth = cam ? cam.WorldToScreenPoint(transform.position).z : 2f;

            _dragStartScreen = position;
            _dragCurrentScreen = position;
            IsDragging = true;
            IsAnyDragging = true;
            _dragHistory.Clear();
            _dragTimes.Clear();
            _dragHistory.Add(position);
            _dragTimes.Add(Time.unscaledTime);
        }

        // Slide the held ball across the view at its locked depth so it tracks the finger 1:1 while aiming.
        // z is the distance from the camera captured at drag start, NOT world z — otherwise the ball
        // collapses to the wrong depth and disappears.
        private void MoveBallTo(Vector2 screenPosition)
        {
            var cam = ResolveCamera();
            if (cam) transform.position = cam.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, _dragDepth));
        }

        private void UpdateDrag(Vector2 position)
        {
            _dragCurrentScreen = position;
            _dragHistory.Add(position);
            _dragTimes.Add(Time.unscaledTime);
            var maxSize = config ? config.dragHistorySize : 15;
            while (_dragHistory.Count > maxSize)
            {
                _dragHistory.RemoveAt(0);
                _dragTimes.RemoveAt(0);
            }
        }

        private void EndDrag()
        {
            IsDragging = false;
            IsAnyDragging = false;
            var dragVector = _dragCurrentScreen - _dragStartScreen;
            var minDrag = config ? config.minDragDistance : 80f;
            if (dragVector.magnitude < minDrag)
            {
                TrySnapToSpawnPoint();
                return;
            }
            ThrowFromDrag(dragVector);
        }

        #endregion

        #region throw

        // Launches the ball forward into the scene: ThrowCalculator turns the flick into a forward+arc
        // velocity (forward from flick speed, up from how far the swipe was dragged, lean from its direction).
        private void ThrowFromDrag(Vector2 dragVector)
        {
            if (BallSpawnManager.Instance == null || BallSpawnManager.Instance.ActiveBall != gameObject) return;
            var cam = ResolveCamera();
            if (!cam) return;

            _ball?.PlayThrowEffects();

            var solution = ThrowCalculator.Solve(_dragHistory, _dragTimes, dragVector, cam, config);

            // Forward the swipe curvature so ScoringSystem can compute the difficulty multiplier.
            EventBus<BallThrownEvent>.Raise(new BallThrownEvent
            {
                Mode = config ? config.mode : ThrowMode.Enhanced,
                TotalCurvature = solution.TotalCurvature,
                ReleasePosition = transform.position,
            });

            if (BallSpawnPoint.Instance != null)
                transform.SetParent(BallSpawnPoint.Instance.PoolParent, true);

            _ball?.Release();
            BallSpawnManager.Instance.ReleaseActiveBall(solution.LinearVelocity, solution.AngularVelocity);
        }

        #endregion

        #region helpers

        private void TrySnapToSpawnPoint()
        {
            if (BallSpawnPoint.Instance == null || _ball == null) return;
            var spawnCollider = BallSpawnPoint.Instance.SpawnCollider;
            var ballCollider = _ball.GetComponent<Collider>();
            if (spawnCollider != null && ballCollider != null && ballCollider.bounds.Intersects(spawnCollider.bounds))
                _ball.SnapToSpawnPoint(BallSpawnPoint.Instance.SpawnParent);
        }

        private bool IsActiveSpawnedBall() =>
            BallSpawnManager.Instance != null
            && BallSpawnManager.Instance.ActiveBall == gameObject
            && _ball != null;

        #endregion
    }
}
