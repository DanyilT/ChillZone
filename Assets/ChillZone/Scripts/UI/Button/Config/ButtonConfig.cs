using System;
using ChillZone.UI.Utils.Config;
using UnityEngine;
using UnityEngine.UI;

namespace ChillZone.UI.Button1.Config
{
    [CreateAssetMenu(fileName = "ButtonConfig", menuName = "ChillZone/UI/Button Config", order = 001)]
    public class ButtonConfig : ScriptableObject
    {
        [Header("ID"), Tooltip("Unique ID for this button config.")]
        public string buttonId;

        public AppearanceConfig buttonAppearance;
        public IconConfig buttonIcon;
        public ToggleConfig buttonToggle;
        [Tooltip("Empty - no label.")]
        public TextEntry buttonLabel;
        public BehaviourConfig buttonBehaviour;
        public ButtonActionEntry actionEntry;

        private void Reset()
        {
            buttonId = name.ToLowerInvariant().Replace(" ", "-");
            buttonAppearance = AppearanceConfig.Default();
            buttonIcon = IconConfig.Default();
            buttonToggle = ToggleConfig.Default();
            buttonLabel = new TextEntry
            {
                text = string.Empty,
                textConfig = TextConfig.ButtonTextDefault(),
                padding = new RectOffset(10, 10, 10, 10),
            };
            buttonBehaviour = BehaviourConfig.Default();
            actionEntry = ButtonActionEntry.Default();
        }

#if UNITY_EDITOR
        private void OnValidate() { if (string.IsNullOrEmpty(buttonId)) buttonId = name.ToLowerInvariant().Replace(" ", "-"); }
#endif
    }

    [Serializable]
    public struct AppearanceConfig
    {
        [Header("Appearance")]
        [Tooltip("Button size in canvas units.")]
        public Vector2 size;
        [Tooltip("Button transition type on interaction.")]
        public Selectable.Transition transition;
        [Tooltip("Background image config for the button.")]
        public BackgroundImageConfig backgroundImage;
        [Tooltip("Dark-background variant. When enabled, the button's ColorTint interaction colours (normal, highlighted, pressed, selected, disabled) are inverted so the tint transition reads correctly on a dark surface. The background sprite, icon, and label colours are left unchanged. Default (off) = light.")]
        public bool darkBackground;

        internal static AppearanceConfig Default() => new()
        {
            size = new Vector2(100f, 100f),
            transition = Selectable.Transition.ColorTint,
            backgroundImage = BackgroundImageConfig.ButtonDefault(),
            darkBackground = true,
        };

        /// <summary>
        /// ColorTint interaction colours for the button. On the default light background this is Unity's
        /// standard white-based block; on a dark background every state colour (normal, highlighted,
        /// pressed, selected, disabled) is inverted so the same tint transition reads correctly on dark.
        /// </summary>
        public ColorBlock ColorTintBlock
        {
            get
            {
                var block = ColorBlock.defaultColorBlock;
                if (!darkBackground) return block;
                block.normalColor      = Invert(block.normalColor);
                block.highlightedColor = Invert(block.highlightedColor);
                block.pressedColor     = Invert(block.pressedColor);
                block.selectedColor    = Invert(block.selectedColor);
                block.disabledColor    = Invert(block.disabledColor);
                return block;
            }
        }

        // Inverts RGB, keeping alpha.
        private static Color Invert(Color c) => new(1f - c.r, 1f - c.g, 1f - c.b, c.a);
    }

    [Serializable]
    public struct ToggleConfig
    {
        [Header("Toggle (Optional)")]
        [Tooltip("When true, the button swaps between the ON icon (above) and the OFF icon below each time it is clicked. Leave off for single-event buttons.")]
        public bool isToggle;
        [Tooltip("Icon tint while the toggle is OFF.")]
        public Color iconColorOff;
        [Tooltip("Icon shown while the toggle is OFF. Only used when 'Is Toggle' is enabled; single-event buttons don't need an OFF icon.")]
        public Sprite iconSpriteOff;
        [Tooltip("Whether the toggle starts in the ON state.")]
        public bool toggleStartsOn;
        [Tooltip("When ON, clicking does NOT flip the icon — instead the icon is driven by external state via ButtonToggleVisual.SetState (e.g. the pause/play icon kept in sync with the game's pause state, so it also updates when resumed by tapping the overlay). Needs a controller to call SetState. Default OFF = normal flip-on-click.")]
        public bool externallyDriven;

        internal static ToggleConfig Default() => new()
        {
            isToggle = false,
            iconColorOff = Color.white,
            iconSpriteOff = null,
            toggleStartsOn = true,
            externallyDriven = false,
        };
    }

    [Serializable]
    public struct ButtonActionEntry
    {
        [Header("Action")]
        [Tooltip("Action type to execute on click.")]
        public ButtonActionType actionType;
        [Tooltip("Scene to load on click. (if actionType is LoadScene)")]
        public string sceneName;
        [Tooltip("Scene index to load on click. (if actionType is LoadScene)")]
        public int sceneIndex;
        [Tooltip("URL to open on click. (if actionType is OpenURL)")]
        public string url;

        internal static ButtonActionEntry Default() => new()
        {
            actionType = ButtonActionType.None,
            sceneName = string.Empty,
            sceneIndex = -1,
            url = string.Empty,
        };
    }

    public enum ButtonActionType
    {
        None,
        TogglePause,
        ResetBall,
        ResetScanning,
        OpenURL,
        LoadScene,
        LoadNextScene,
        LoadPreviousScene,
        ReloadCurrentScene,
        Quit,
    }
}
