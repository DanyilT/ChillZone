using UnityEngine;

namespace ChillZone.UI.Header.Config
{
    [CreateAssetMenu(fileName = "AdaptiveHeaderConfig", menuName = "ChillZone/UI/Adaptive Header Config", order = 000)]
    public class AdaptiveHeaderConfig : ScriptableObject
    {
        [Header("Size")]
        [Tooltip("When true, header height is derived from the device status bar / safe area.")]
        public bool useStatusBarHeight = true;
        [Tooltip("Header height when status-bar detection is disabled.")]
        public float fixedHeight = 100f;
        [Tooltip("Minimum height — the detected status-bar height is clamped to at least this value.")]
        public float minHeight = 80f;

        [Header("Layout")]
        [Tooltip("Fraction of canvas width given to the left panel (score / credits). Right panel takes the rest."), Range(0.2f, 0.6f)]
        public float leftPanelRatio = 0.6f;
        [Tooltip("[Adaptive] Left-panel width fraction used WHEN a cutout is detected. Lets you rebalance the row around the notch; the right panel takes the remaining non-cutout width."), Range(0.1f, 0.9f)]
        public float cutoutLeftPanelRatio = 0.6f;
        [Tooltip("Horizontal spacing between the two panels in header in canvas units.")]
        public float headerSpacing = 20f;
        [Tooltip("Header panel padding (left, right, top, bottom) in canvas units.")]
        public RectOffset headerPadding;

        [Header("Behaviour")]
        [Tooltip("When true, the header is inset into the device safe area (pushed below the status bar / notch). When false it spans the full screen and overlaps the status-bar row.")]
        public bool useSafeArea = false;
        [Tooltip("When the header overlaps the status-bar row, treat the camera / notch as a fixed-width element in the row so the panels flex around it. If false, left panel is always left and right panel always right.")]
        public bool useAdaptiveHeader = true;
        [Tooltip("Camera-avoidance ordering. True → the element on the camera side moves to Center. False → the two elements swap sides.")]
        public bool useCollapse = true;

        private void Reset()
        {
            headerPadding = new RectOffset(20, 20, 10, 10);
        }
    }
}
