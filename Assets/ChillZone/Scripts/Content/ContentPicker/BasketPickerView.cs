using System;
using System.Collections.Generic;
using ChillZone.Content.ContentPicker;
using ChillZone.Content;
using ChillZone.Player;
using UnityEngine;

namespace ChillZone.Basket
{
    /// <summary>
    /// Bottom-sheet picker for baskets. Supplies the basket list and the select/unlock
    /// behaviour to the reusable <see cref="ContentPickerView"/>. Selecting a basket sets
    /// which prefab the next placement uses.
    /// </summary>
    public class BasketPickerView : ContentPickerView
    {
        [Header("Basket Picker")]
        [SerializeField, Tooltip("Registry containing all available baskets (BasketRegistry).")]
        private UnlockableContentRegistry registry;

        protected override string TriggerObjectName => "BasketPickerTrigger";
        protected override string SheetObjectName => "BasketPickerSheet";
        protected override string HeaderObjectName => "BasketPickerHeader";

        protected override IReadOnlyList<UnlockableContent> GetContentItems() => registry ? registry.content : Array.Empty<UnlockableContent>();

        protected override bool IsUnlocked(UnlockableContent item, PlayerProfileData profile)
        {
            var criteriaUnlocked = item.unlockCriteria == null || item.unlockCriteria.IsUnlocked(profile);
            var id = item.GetStableId();

            if (criteriaUnlocked) PlayerProfileManager.Instance?.UnlockBasket(id);

            if (profile == null) return criteriaUnlocked;
            return profile.HasBasketUnlocked(id) || criteriaUnlocked;
        }

        protected override bool IsSelected(UnlockableContent item)
        {
            var selected = ContentManager.Instance ? ContentManager.Instance.GetSelected(ContentTypes.Basket) : null;
            if (selected != null)
                return string.Equals(selected.GetStableId(), item.GetStableId(), StringComparison.OrdinalIgnoreCase);

            var profile = PlayerProfileManager.Instance ? PlayerProfileManager.Instance.EnsureProfile() : null;
            return profile != null && string.Equals(profile.selectedBasketId, item.GetStableId(), StringComparison.OrdinalIgnoreCase);
        }

        protected override void OnContentSelected(UnlockableContent item)
        {
            if (item is not BasketData basket) return;

            var profile = PlayerProfileManager.Instance ? PlayerProfileManager.Instance.EnsureProfile() : null;
            var unlocked = basket.unlockCriteria == null || basket.unlockCriteria.IsUnlocked(profile) || (profile != null && profile.HasBasketUnlocked(basket.GetStableId()));
            if (!unlocked) return;

            if (PlayerProfileManager.Instance)
            {
                PlayerProfileManager.Instance.SelectBasket(basket.GetStableId());
                PlayerProfileManager.Instance.UnlockBasket(basket.GetStableId());
            }

            ContentManager.Instance?.Select(basket);
            if (BasketSpawnManager.Instance) BasketSpawnManager.Instance.ReplaceBasketWithSelected();

            Close();
        }
    }
}
