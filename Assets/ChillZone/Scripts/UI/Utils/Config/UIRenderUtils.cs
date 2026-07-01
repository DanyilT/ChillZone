using ChillZone.UI.Window.Utils;
using UnityEngine;
using UnityEngine.UI;

namespace ChillZone.UI.Utils.Config
{
    /// <summary>
    /// Global renderer for the reusable UI config elements (<see cref="TextConfig"/>,
    /// <see cref="BackgroundImageConfig"/>, <see cref="IconConfig"/>, <see cref="BehaviourConfig"/>).
    /// Any config — buttons, windows, the content picker — can render its shared sub-configs through here
    /// instead of duplicating the wiring. Low-level GameObject/component creation is delegated to
    /// <see cref="RenderUtils"/>; this class only knows how to turn a config struct into the right visual.
    /// </summary>
    public static class UIRenderUtils
    {
        #region text

        /// <summary>Create a TMP text element styled by <paramref name="config"/>.</summary>
        public static GameObject RenderText(Transform parent, string text, TextConfig config, string name = "Text")
            => RenderUtils.CreateText(parent, text, config, name);

        #endregion

        #region background

        /// <summary>
        /// The sprite + image type for a background shape. RoundedRect/Circle are generated in code (no asset);
        /// None returns a null sprite so the Image draws nothing but its colour (no sprite is created).
        /// </summary>
        public static (Sprite sprite, Image.Type type) BackgroundVisual(BackgroundImageConfig config) => config.imageShape switch
        {
            BackgroundImageConfig.BackgroundShape.RoundedRect => (UIShapeFactory.RoundedRect(config.cornerRadius), Image.Type.Sliced),
            BackgroundImageConfig.BackgroundShape.Circle      => (UIShapeFactory.Circle(), Image.Type.Simple),
            _                                                 => (null, Image.Type.Simple),   // None — no generated sprite
        };

        /// <summary>Apply a <see cref="BackgroundImageConfig"/> (colour + generated shape) to an existing image.</summary>
        public static void ApplyBackground(Image image, BackgroundImageConfig config, bool raycastTarget = true)
        {
            var (sprite, type) = BackgroundVisual(config);
            RenderUtils.SetupImage(image, config.color, sprite, type, raycastTarget, preserveAspect: false);
        }

        /// <summary>Create a background image child styled by <paramref name="config"/>.</summary>
        public static GameObject RenderBackground(Transform parent, BackgroundImageConfig config, bool raycastTarget = true, string name = "Background")
        {
            var go = RenderUtils.CreateImage(parent, config.color, null, Image.Type.Sliced, raycastTarget, preserveAspect: false, name);
            ApplyBackground(go.GetComponent<Image>(), config, raycastTarget);
            return go;
        }

        #endregion

        #region icon

        /// <summary>Apply an <see cref="IconConfig"/> (sprite + tint + fit) to an existing image, keeping its raycast flag.</summary>
        public static void ApplyIcon(Image image, IconConfig config)
            => RenderUtils.SetupImage(image, config.iconColor, config.iconSprite, config.iconSpriteType, image.raycastTarget, config.iconPreserveAspect);

        /// <summary>Create an icon image styled by <paramref name="config"/>.</summary>
        public static GameObject RenderIcon(Transform parent, IconConfig config, bool raycastTarget = false, string name = "Icon")
            => RenderUtils.CreateImage(parent, config.iconColor, config.iconSprite, config.iconSpriteType, raycastTarget, config.iconPreserveAspect, name);

        #endregion

        #region behaviour

        /// <summary>Apply a <see cref="BehaviourConfig"/> (raycast blocking) to a graphic.</summary>
        public static void ApplyBehaviour(Graphic graphic, BehaviourConfig config)
        {
            if (graphic) graphic.raycastTarget = config.blockRaycasts;
        }

        #endregion

        /// <summary>
        /// Get pivot/anchor point based on the corner (0-1 normalized)
        /// </summary>
        public static Vector2 GetPivot(TextAnchor corner) => corner switch
        {
            TextAnchor.UpperLeft => new Vector2(0, 1),
            TextAnchor.UpperCenter => new Vector2(0.5f, 1),
            TextAnchor.UpperRight => new Vector2(1, 1),
            TextAnchor.MiddleLeft => new Vector2(0, 0.5f),
            TextAnchor.MiddleCenter => new Vector2(0.5f, 0.5f),
            TextAnchor.MiddleRight => new Vector2(1, 0.5f),
            TextAnchor.LowerLeft => new Vector2(0, 0),
            TextAnchor.LowerCenter => new Vector2(0.5f, 0),
            TextAnchor.LowerRight => new Vector2(1, 0),
            _ => new Vector2(0.5f, 0.5f)
        };
    }
}
