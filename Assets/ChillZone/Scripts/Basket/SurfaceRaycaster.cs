using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace ChillZone.Basket.Utils
{
    /// <summary>
    /// Shared AR raycasting helper. Casts a screen point against detected planes and
    /// returns the nearest hit on an upward-facing, near-level, non-subsumed, tracking
    /// plane — the single source of the "what counts as a valid floor" rules used by
    /// both basket placement (<see cref="BasketSpawnManager"></see>)
    /// and repositioning (<see cref="BasketController"></see>).
    /// </summary>
    public sealed class SurfaceRaycaster
    {
        private readonly ARRaycastManager _raycastManager;
        private readonly ARPlaneManager _planeManager;
        private readonly float _maxTiltDegrees;
        private readonly List<ARRaycastHit> _hits = new();

        public SurfaceRaycaster(ARRaycastManager raycastManager, ARPlaneManager planeManager, float maxTiltDegrees)
        {
            _raycastManager = raycastManager;
            _planeManager   = planeManager;
            _maxTiltDegrees = maxTiltDegrees;
        }

        #region public api

        /// <summary>
        /// Raycasts against detected planes and returns the nearest hit that lies on an
        /// upward-facing, near-level, non-subsumed plane. Rejects walls, tilted planes,
        /// and stale/overlapping planes that AR has merged away.
        /// </summary>
        public bool TryRaycast(Vector2 screenPosition, out Pose hitPose)
        {
            hitPose = default;
            if (!_raycastManager) return false;
            if (!_raycastManager.Raycast(screenPosition, _hits, TrackableType.PlaneWithinPolygon)) return false;

            // _hits is sorted nearest-first; take the first one that passes every filter.
            foreach (var hit in _hits.Where(hit => !(Vector3.Angle(hit.pose.up, Vector3.up) > _maxTiltDegrees)).Where(hit => IsValidPlacementPlane(hit.trackableId)))
            {
                hitPose = hit.pose;
                return true;
            }

            return false;
        }

        #endregion

        #region helpers (private)

        private bool IsValidPlacementPlane(TrackableId planeId)
        {
            if (!_planeManager) return true; // no manager → can't filter, accept hit
            var plane = _planeManager.GetPlane(planeId);
            return plane && !plane.subsumedBy && plane.trackingState == TrackingState.Tracking && plane.alignment == PlaneAlignment.HorizontalUp;
        }

        #endregion
    }
}
