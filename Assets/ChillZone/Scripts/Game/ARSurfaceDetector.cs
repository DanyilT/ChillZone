using System;
using System.Collections.Generic;
using ChillZone.Config;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace ChillZone.Game
{
    public class ARSurfaceDetector : MonoBehaviour
    {
        [SerializeField, Tooltip("Reference to the ARPlaneManager in the scene. If not set, it will try to find one in the scene.")]
        private ARPlaneManager planeManager;
        [SerializeField, Tooltip("Game config supplying the minimum placement plane area (minimumPlaneArea). Falls back to 1m² if unset.")]
        private GameConfig gameConfig;

        // Single source of the minimum placement area, shared with BasketSpawnManager via GameConfig.
        private float MinSurfaceArea => gameConfig ? gameConfig.minimumPlaneArea : 1f;

        public Action OnSurfaceRequirementMet;
        private bool _requirementMet;

        // Planes that already existed at the last ResetDetection. After resetting scanning, AR usually keeps
        // (or instantly re-reports) the old floor, which would satisfy detection on the next frame and skip
        // the scan hint straight to placing. Ignoring those ids forces the user to scan a genuinely new
        // surface, so the scanning state actually shows.
        private readonly HashSet<TrackableId> _ignoredPlanes = new();

        private void Awake()
        {
            if (planeManager == null)
                planeManager = FindObjectOfType<ARPlaneManager>();

            // Only ever look for floor/table-like surfaces. This stops walls and other
            // vertical planes from being treated as valid placement surfaces.
            if (planeManager != null)
                planeManager.requestedDetectionMode = PlaneDetectionMode.Horizontal;
        }

        private void Update()
        {
            if (_requirementMet) return;
            if (!CheckScanningProgress()) return;

            _requirementMet = true;
            OnSurfaceRequirementMet?.Invoke();
        }

        private bool CheckScanningProgress()
        {
            if (!planeManager) return false;
            foreach (var plane in planeManager.trackables)
                if (IsQualifiedPlane(plane)) return true;
            return false;
        }

        private bool IsQualifiedPlane(ARPlane plane)
        {
            if (!plane || plane.trackingState != TrackingState.Tracking) return false;

            // Planes carried over from before a scan reset don't count — require a freshly detected surface.
            if (_ignoredPlanes.Contains(plane.trackableId)) return false;

            // Ignore planes AR has merged into a bigger one — they're stale duplicates and cause the "overlapping planes" glitches.
            if (plane.subsumedBy) return false;

            // Reject walls / ceilings / vertical planes — only an upward-facing horizontal surface can hold a basket.
            if (plane.alignment != PlaneAlignment.HorizontalUp) return false;

            // extents are half-size, so multiply by 4 to get the full area.
            return plane.extents.x * plane.extents.y * 4f >= MinSurfaceArea;
        }

        public void ResetDetection()
        {
            _requirementMet = false;

            // Snapshot the planes that exist right now and ignore them, so detection only fires for a new
            // surface scanned after this reset (a re-detected old floor gets a new TrackableId and counts).
            _ignoredPlanes.Clear();
            if (!planeManager) return;
            foreach (var plane in planeManager.trackables)
                _ignoredPlanes.Add(plane.trackableId);
        }
    }
}
