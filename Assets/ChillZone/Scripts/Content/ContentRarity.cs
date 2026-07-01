using UnityEngine;

namespace ChillZone.Content
{
    /// <summary>
    /// Shared rarity tier for any unlockable content (balls, baskets, …).
    /// Drives accent colouring in the content picker UI.
    /// </summary>
    public enum ContentRarity
    {
        Common,
        Uncommon,
        Rare,
        Epic,
        Legendary
    }

    /// <summary>Default rarity → colour mapping, used for content icon outlines and name-text outlines.</summary>
    public static class RarityColors
    {
        public static Color Of(ContentRarity rarity) => rarity switch
        {
            ContentRarity.Common    => new Color(0.7f, 0.7f, 0.7f),
            ContentRarity.Uncommon  => new Color(0.3f, 0.8f, 0.3f),
            ContentRarity.Rare      => new Color(0.3f, 0.5f, 1.0f),
            ContentRarity.Epic      => new Color(0.7f, 0.2f, 1.0f),
            ContentRarity.Legendary => new Color(1.0f, 0.7f, 0.1f),
            _                       => Color.white
        };
    }
}
