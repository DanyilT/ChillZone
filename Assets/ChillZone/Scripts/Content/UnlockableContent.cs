using UnityEngine;

namespace ChillZone.Content
{
    /// <summary>
    /// Base for any unlockable, selectable game content (balls, baskets, …). Holds the
    /// shared identity, presentation, and unlock rule so every content type flows through
    /// one pipeline: a registry, the content picker, and profile unlock tracking.
    /// </summary>
    public abstract class UnlockableContent : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Stable id used for saving/lookups. Keep it unique and never rename it once shipped.")]
        public string id; // use asset prefix (ball-, basket-) to avoid collisions
        [Tooltip("Display name shown in the content picker.")]
        public string displayName;
        [TextArea] public string description;
        public ContentRarity rarity;

        [Header("Presentation")]
        [Tooltip("Sprite shown in the content picker UI.")]
        public Sprite menuSprite;
        [Tooltip("Icon shown in the picker when this content is LOCKED. Falls back to the menu sprite if empty.")]
        public Sprite lockedSprite;
        [Tooltip("Accent color used in the content picker UI.")]
        public Color uiAccentColor = Color.white;

        [Header("Unlock"), Tooltip("Criteria that must be met to unlock this content. If null, the content is always unlocked.")]
        public UnlockCriteria unlockCriteria;

        /// <summary>Which content family this is (ball, basket, …). Lets generic systems — ContentManager, the profile, the registry — route by type without a concrete cast.</summary>
        public abstract ContentTypes ContentType { get; }

        /// <summary>Stable identifier for persistence and lookups; falls back to the display name, then the asset name.</summary>
        public string GetStableId()
        {
            return !string.IsNullOrWhiteSpace(id)
                ? id.Trim()
                : string.IsNullOrWhiteSpace(displayName)
                    ? name
                    : displayName.Trim();
        }

#if UNITY_EDITOR
        private void Reset() => id = GetStableId();
        private void OnValidate() => id = GetStableId().ToLowerInvariant().Replace(' ', '-').Replace('_', '-');
#endif
    }
}
