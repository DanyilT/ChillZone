#if UNITY_EDITOR
using ChillZone.UI.Header;
using ChillZone.UI.Header.Config;
using ChillZone.UI.Helpers;
using ChillZone.UI.Utils.Config;
using ChillZone.UI.Window.Utils;
using UnityEngine;

namespace ChillZone.UI.Preview
{
    /// <summary>
    /// Edit-mode preview for <see cref="AdaptiveHeaderConfig"/>. Renders a mock header
    /// (panel + left / right placeholders + cutout spacer) using the exact layout logic
    /// AdaptiveHeader runs at runtime, so what you see here matches the device.
    ///
    /// Device metrics (status-bar height, cutout, safe area) come from one of two sources:
    ///   • useSimulatorDevice = true  → the live Unity Device Simulator (Screen.safeArea /
    ///     Screen.cutouts). Open the Device Simulator window and pick a phone to drive it.
    ///   • useSimulatorDevice = false → the manual "Simulated Device" fields below.
    /// </summary>
    [ExecuteAlways]
    public class AdaptiveHeaderPreview : RenderPreview
    {
        [SerializeField, Tooltip("Config asset to preview.")]
        private AdaptiveHeaderConfig config;

        [Header("Device Source")]
        [SerializeField, Tooltip("Read status-bar height + cutout from the active Unity Device Simulator (Screen.safeArea / Screen.cutouts). Outside the Simulator window this reports a plain no-notch screen. When off, the manual fields below are used.")]
        private bool useSimulatorDevice;

        [Header("Manual Device (when not using the Simulator)")]
        [SerializeField, Tooltip("Simulated notch / front-camera position.")]
        private CameraDetectionHelper.CameraPosition simulatedCamera = CameraDetectionHelper.CameraPosition.None;
        [SerializeField, Tooltip("Simulated cutout width in canvas units. Drives the fixed spacer width.")]
        private float simulatedCutoutWidth = 140f;
        [SerializeField, Tooltip("Simulated status-bar height in canvas units — drives header height (useStatusBarHeight) and the safe-area top inset (useSafeArea).")]
        private float simulatedStatusBarHeight = 100f;

        [Header("Preview Colors")]
        [SerializeField] private Color panelColor  = new(0f, 0f, 0f, 0.6f);
        [SerializeField] private Color leftColor   = new(0.2f, 0.6f, 1f, 0.85f);
        [SerializeField] private Color rightColor  = new(1f, 0.55f, 0.2f, 0.85f);
        [SerializeField] private Color cutoutColor = new(0.9f, 0.1f, 0.1f, 0.6f);

        private Vector2Int _lastScreenSize;

        protected override string PreviewName => useSimulatorDevice ? "_AdaptiveHeaderPreview_Device" : $"_AdaptiveHeaderPreview_{simulatedCamera}";

        // Device-Simulator changes resize Screen but don't fire inspector-change events,
        // so poll the screen size and rebuild when it changes while reading the simulator.
        private void Update()
        {
            if (!useSimulatorDevice || Application.isPlaying) return;
            var size = new Vector2Int(Screen.width, Screen.height);
            if (size == _lastScreenSize) return;
            _lastScreenSize = size;
            Rebuild();
        }

        protected override void Rebuild()
        {
            base.Rebuild();
            if (!config) return;

            var canvas = PreviewRoot.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = Camera.main;
            canvas.planeDistance = 10f;

            var canvasWidth = ((RectTransform)canvas.transform).rect.width;
            if (canvasWidth <= 0f) canvasWidth = Screen.width > 0 ? Screen.width : 1080f;

            // Device metrics: either the live Device Simulator, or the manual fields.
            var camera = simulatedCamera;
            var statusBarHeight = simulatedStatusBarHeight;
            var cutoutWidth = simulatedCutoutWidth;
            var leftInset = 0f;
            var rightInset = 0f;

            if (useSimulatorDevice && Screen.width > 0)
            {
                var unitsPerPixel = canvasWidth / Screen.width;
                statusBarHeight = Mathf.Max(0f, Screen.height - Screen.safeArea.yMax) * unitsPerPixel;

                CameraDetectionHelper.ClearCache(); // re-read in case the simulated device changed
                var info = CameraDetectionHelper.DetectCutout();
                camera = info.Position;
                cutoutWidth = info.Exists ? info.Rect.width * unitsPerPixel : 0f;

                if (config.useSafeArea)
                {
                    leftInset  = Mathf.Max(0f, Screen.safeArea.xMin) * unitsPerPixel;
                    rightInset = Mathf.Max(0f, Screen.width - Screen.safeArea.xMax) * unitsPerPixel;
                }
            }

            // Resolve the same values AdaptiveHeader computes at runtime.
            var statusBar = config.useStatusBarHeight ? statusBarHeight : 0f;
            var height = AdaptiveHeader.ResolveHeight(config, statusBar);
            var topInset = config.useSafeArea ? statusBarHeight : 0f;

            var cameraExists = camera is CameraDetectionHelper.CameraPosition.Left or CameraDetectionHelper.CameraPosition.Right or CameraDetectionHelper.CameraPosition.Center;
            var spacerWidth = config.useAdaptiveHeader && !config.useSafeArea && cameraExists ? cutoutWidth : 0f;

            var (leftPos, rightPos) = config.useAdaptiveHeader ? AdaptiveHeader.ResolvePositions(camera, config.useCollapse) : (AdaptiveHeader.HeaderElementPosition.Left, AdaptiveHeader.HeaderElementPosition.Right);

            // Header panel + side placeholders + cutout spacer, positioned by the real layout method.
            var panel = RenderUtils.CreateImage(PreviewRoot.transform, panelColor, name: "HeaderPanel").GetComponent<RectTransform>();
            var left = BuildPlaceholder(panel, "Left",  leftColor);
            var right = BuildPlaceholder(panel, "Right", rightColor);
            var spacer = RenderUtils.CreateImage(panel, cutoutColor, name: "Cutout").GetComponent<RectTransform>();

            AdaptiveHeader.ApplyHeaderLayout(panel, left, right, spacer, config, height, leftInset, rightInset, topInset, camera, leftPos, rightPos, spacerWidth);
        }

        /// <summary>Create a coloured side placeholder with a centered label.</summary>
        private static RectTransform BuildPlaceholder(Transform parent, string label, Color color)
        {
            var rect = RenderUtils.CreateImage(parent, color, name: label).GetComponent<RectTransform>();
            var txt  = RenderUtils.CreateText(rect, label, TextConfig.ButtonTextDefault(), "Label").GetComponent<RectTransform>();
            RenderUtils.SetupRectTransformFullScreen(txt);
            return rect;
        }
    }
}
#endif
