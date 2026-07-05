using System;
using System.Linq;
using ChillZone.Ball;
using ChillZone.Basket.Utils;
using ChillZone.Core;
using ChillZone.Gameplay;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ChillZone.Basket
{
    /// <summary>
    /// Lives on the basket prefab. Owns the placed basket's own behaviour: grounding
    /// itself on a surface, hold-and-drag repositioning, double-tap removal, and the
    /// fuzzy on-screen selection test. Driven each gated frame by BasketSpawnManager,
    /// which reads input only while no basket exists and otherwise forwards the tick here.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class BasketController : MonoBehaviour
    {
        [SerializeField, Header("Grounding"), Tooltip("Tiny gap (metres) kept between the basket's base and the surface to avoid z-fighting with the plane.")]
        private float surfaceClearance = 0.002f;
        [SerializeField, Header("Selection"), Tooltip("Extra screen-space padding (pixels) around the basket so the small target is easy to grab/double-tap.")]
        private float tapPaddingPixels = 10f;

        /// <summary>Raised right before the basket destroys itself on a double-tap.</summary>
        public event Action Deleted;

        private ISurfaceRaycaster _raycaster;
        private Renderer[] _renderers;
        private bool _isDragging;
        private float _lastTapTime;
        private const float DoubleTapThreshold = 0.3f;

        #region public api

        /// <summary>Called by BasketSpawnManager right after the basket is instantiated.</summary>
        public void Initialize(ISurfaceRaycaster raycaster)
        {
            _raycaster = raycaster;
            _renderers = GetComponentsInChildren<Renderer>(); // Cache after Instantiate so RealWorldScaler (runs in Awake) has set the scale.
            // Keep the placed basket fixed so it doesn't drift and so dragging it via transform is stable (its score collider is a trigger, so kinematic is fine).
            if (TryGetComponent<Rigidbody>(out var rb)) rb.isKinematic = true;
        }

        /// <summary>The last surface point this basket was grounded on (world). Lets a replacement basket be re-grounded at the same spot.</summary>
        public Vector3 GroundSurfacePoint { get; private set; }

        /// <summary>Places the basket so its lowest visible point rests on the surface point (plus a tiny clearance), instead of sinking the centred pivot into the plane.</summary>
        public void GroundOn(Vector3 surfacePoint)
        {
            GroundSurfacePoint = surfacePoint;
            transform.position = surfacePoint;
            if (TryGetWorldBounds(out var bounds))
                transform.position += Vector3.up * (surfacePoint.y - bounds.min.y + surfaceClearance);
        }

        /// <summary>Reads the primary pointer and handles hold-and-drag-to-move and double-tap-to-remove. Only invoked by BasketSpawnManager while a basket exists.</summary>
        public void HandleInteraction()
        {
            var pointer = Pointer.current;
            if (pointer == null) return;

            // While the player is dragging the ready ball to throw, don't let a basket (even one behind the ball)
            // steal the input — cancel any in-progress basket drag and ignore taps until the throw drag ends.
            if (BallController.IsAnyDragging) { _isDragging = false; return; }

            var position = pointer.position.ReadValue();

            if (pointer.press.wasPressedThisFrame)
            {
                // A tap on the HUD (picker sheet / buttons) isn't a world interaction — ignore it.
                if (PointerOverUI.At(position)) return;

                // Double-tap on the basket removes it.
                if (Time.time - _lastTapTime < DoubleTapThreshold && IsPointerOnBasket(position))
                {
                    _lastTapTime = 0f;
                    DeleteSelf();
                    return;
                }

                _lastTapTime = Time.time;
                _isDragging = IsPointerOnBasket(position);
                return;
            }

            if (pointer.press.isPressed)
            {
                if (_isDragging) Move(position);
                return;
            }

            if (pointer.press.wasReleasedThisFrame)
                _isDragging = false;
        }

        #endregion

        #region move / delete (private)

        private void Move(Vector2 screenPosition)
        {
            // Re-raycast every drag frame so the basket stays glued to a real plane.
            if (_raycaster != null && _raycaster.TryRaycast(screenPosition, out var hitPose))
                GroundOn(hitPose.position);
        }

        private void DeleteSelf()
        {
            _isDragging = false;
            Deleted?.Invoke();
            Destroy(gameObject);
        }

        #endregion

        #region selection (private)

        /// <summary>
        /// True when the tap is on (or near) the basket. Uses a precise physics raycast
        /// first, then falls back to the basket's on-screen bounding box plus padding —
        /// the fallback is what makes the small basket selectable despite the AR plane
        /// mesh colliders sitting right under it. The fuzzy fallback yields to any other
        /// dynamic body (e.g. the ball) so it never steals a throw drag.
        /// </summary>
        private bool IsPointerOnBasket(Vector2 screenPosition)
        {
            var cam = ResolveCamera();
            if (!cam) return false;

            var ray  = cam.ScreenPointToRay(screenPosition);
            var hits = Physics.RaycastAll(ray, 100f, ~0, QueryTriggerInteraction.Collide);

            // Direct hit anywhere on the basket hierarchy → definitely the basket.
            if (hits.Any(h => h.collider && h.collider.transform.IsChildOf(transform))) return true;

            // The basket carries a Rigidbody; AR planes don't. If the ray hits some other rigidbody (the ball), the user is interacting with that, not the basket.
            if (hits.Select(h => h.collider ? h.collider.attachedRigidbody : null).Any(rb => rb && !rb.transform.IsChildOf(transform))) return false;

            return TryGetBasketScreenRect(cam, out var rect) && rect.Contains(screenPosition);
        }

        private bool TryGetBasketScreenRect(Camera cam, out Rect rect)
        {
            rect = default;
            if (!TryGetWorldBounds(out var bounds)) return false;

            var center = bounds.center;
            var ext = bounds.extents;
            var min = new Vector2(float.MaxValue, float.MaxValue);
            var max = new Vector2(float.MinValue, float.MinValue);

            for (var i = 0; i < 8; i++)
            {
                var corner = center + new Vector3(
                    (i & 1) == 0 ? -ext.x : ext.x,
                    (i & 2) == 0 ? -ext.y : ext.y,
                    (i & 4) == 0 ? -ext.z : ext.z);
                var sp = cam.WorldToScreenPoint(corner);
                if (sp.z < 0f) return false; // basket partly behind camera → skip the fuzzy test
                min = Vector2.Min(min, sp);
                max = Vector2.Max(max, sp);
            }

            rect = Rect.MinMaxRect(min.x - tapPaddingPixels, min.y - tapPaddingPixels,
                                   max.x + tapPaddingPixels, max.y + tapPaddingPixels);
            return true;
        }

        #endregion

        #region helpers (private)

        private bool TryGetWorldBounds(out Bounds bounds)
        {
            bounds = default;
            _renderers ??= GetComponentsInChildren<Renderer>();

            var found = false;
            foreach (var r in _renderers)
            {
                if (!r) continue;
                if (!found) { bounds = r.bounds; found = true; }
                else bounds.Encapsulate(r.bounds);
            }
            return found;
        }

        /// <summary>Robustly resolves the AR camera (Camera.main can be null mid-session).</summary>
        private static Camera ResolveCamera() => CameraProvider.Current;

        #endregion
    }
}
