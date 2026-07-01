using ChillZone.UI.Utils.Config;
using TMPro;
using UnityEngine;

namespace ChillZone.UI.ContentPicker.Config
{
    /// <summary>
    /// Appearance + behaviour for a code-generated content picker view: a full-width bottom
    /// sheet that opens to <see cref="openHeightFraction"/> of the screen and expands to fullscreen as the
    /// grid scrolls. The view builds its canvas / backdrop / sheet / scroll / grid from this config, so
    /// nothing needs wiring in the scene — only the registry + select/unlock hooks come from the subclass.
    /// </summary>
    [CreateAssetMenu(fileName = "ContentPickerConfig", menuName = "ChillZone/UI/Content Picker Config", order = 040)]
    public class ContentPickerConfig : ScriptableObject
    {
        [Header("Sheet")]
        [Tooltip("Backdrop colour behind the sheet (tap to close).")]
        public Color backdropColor = new(0f, 0f, 0f, 0.5f);
        [Tooltip("Sheet background colour.")]
        public Color sheetColor = new(0.08f, 0.08f, 0.1f, 0.98f);
        [Tooltip("Open height as a fraction of screen height; the sheet expands to fullscreen on scroll up."), Range(0.2f, 1f)]
        public float openHeightFraction = 0.5f;
        [Tooltip("Minimum sheet height (canvas units); below this the sheet opens fullscreen.")]
        public float minimumSheetHeight = 800f;
        [Tooltip("Open / close / expand animation duration (seconds).")]
        public float animDuration = 0.3f;
        [Tooltip("Padding inside the sheet around its content (canvas units).")]
        public RectOffset padding;

        [Header("Header")]
        [Tooltip("Header background colour.")]
        public Color headerColor = new(0f, 0f, 0f, 1f);
        [Tooltip("Header height (canvas units).")]
        public float headerHeight = 100f;
        [Tooltip("Title shown in the header. Empty = no title text.")]
        public string title = "Select";
        public TextConfig titleTextConfig;

        [Header("Grid")]
        [Tooltip("Number of item columns.")]
        public int columns = 3;
        [Tooltip("Item cell size (canvas units).")]
        public Vector2 cellSize = new(220f, 280f);
        [Tooltip("Spacing between cells (canvas units).")]
        public Vector2 spacing = new(50f, 50f);

        [Header("Item")]
        [Tooltip("Config that code-generates each content cell (icon, name, rarity outline, lock state).")]
        public ContentItem item;

        private void Reset()
        {
            padding = new RectOffset(50, 50, 50, 50);
            titleTextConfig = new TextConfig
            {
                fontSize = 42,
                enableAutoSizing = false,
                fontStyle = FontStyles.SmallCaps,
                color = Color.white,
                alignment = TextAlignmentOptions.Center,
                fontMaterial = TextConfig.DefaultFontMaterial(),
            };
        }
    }
}
