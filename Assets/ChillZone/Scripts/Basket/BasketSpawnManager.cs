using System;
using ChillZone.Basket.Utils;
using ChillZone.Content;
using ChillZone.Core;
using ChillZone.Gameplay;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.ARFoundation;

namespace ChillZone.Basket
{
    /// <summary>
    /// Scene component that owns the single AR basket's lifecycle. While no basket exists
    /// it reads the primary pointer: a tap on a valid floor instantiates the basket prefab,
    /// hands it a shared <see cref="SurfaceRaycaster"/> and grounds it. Once a basket is
    /// placed it forwards the (state-gated) tick to the basket's own <see cref="BasketController"/>,
    /// which owns move/delete. Driven by GameFlowController in the Placing/Playing states.
    /// </summary>
    public class BasketSpawnManager : MonoBehaviour
    {
        [SerializeField, Header("Placement filtering"), Tooltip("Reject surfaces tilted more than this many degrees from world-up, even if AR classifies them as horizontal.")]
        private float maxSurfaceTiltDegrees = 15f;
        [SerializeField, Tooltip("Basket registry — fallback source for the first/default basket when no ContentManager selection is available.")]
        private UnlockableContentRegistry registry;

        public static BasketSpawnManager Instance { get; private set; }

        private ARRaycastManager _raycastManager;
        private ARPlaneManager _planeManager;
        private ISurfaceRaycaster _raycaster;

        private GameObject _basket;
        private BasketController _controller;
        private Action _onDestroyed;

        #region setup

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void Initialize(ARRaycastManager raycastManager, ARPlaneManager planeManager = null)
        {
            _raycastManager = raycastManager;
            _planeManager = planeManager ? planeManager : FindObjectOfType<ARPlaneManager>();
            _raycaster = new SurfaceRaycaster(_raycastManager, _planeManager, maxSurfaceTiltDegrees);
        }

        public GameObject GetBasket() => _basket;

        /// <summary>Switch placement/move to the virtual ground plane (camera-off mode), clamped to a ±halfExtent square so the basket stays on the ground. Safe to call while no basket exists.</summary>
        public void UseVirtualGround(float groundHeight = 0f, float halfExtent = 0f) => _raycaster = new VirtualGroundRaycaster(groundHeight, halfExtent);

        /// <summary>Switch placement/move back to AR detected planes (rebuilds the AR raycaster from the managers passed to Initialize).</summary>
        public void UseARSurface()
        {
            if (_raycastManager) _raycaster = new SurfaceRaycaster(_raycastManager, _planeManager, maxSurfaceTiltDegrees);
        }

        #endregion

        #region input

        /// <summary>Driven by GameFlowController while in Placing/Playing. With no basket it reads the pointer to place one; with a basket it forwards the tick to the BasketController.</summary>
        public void HandleInput(Action onPlaced, Action onDestroyed = null)
        {
            _onDestroyed = onDestroyed;

            if (_basket)
            {
                _controller?.HandleInteraction();
                return;
            }

            var pointer = Pointer.current;
            if (pointer == null || !pointer.press.wasPressedThisFrame) return;

            var screenPosition = pointer.position.ReadValue();
            if (PointerOverUI.At(screenPosition)) return;  // tap landed on the HUD, not the floor

            if (TryPlaceBasket(screenPosition, ResolveBasketPrefab()))
                onPlaced?.Invoke();
        }

        /// <summary>
        /// Editor/debug helper: drop the basket a fixed distance straight in front of the camera (no surface
        /// raycast — XR Simulation planes are unreliable), with the prefab's own authored rotation.
        /// Returns true once placed (or if one already exists). Used by GameFlowController's debug auto-start.
        /// </summary>
        public bool PlaceBasketInFrontOfCamera(float distanceMeters)
        {
            if (_basket) return true;

            var prefab = ResolveBasketPrefab();
            var cam = CameraProvider.Current;
            if (!prefab || !cam) return false;

            // Horizontal forward so the basket sits level regardless of camera pitch.
            var forward = cam.transform.forward;
            forward.y = 0f;
            forward = forward.sqrMagnitude > 1e-4f ? forward.normalized : Vector3.forward;

            var position = cam.transform.position + forward * distanceMeters;
            position.y = 0f;

            PlaceBasket(prefab, position, ground: false);
            return true;
        }

        // The basket to place: the player's last-selected basket (ContentManager persists + defaults it), falling
        // back to the first basket in the registry when no ContentManager selection is available.
        private GameObject ResolveBasketPrefab()
        {
            var prefab = ContentManager.Instance ? ContentManager.Instance.GetSelected<BasketData>(ContentTypes.Basket)?.prefab : null;
            if (!prefab && registry) prefab = (registry.GetDefaultContent() as BasketData)?.prefab;
            return prefab;
        }

        #endregion

        #region placement (private)

        private bool TryPlaceBasket(Vector2 screenPosition, GameObject prefab)
        {
            if (_basket || !prefab || _raycaster == null) return false;
            if (!_raycaster.TryRaycast(screenPosition, out var hitPose)) return false;

            PlaceBasket(prefab, hitPose.position, ground: true);
            return true;
        }

        // Instantiate the basket with the prefab's OWN authored rotation (the placement pose's rotation is
        // ignored) and wire its controller. `ground` rests its base on the point (for real surface placement);
        // the debug in-front spawn skips grounding since there's no floor to sit on.
        private void PlaceBasket(GameObject prefab, Vector3 position, bool ground)
        {
            _basket = Instantiate(prefab, position, prefab.transform.rotation);
            _controller = _basket.GetComponent<BasketController>() ?? _basket.AddComponent<BasketController>();
            _controller.Initialize(_raycaster);
            _controller.Deleted += OnBasketDeleted;
            if (ground) _controller.GroundOn(position);
        }

        private void OnBasketDeleted()
        {
            if (_controller) _controller.Deleted -= OnBasketDeleted;
            _basket = null;
            _controller = null;
            _onDestroyed?.Invoke();
        }

        /// <summary>Destroys the placed basket (if any) without firing the onDestroyed callback — used by scan reset, which drives its own next state.</summary>
        public void RemoveBasket()
        {
            if (!_basket) return;
            if (_controller) _controller.Deleted -= OnBasketDeleted;
            Destroy(_basket);
            _basket = null;
            _controller = null;
        }

        /// <summary>Swaps the placed basket for the currently-selected one at the same grounded spot. No-op if none is placed.
        /// Call AFTER ContentManager.Select so <see cref="ResolveBasketPrefab"/> returns the new pick.</summary>
        public void ReplaceBasketWithSelected()
        {
            if (!_basket) return;

            var prefab = ResolveBasketPrefab();
            if (!prefab) return;

            var surfacePoint = _controller ? _controller.GroundSurfacePoint : _basket.transform.position;
            RemoveBasket();
            PlaceBasket(prefab, surfacePoint, ground: true);
        }

        #endregion
    }
}
