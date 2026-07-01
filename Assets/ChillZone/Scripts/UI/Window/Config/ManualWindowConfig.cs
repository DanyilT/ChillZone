using System;
using ChillZone.UI.Utils.Config;
using TMPro;
using UnityEngine;
using UnityEngine.TextCore.Text;
using FontStyles = TMPro.FontStyles;

namespace ChillZone.UI.Window.Config
{
    /// <summary>
    /// A <see cref="WindowConfig"/> for the manual/help modal: a vertical list of rows, each a
    /// text block (title + description) beside an icon, plus a summary pinned to the bottom. The
    /// panel grows to fit the content up to <see cref="ManualConfig.maxScreenHeightFraction"/> of the screen,
    /// then scrolls elastically. Built by WindowObject's manual branch; backdrop / panel background
    /// / animation come from the base.
    /// </summary>
    [CreateAssetMenu(fileName = "ManualWindowConfig", menuName = "ChillZone/UI/Manual Window Config", order = 022)]
    public class ManualWindowConfig : WindowConfig
    {
        public ManualConfig manualConfig;

        protected override void Reset()
        {
            base.Reset();
            windowId = "manual";
            manualConfig = ManualConfig.Default();
        }
    }

    [Serializable]
    public struct ManualConfig
    {
        [Header("Manual — Rows (icon left, text right)")]
        public Row[] rows;
        [Tooltip("Gap between the rows in the scroll content (0 = default 65).")]
        public float rowSpacing;

        [Header("Manual — Shared Row Styling")]
        public TextConfig titleConfig;
        public TextConfig descriptionConfig;

        [Header("Manual — Summary (pinned bottom)"), TextArea(1, 3)]
        public string summary;
        public TextConfig summaryConfig;
        [Tooltip("Gap between the scroll area and the summary below it (0 = default 60).")]
        public float summarySpacing;

        [Header("Manual — Sizing"), Tooltip("Max panel height as a fraction of screen height before the content scrolls."), Range(0.3f, 1f)]
        public float maxScreenHeightFraction;

        public static ManualConfig Default() => new ()
        {
            rows = new[] { Row.Default() },
            rowSpacing = 65f,
            titleConfig = TextConfig.BodyTextDefault(),
            descriptionConfig = new TextConfig
            {
                fontSize = 32,
                enableAutoSizing = false,
                fontStyle = FontStyles.Italic,
                color = new Color(0.85f, 0.85f, 0.85f, 1f),
                alignment = TextAlignmentOptions.TopLeft,
            },
            summary = "",
            summaryConfig = new TextConfig
            {
                fontSize = 32,
                enableAutoSizing = false,
                fontStyle = FontStyles.Normal,
                color = new Color(0.85f, 0.85f, 0.85f, 1f),
                alignment = TextAlignmentOptions.Center,
            },
            summarySpacing = 60f,
            maxScreenHeightFraction = 0.8f
        };

        [Serializable]
        public struct Row
        {
            [Tooltip("Icons shown beside the text — the SAME config as the window panel's icon list. Add multiple images and set their layout / size / spacing / colour.")]
            public IconRowConfig iconConfig;
            [Tooltip("Main text in a row.")]
            public string title;
            [Tooltip("Description text shown below the title."), TextArea(1, 3)]
            public string description;

            public static Row Default() => new ()
            {
                iconConfig = new IconRowConfig
                {
                    icons = new IconRowConfig.IconEntry[]{},
                    iconsLayout = RectTransform.Axis.Horizontal,
                    iconSize = 120f,
                    iconSpacing = 0f,
                    iconColor = Color.white
                },
                title = string.Empty,
                description = string.Empty
            };
        }
    }
}
