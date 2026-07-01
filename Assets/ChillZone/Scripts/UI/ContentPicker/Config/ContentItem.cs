using System;
using System.Collections.Generic;
using ChillZone.Content;
using ChillZone.Core;
using ChillZone.UI.Utils.Config;
using ChillZone.UI.Window.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ChillZone.UI.ContentPicker.Config
{
    /// <summary>
    /// Config for ONE content cell in the picker. Code-generates the cell (icon + name) from a piece of
    /// <see cref="UnlockableContent"/> and its unlocked/selected state: a rarity-coloured outline on the
    /// icon and the name, the locked icon + unlock requirement when locked (Code unlocks read "Special"),
    /// and a selected highlight. Backgrounds/icons/text are drawn through the shared <see cref="UIRenderUtils"/>.
    /// No prefab needed.
    /// </summary>
    [CreateAssetMenu(fileName = "ContentItem", menuName = "ChillZone/UI/Content Item", order = 041)]
    public class ContentItem : ScriptableObject
    {
        [Header("Cell")]
        [Tooltip("Cell background (shape + colour). Reuses the shared background config.")]
        public BackgroundImageConfig cellBackground;
        [Tooltip("Background colour override while this cell is the selected one.")]
        public Color selectedColor = new(1f, 1f, 1f, 0.20f);
        public RectOffset padding;
        public float spacing = 20f;

        [Header("Icon")]
        public float iconSize = 150f;
        [Tooltip("Shiny rarity shader put on the icon image (ChillZone/UI/RarityShine). Its _RarityColor is set per rarity tier. If empty, falls back to a flat rarity Outline.")]
        public Shader iconShineShader;
        [Tooltip("Pulse strength applied to EPIC and LEGENDARY item icons only (0 = no pulse)."), Range(0f, 1f)]
        public float iconPulseIntensity = 0.35f;
        [Tooltip("Fallback rarity outline distance on the icon (UI Outline effect) — used only when no Icon Shine Shader is assigned.")]
        public float iconOutlineDistance = 4f;
        [Tooltip("Default locked icon when the content has no lockedSprite of its own.")]
        public Sprite defaultLockedIcon;

        [Header("Name")]
        public TextConfig nameTextConfig;
        [Tooltip("Rarity outline width on the name text (0..1)."), Range(0f, 1f)]
        public float nameOutlineWidth = 0.2f;

        private void Reset()
        {
            cellBackground = new BackgroundImageConfig
            {
                color = new Color(1f, 1f, 1f, 0.01f),
                imageShape = BackgroundImageConfig.BackgroundShape.RoundedRect,
                cornerRadius = 24f,
            };
            padding = new RectOffset(25, 25, 25, 20);
            nameTextConfig = TextConfig.ButtonTextDefault();
        }

        /// <summary>Code-generates one cell under <paramref name="parent"/> and returns it.</summary>
        public GameObject Build(Transform parent, UnlockableContent data, bool unlocked, bool selected, Action<UnlockableContent> onSelect)
        {
            var rarity = RarityColors.Of(data.rarity);

            var cell = RenderUtils.CreateChild(parent, data.GetStableId(), typeof(RectTransform), typeof(Image), typeof(Button), typeof(VerticalLayoutGroup));
            var bg = cell.GetComponent<Image>();
            UIRenderUtils.ApplyBackground(bg, cellBackground);
            if (selected) bg.color = selectedColor;

            var vlg = cell.GetComponent<VerticalLayoutGroup>();
            vlg.childAlignment = TextAnchor.MiddleCenter;
            vlg.childControlWidth = vlg.childControlHeight = true;
            vlg.childForceExpandWidth = vlg.childForceExpandHeight = false;
            vlg.padding = padding ?? new RectOffset();
            vlg.spacing = spacing;

            // Icon: the locked icon when locked (own lockedSprite → config default → menu sprite), rarity outline.
            var iconSprite = !unlocked ? data.lockedSprite ? data.lockedSprite : defaultLockedIcon ? defaultLockedIcon : data.menuSprite : data.menuSprite;
            var iconConfig = new IconConfig
            {
                iconColor = Color.white,
                iconSprite = iconSprite,
                iconSpriteType = Image.Type.Simple,
                iconPreserveAspect = true,
            };
            var icon = UIRenderUtils.RenderIcon(cell.transform, iconConfig);
            ApplyIconRarity(icon.GetComponent<Image>(), data.rarity, rarity);
            // Pin the icon to a fixed square: MIN (not just preferred) size + no flex, so the layout can never
            // shrink it to make room for a long name. Every cell's icon is then exactly iconSize (clean grid);
            // the name absorbs the leftover space and truncates (see below) instead of stealing the icon's.
            var iconLe = icon.AddComponent<LayoutElement>();
            iconLe.minWidth = iconLe.preferredWidth = iconSize;
            iconLe.minHeight = iconLe.preferredHeight = iconSize;
            iconLe.flexibleWidth = iconLe.flexibleHeight = 0f;

            // Name: the content's accent colour as the fill (falls back to the config default when the content has
            // no accent, i.e. white) plus a rarity-coloured outline. When locked, show the unlock requirement.
            var nameText = unlocked || data.unlockCriteria == null ? data.displayName : data.unlockCriteria.Describe();
            var tmp = UIRenderUtils.RenderText(cell.transform, nameText, nameTextConfig, "Name").GetComponent<TextMeshProUGUI>();
            tmp.color = Color.white;
            // Take only the space left by the fixed icon and truncate a too-long name with an ellipsis, rather
            // than growing and pushing the icon smaller. (min height is 0, so the layout shrinks the name first.)
            tmp.overflowMode = TextOverflowModes.Ellipsis;
            // Rarity outline. The default font material has the outline feature OFF (width/colour render nothing),
            // so assign an outline-enabled preset first. TMP also re-applies its outlineColor / outlineWidth
            // PROPERTIES onto the material each rebuild, so a direct material write alone gets reset — set both ways.
            tmp.fontSharedMaterial = TextConfig.DefaultOutlineFontMaterial();
            var nameMaterial = tmp.fontMaterial;
            nameMaterial.SetColor(ShaderUtilities.ID_OutlineColor, rarity);
            nameMaterial.SetFloat(ShaderUtilities.ID_OutlineWidth, nameOutlineWidth);
            tmp.outlineColor = rarity;
            tmp.outlineWidth = nameOutlineWidth;
            tmp.UpdateMeshPadding();

            var button = cell.GetComponent<Button>();
            button.targetGraphic = bg;
            button.interactable = unlocked;
            if (onSelect != null) button.onClick.AddListener(() => onSelect(data));
            if (unlocked) button.onClick.AddListener(() => AudioService.PlayUi(UiSound.PickItem));   // picking an unlocked item

            return cell;
        }

        // Rarity shine material per rarity tier (shared across cells of the same rarity; built from iconShineShader).
        private readonly Dictionary<ContentRarity, Material> _shineMaterials = new();
        private static readonly int RarityColorId = Shader.PropertyToID("_RarityColor");
        private static readonly int PulseIntensityId = Shader.PropertyToID("_PulseIntensity");

        // Puts the shiny rarity shader on the icon (its _RarityColor set per rarity tier). Falls back to a flat
        // rarity Outline when no Icon Shine Shader is assigned, so icons still read their rarity without it wired.
        private void ApplyIconRarity(Image iconImage, ContentRarity rarityTier, Color rarityColor)
        {
            if (!iconImage) return;

            var material = GetShineMaterial(rarityTier, rarityColor);
            if (material)
            {
                iconImage.material = material;
                return;
            }

            var outline = iconImage.gameObject.AddComponent<Outline>();
            outline.effectColor = rarityColor;
            outline.effectDistance = new Vector2(iconOutlineDistance, iconOutlineDistance);
        }

        private Material GetShineMaterial(ContentRarity rarityTier, Color rarityColor)
        {
            if (!iconShineShader) return null;
            if (_shineMaterials.TryGetValue(rarityTier, out var cached) && cached) return cached;

            var material = new Material(iconShineShader) { name = $"RarityShine_{rarityTier}" };
            material.SetColor(RarityColorId, rarityColor);
            // Pulse only the high tiers; common/rare stay static.
            material.SetFloat(PulseIntensityId, rarityTier is ContentRarity.Epic or ContentRarity.Legendary ? iconPulseIntensity : 0f);
            _shineMaterials[rarityTier] = material;
            return material;
        }
    }
}
