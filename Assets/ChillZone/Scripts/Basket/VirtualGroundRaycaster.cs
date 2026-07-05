using ChillZone.Core;
using UnityEngine;

namespace ChillZone.Basket.Utils
{
    /// <summary>
    /// Placement raycaster for the virtual (camera-off) environment: intersects the pointer ray with a
    /// horizontal ground plane at a fixed world height (default y = 0). Has no AR dependency, so it works
    /// on devices without ARCore. Swapped in for <see cref="SurfaceRaycaster"/> while virtual mode is on.
    /// </summary>
    public sealed class VirtualGroundRaycaster : ISurfaceRaycaster
    {
        private readonly float _groundHeight;
        private readonly float _halfExtent; // clamp hits to ±this on X/Z so the basket stays on the ground (0 = unbounded)

        public VirtualGroundRaycaster(float groundHeight = 0f, float halfExtent = 0f)
        {
            _groundHeight = groundHeight;
            _halfExtent = halfExtent;
        }

        #region public api

        public bool TryRaycast(Vector2 screenPosition, out Pose hitPose)
        {
            hitPose = default;

            var cam = CameraProvider.Current;
            if (!cam) return false;

            var ray = cam.ScreenPointToRay(screenPosition);
            var ground = new Plane(Vector3.up, new Vector3(0f, _groundHeight, 0f));
            if (!ground.Raycast(ray, out var enter)) return false;

            var point = ray.GetPoint(enter);
            if (_halfExtent > 0f)
            {
                // Keep the basket on the finite ground (the box footprint) instead of the infinite plane.
                point.x = Mathf.Clamp(point.x, -_halfExtent, _halfExtent);
                point.z = Mathf.Clamp(point.z, -_halfExtent, _halfExtent);
            }

            hitPose = new Pose(point, Quaternion.identity);
            return true;
        }

        #endregion
    }
}
