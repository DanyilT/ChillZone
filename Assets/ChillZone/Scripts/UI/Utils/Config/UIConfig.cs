using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ChillZone.UI.Utils.Config
{
    [Serializable]
    public struct TextConfig
    {
        [Tooltip("Font size for this text element. Used only if autoFontSize is false.")]
        public int fontSize;
        [Tooltip("Automatically adjust font size based on available space.")]
        public bool enableAutoSizing;
        [Tooltip("Minimum font size when autoFontSize is enabled.")]
        public int fontSizeMin;
        [Tooltip("Maximum font size when autoFontSize is enabled.")]
        public int fontSizeMax;
        [Tooltip("Font style (Normal, Bold, Italic, BoldAndItalic).")]
        public FontStyles fontStyle;
        [Tooltip("Text color.")]
        public Color color;
        [Tooltip("Text alignment.")]
        public TextAlignmentOptions alignment;
        [Tooltip("Text font material.")]
        public Material fontMaterial;

        public enum Preset
        {
            Title,
            Body,
            Button,
            ScoreText,
            Default
        }

        public static TextConfig Get(Preset preset) => preset switch
        {
            Preset.Title => new TextConfig
            {
                fontSize = 42,
                enableAutoSizing = true,
                fontSizeMin = 16,
                fontSizeMax = 60,
                fontStyle = FontStyles.SmallCaps,
                color = Color.white,
                alignment = TextAlignmentOptions.Center,
                fontMaterial = DefaultFontMaterial()
            },
            Preset.Body => new TextConfig
            {
                fontSize = 28,
                enableAutoSizing = true,
                fontSizeMin = 16,
                fontSizeMax = 48,
                fontStyle = FontStyles.Normal,
                color = new Color(0.85f, 0.85f, 0.85f, 1f),
                alignment = TextAlignmentOptions.TopLeft,
                fontMaterial = DefaultFontMaterial()
            },
            Preset.Button => new TextConfig
            {
                fontSize = 28,
                enableAutoSizing = true,
                fontSizeMin = 16,
                fontSizeMax = 48,
                fontStyle = FontStyles.Bold,
                color = Color.white,
                alignment = TextAlignmentOptions.Center,
                fontMaterial = DefaultFontMaterial()
            },
            Preset.ScoreText => new TextConfig
            {
                fontSize = 42,
                enableAutoSizing = true,
                fontSizeMin = 16,
                fontSizeMax = 72,
                fontStyle = FontStyles.Normal,
                color = Color.white,
                alignment = TextAlignmentOptions.Center,
                fontMaterial = DefaultDropShadowFontMaterial()
            },
            Preset.Default => new TextConfig
            {
                fontSize = 28,
                enableAutoSizing = true,
                fontSizeMin = 16,
                fontSizeMax = 48,
                fontStyle = FontStyles.Normal,
                color = Color.white,
                alignment = TextAlignmentOptions.Center,
                fontMaterial = DefaultFontMaterial()
            },
            _ => throw new ArgumentOutOfRangeException(nameof(preset), preset, null)
        };

        public static TextConfig Default() => Get(Preset.Default);
        public static TextConfig TitleTextDefault() => Get(Preset.Title);
        public static TextConfig BodyTextDefault() => Get(Preset.Body);
        public static TextConfig ButtonTextDefault() => Get(Preset.Button);
        public static TextConfig ScoreTextDefault() => Get(Preset.ScoreText);

        public static Material DefaultFontMaterial() => Resources.Load<Material>("Fonts & Materials/LiberationSans SDF");
        public static Material DefaultDropShadowFontMaterial() => Resources.Load<Material>("Fonts & Materials/LiberationSans SDF - Drop Shadow");
        public static Material DefaultOutlineFontMaterial() => Resources.Load<Material>("Fonts & Materials/LiberationSans SDF - Outline");
    }

    [Serializable]
    public struct TextEntry
    {
        [Tooltip("Text to display.")]
        public string text;
        [Tooltip("Text configuration. If null, uses TextConfig.BodyTextDefault().")]
        public TextConfig textConfig;
        [Tooltip("Padding when stretching the text to fill its parent.")]
        public RectOffset padding;

        public static TextEntry Default() => new()
        {
            text = string.Empty,
            textConfig = TextConfig.BodyTextDefault(),
            padding = new RectOffset(0, 0, 0, 0),
        };
    }

    [Serializable]
    public struct BackgroundImageConfig
    {
        [Tooltip("Image background color (tints the generated shape, or the sprite below).")]
        public Color color;
        [Tooltip("How the background is drawn. None = no sprite (colour only; alpha 0 = fully transparent). RoundedRect and Circle are generated in code (no sprite needed).")]
        public BackgroundShape imageShape;
        [Tooltip("Corner radius for rounded rectangle backgrounds. Ignored for Circle.")]
        public float cornerRadius;

        public static BackgroundImageConfig Default() => new()
        {
            color = Color.white,
            imageShape = BackgroundShape.RoundedRect,
            cornerRadius = 24f,
        };

        public static BackgroundImageConfig WindowDefault() => new()
        {
            color = Color.black,
            imageShape = BackgroundShape.RoundedRect,
            cornerRadius = 32f,
        };

        public static BackgroundImageConfig ButtonDefault() => new()
        {
            color = new Color(1f, 1f, 1f, 0.4f),
            imageShape = BackgroundShape.Circle,
            cornerRadius = 0f,
        };
    
        /// <summary>How a background is rendered. None = no sprite (the Image shows only its colour — set alpha 0 for fully transparent); RoundedRect/Circle are generated in code via <see cref="ChillZone.UI.Utils.UIShapeFactory"/>.</summary>
        public enum BackgroundShape { None, RoundedRect, Circle }
    }


    [Serializable]
    public struct BehaviourConfig
    {
        [Header("Behaviour")]
        [Tooltip("Whether the button should block raycasts.")]
        public bool blockRaycasts;

        public static BehaviourConfig Default() => new()
        {
            blockRaycasts = true,
        };
    }

    /// <summary>
    /// A single icon graphic (sprite + tint + fit). Reused anywhere a config needs one icon — buttons,
    /// content cells, etc. Render it with <see cref="UIRenderUtils.RenderIcon"/> /
    /// <see cref="UIRenderUtils.ApplyIcon"/>. For a row/list of icons see the window panel's own icon list.
    /// </summary>
    [Serializable]
    public struct IconConfig
    {
        [Header("Icon")]
        [Tooltip("Icon colour (tint).")]
        public Color iconColor;
        [Tooltip("Icon sprite to display.")]
        public Sprite iconSprite;
        [Tooltip("Image type for the icon sprite.")]
        public Image.Type iconSpriteType;
        [Tooltip("Preserve aspect ratio of the icon sprite.")]
        public bool iconPreserveAspect;
        [Tooltip("Padding when stretching the icon to fill its parent.")]
        public RectOffset iconPadding;

        public static IconConfig Default() => new()
        {
            iconColor = Color.white,
            iconSprite = null,
            iconSpriteType = Image.Type.Simple,
            iconPreserveAspect = true,
            iconPadding = new RectOffset(20, 20, 20, 20),
        };
    }
}
