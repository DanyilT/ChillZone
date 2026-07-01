using ChillZone.UI.Utils;
using UnityEngine;

namespace ChillZone.UI.Window.Config
{
    [CreateAssetMenu(fileName = "WindowGlobalConfig", menuName = "ChillZone/UI/Window Global Config", order = 020)]
    public class WindowGlobalConfig : ScriptableObject
    {
        [Header("Canvas Reference"), Tooltip("Default Canvas preset for all windows.")]
        public CanvasUtils.Preset canvasPreset = CanvasUtils.Preset.Overlay;

        [Header("Dimensions"), Tooltip("Header height in canvas units.")]
        public float headerHeight = 100f;

        [Header("Padding (Left, Right, Top, Bottom)")]
        [Tooltip("Panel content padding.")]
        public RectOffset panelPadding;
        [Tooltip("Window header text padding.")]
        public RectOffset headerPadding;
        [Tooltip("Body text padding.")]
        public RectOffset bodyPadding;
        [Tooltip("Icon row padding.")]
        public RectOffset iconRowPadding;

        public static WindowGlobalConfig Instance { get; private set; }

        private void Reset()
        {
            panelPadding = new RectOffset(0, 0, 0, 0);
            headerPadding = new RectOffset(70, 70, 25, 25);
            bodyPadding = new RectOffset(70, 70, 50, 50);
            iconRowPadding = new RectOffset(70, 70, 50, 50);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void AutoLoad()
        {
            if (Instance != null) return;
            var configs = Resources.LoadAll<WindowGlobalConfig>("");
            Instance = configs.Length > 0 ? configs[0] : CreateInstance<WindowGlobalConfig>();
        }
    }
}
