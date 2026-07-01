using System;
using ChillZone.UI.Utils.Config;
using UnityEngine;

namespace ChillZone.UI.Window.Config
{
    [CreateAssetMenu(fileName = "WindowConfig", menuName = "ChillZone/UI/Window Config", order = 021)]
    public class WindowConfig : ScriptableObject
    {
        [Header("ID"), Tooltip("Unique ID used to show/close this window by name.")]
        public string windowId;
        [Header("Global Config (Optional)"), Tooltip("Optional global config for shared defaults. If null, uses built-in defaults.")]
        public WindowGlobalConfig globalConfig;

        [Header("Backdrop")] public BackdropConfig backdropConfig;
        [Header("Header")] public HeaderConfig headerConfig;
        [Header("Panel")] public PanelConfig panelConfig;

        [Header("Animation"), Tooltip("Optional animation played when this window shows and hides. Requires DOTween. Leave null for instant.")]
        public WindowAnimationConfig animationConfig;
        [Header("Behaviour"), Tooltip("While this window is open, disable every OTHER (non-window) Canvas in the scene so only this window — over the camera — is visible (restored on close). Order-independent; intended for a full-screen takeover such as the welcome window.")]
        public bool hideOtherUI;

        /// <summary>
        /// Sort order for this window's canvas. Default 100 keeps the regular flow windows (welcome/scan/place/manual)
        /// below the HUD, as intended. Subclasses override it to sit higher — e.g. the best-score window renders above
        /// the pause overlay (sort 1000) so it's visible when opened while paused.
        /// </summary>
        public virtual int CanvasSortingOrder => 100;

        protected virtual void Reset()
        {
            backdropConfig = BackdropConfig.Default();
            headerConfig = HeaderConfig.Default();
            panelConfig = PanelConfig.Default();
        }

#if UNITY_EDITOR
        protected void OnValidate() { if (string.IsNullOrEmpty(windowId)) windowId = name.ToLowerInvariant().Replace(" ", "-"); }
#endif
    }

    [Serializable]
    public struct BackdropConfig
    {
        [Header("Appearance")]
        [Tooltip("Backdrop color.")]
        public Color color;

        [Header("Behaviour")]
        [Tooltip("Whether backdrop should block raycasts to elements behind.")]
        public bool blockRaycasts;
        [Tooltip("Tapping the backdrop closes this window (overridden by WindowShowOptions.onBackdropClick).")]
        public bool closeOnBackdropClick;

        public static BackdropConfig Default() => new ()
        {
            color = new Color(0f, 0f, 0f, 0.5f),
            blockRaycasts = true,
            closeOnBackdropClick = true
        };
    }

    [Serializable]
    public struct PanelConfig
    {
        [Header("Appearance")]
        [Tooltip("Panel background alpha. 0 = transparent, 1 = opaque."), Range(0f, 1f)]
        public float backgroundAlpha;
        [Tooltip("Panel appearance configuration. If null, uses BackgroundImageConfig.WindowDefault().")]
        public BackgroundImageConfig panelAppearanceConfig;

        [Space(10), Header("Content")]
        [Header("Text Configuration")]
        [Tooltip("Text to display in the panel body."), TextArea(2, 6)]
        public string bodyText;
        [Tooltip("Text configuration. If null, uses TextConfig.BodyTextDefault().")]
        public TextConfig bodyTextConfig;
        [Tooltip("Optional list of icons to display in the panel body.")]
        public IconRowConfig iconConfig;

        [Space(10), Header("Size")]
        [Tooltip("Fraction of canvas width used when preferredWidth is 0."), Range(0.4f, 1f)]
        public float screenWidthFraction;
        [Tooltip("Fixed canvas-unit width. 0 = derive from screenWidthFraction.")]
        public float preferredWidth;
        [Tooltip("Fixed canvas-unit height. 0 = shrink-to-fit content.")]
        public float preferredHeight;
        [Tooltip("Minimum canvas-unit width and height.")]
        public float minWidth;
        [Tooltip("Minimum canvas-unit height.")]
        public float minHeight;

        [Header("Behaviour")]
        [Tooltip("Whether the panel should block raycasts to elements behind.")]
        public bool blockRaycasts;
        [Tooltip("Tapping the window panel itself closes this window (overridden by WindowShowOptions.onPanelClick).")]
        public bool closeOnPanelClick;

        public static PanelConfig Default() => new ()
        {
            panelAppearanceConfig = BackgroundImageConfig.WindowDefault(),
            backgroundAlpha = 0.9f,

            bodyText = "",
            bodyTextConfig = TextConfig.BodyTextDefault(),
            iconConfig = IconRowConfig.Default(),

            screenWidthFraction = 0.8f,
            preferredWidth = 0f,
            preferredHeight = 0f,
            minWidth = 320f,
            minHeight = 140f,

            blockRaycasts = true,
            closeOnPanelClick = false
        };
    }

    [Serializable]
    public struct HeaderConfig
    {
        [Header("Appearance")]
        [Tooltip("Header appearance configuration. If null, uses BackgroundImageConfig.WindowDefault().")]
        public BackgroundImageConfig headerAppearanceConfig;

        [Header("Text Configuration")]
        [Tooltip("Text to display in the header. If empty, no header is rendered.")]
        public string titleText;
        [Tooltip("Text configuration. If null, uses TextConfig.TitleTextDefault().")]
        public TextConfig titleTextConfig;

        [Header("Behaviour")]
        [Tooltip("Whether the panel should block raycasts to elements behind.")]
        public bool blockRaycasts;
        [Tooltip("Tapping the header closes this window (only used when the header is rendered).")]
        public bool closeOnHeaderClick;

        public static HeaderConfig Default() => new ()
        {
            headerAppearanceConfig = BackgroundImageConfig.WindowDefault(),
            titleText = "",
            titleTextConfig = TextConfig.TitleTextDefault(),
            blockRaycasts = true,
            closeOnHeaderClick = true
        };
    }

    /// <summary>A row/list of icons rendered in the panel body. Distinct from the shared single-icon
    /// <see cref="IconConfig"/> — this one holds many <see cref="IconEntry"/> items.</summary>
    [Serializable]
    public struct IconRowConfig
    {
        [Header("Icon Configuration")]
        [Tooltip("List of icons to display in the panel body.")]
        public IconEntry[] icons;
        [Tooltip("Layout direction for the icons list.")]
        public RectTransform.Axis iconsLayout;
        [Tooltip("Fallback icon size when an IconEntry.sizeOverride is 0.")]
        public float iconSize;
        [Tooltip("Default spacing between icons.")]
        public float iconSpacing;
        [Tooltip("Default icon color (tint) for all icons.")]
        public Color iconColor;

        public static IconRowConfig Default() => new ()
        {
            icons = new IconEntry[]{},
            iconsLayout = RectTransform.Axis.Vertical,
            iconSize = 100f,
            iconSpacing = 20f,
            iconColor = Color.white
        };

        [Serializable]
        public struct IconEntry
        {
            [Tooltip("Icon sprite.")]
            public Sprite sprite;
            [Tooltip("Per-icon size override. 0 = use WindowConfig.defaultIconSize.")]
            public float sizeOverride;
            [Tooltip("Extra gap inserted after this icon (between it and the next one).")]
            public float spacingAfter;
        }
    }
}
