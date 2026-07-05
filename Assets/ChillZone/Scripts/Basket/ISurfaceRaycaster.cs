using UnityEngine;

namespace ChillZone.Basket.Utils
{
    /// <summary>
    /// Abstraction over "cast a screen point at a placement surface and return a pose".
    /// Implemented by <see cref="SurfaceRaycaster"/> (AR detected planes) and
    /// <see cref="VirtualGroundRaycaster"/> (a virtual ground plane at a fixed height), so basket
    /// placement/repositioning works the same in AR and virtual-environment (camera-off) modes.
    /// </summary>
    public interface ISurfaceRaycaster
    {
        bool TryRaycast(Vector2 screenPosition, out Pose hitPose);
    }
}
