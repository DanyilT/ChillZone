using UnityEngine;

namespace ChillZone.UI.Helpers
{
    /// <summary>
    /// Detects the front camera / notch position and size using <see cref="Screen.cutouts"/>.
    /// Result is cached after the first call; call <see cref="ClearCache"/> on orientation change.
    /// </summary>
    public static class CameraDetectionHelper
    {
        public enum CameraPosition { Unknown, Left, Right, Center, None }

        /// <summary>
        /// Detected cutout: coarse <see cref="Position"/>, exact screen-pixel <see cref="Rect"/>
        /// (position + size), and resolution-independent normalized X accessors for placing UI
        /// around it regardless of screen resolution.
        /// </summary>
        public readonly struct CutoutInfo
        {
            public readonly CameraPosition Position;
            /// <summary>Cutout bounds in screen pixels (x, y, width, height). <see cref="Rect.zero"/> when there is no top cutout.</summary>
            public readonly Rect Rect;

            public CutoutInfo(CameraPosition position, Rect rect)
            {
                Position = position;
                Rect = rect;
            }

            /// <summary>True when a real top cutout (camera / notch) was found.</summary>
            public bool Exists => Position is CameraPosition.Left or CameraPosition.Right or CameraPosition.Center;

            /// <summary>Left edge of the cutout as a 0..1 fraction of screen width.</summary>
            public float NormalizedMinX => Screen.width > 0 ? Rect.xMin / Screen.width : 0f;
            /// <summary>Right edge of the cutout as a 0..1 fraction of screen width.</summary>
            public float NormalizedMaxX => Screen.width > 0 ? Rect.xMax / Screen.width : 0f;
            /// <summary>Centre of the cutout as a 0..1 fraction of screen width.</summary>
            public float NormalizedCenterX => Screen.width > 0 ? (Rect.x + Rect.width * 0.5f) / Screen.width : 0f;
            /// <summary>Cutout width as a 0..1 fraction of screen width.</summary>
            public float NormalizedWidth => Screen.width > 0 ? Rect.width / Screen.width : 0f;

            public static readonly CutoutInfo None = new(CameraPosition.None, Rect.zero);
        }

        private static CutoutInfo? _cached;

        #region public static api

        /// <summary>
        /// Detects the front camera / notch position and bounds using Unity's <see cref="Screen.cutouts"/>.
        /// The result is cached after the first call to prevent redundant work.
        /// </summary>
        /// <param name="showDebugLogs">If true, detection details are printed to the console.</param>
        /// <returns>The detected <see cref="CutoutInfo"/> (position + screen-pixel rect).</returns>
        public static CutoutInfo DetectCutout(bool showDebugLogs = false)
        {
            if (_cached.HasValue) return _cached.Value;

            if (showDebugLogs)
            {
                Debug.Log($"[CameraDetection] Screen: {Screen.width}x{Screen.height}");
                Debug.Log($"[CameraDetection] Safe Area: {Screen.safeArea}");
                Debug.Log($"[CameraDetection] Cutouts Count: {Screen.cutouts.Length}");
            }

            if (Screen.cutouts.Length == 0)
            {
                if (showDebugLogs)
                    Debug.Log("[CameraDetection] No cutouts detected");
                return Cache(CutoutInfo.None, showDebugLogs);
            }

            // Analyze each cutout — keep the first one in the top half (cameras / notches).
            foreach (var cutout in Screen.cutouts)
            {
                var cx = cutout.x + cutout.width  * 0.5f;
                var cy = cutout.y + cutout.height * 0.5f;

                if (showDebugLogs)
                {
                    Debug.Log($"[CameraDetection] Cutout {cutout}: x={cutout.x}, y={cutout.y}, width={cutout.width}, height={cutout.height}");
                    Debug.Log($"[CameraDetection] Cutout center X: {cx}, Screen center X: {Screen.width * 0.5f}");
                    Debug.Log($"[CameraDetection] Cutout center Y: {cy}, Screen center Y: {Screen.height * 0.5f}");
                }

                if (cy <= Screen.height * 0.5f) continue;  // only top cutouts (cameras/notches)

                var position = cx < Screen.width * 0.33f ? CameraPosition.Left
                             : cx > Screen.width * 0.67f ? CameraPosition.Right
                             : CameraPosition.Center;
                return Cache(new CutoutInfo(position, cutout), showDebugLogs);
            }

            if (showDebugLogs)
                Debug.Log("[CameraDetection] No top cutout, set position to Unknown");
            return Cache(new CutoutInfo(CameraPosition.Unknown, Rect.zero), showDebugLogs);
        }

        /// <summary>Detects only the camera / notch position. See <see cref="DetectCutout"/> for the bounds too.</summary>
        public static CameraPosition Detect(bool showDebugLogs = false) => DetectCutout(showDebugLogs).Position;

        /// <summary>
        /// Clears the cached detection result.
        /// Call this manually when the device orientation changes or the previous result may be stale.
        /// </summary>
        public static void ClearCache() => _cached = null;

        /// <summary>Overrides the cached position with no bounds (rect = zero).</summary>
        /// <param name="position">The camera position to cache.</param>
        public static void Override(CameraPosition position) => _cached = new CutoutInfo(position, Rect.zero);

        /// <summary>Overrides the cached position and bounds.</summary>
        /// <param name="position">The camera position to cache.</param>
        /// <param name="rect">The cutout bounds in screen pixels.</param>
        public static void Override(CameraPosition position, Rect rect) => _cached = new CutoutInfo(position, rect);

        #endregion

        #region internal

        /// <summary>Caches the detected cutout and optionally logs the result.</summary>
        private static CutoutInfo Cache(CutoutInfo info, bool showDebugLogs = false)
        {
            if (showDebugLogs)
                Debug.Log($"[CameraDetection] ✓ Camera/Notch – {info.Position} @ {info.Rect}");
            _cached = info;
            return info;
        }

        #endregion
    }
}
