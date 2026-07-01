using UnityEngine;

namespace ChillZone.Core
{
    /// <summary>
    /// Resolves the active scene camera robustly and caches it. <c>Camera.main</c> can be null
    /// mid-session in AR, so this falls back to any camera in the scene.
    /// </summary>
    /// <remarks>
    /// Returns the cached camera while it is alive; re-resolves once it is destroyed (e.g. on scene
    /// reload). Components that must act on a specific camera while it is being disabled (such as the
    /// pause flow toggling the AR camera) should keep their own reference rather than use this.
    /// </remarks>
    public static class CameraProvider
    {
        private static Camera _cached;

        /// <summary>The best-guess active camera, or null if none exists.</summary>
        public static Camera Current
        {
            get
            {
                if (_cached) return _cached;
                _cached = Camera.main;
                if (!_cached) _cached = Object.FindObjectOfType<Camera>();
                return _cached;
            }
        }

        /// <summary>Forget the cached camera so the next access re-resolves.</summary>
        public static void Clear() => _cached = null;
    }
}
