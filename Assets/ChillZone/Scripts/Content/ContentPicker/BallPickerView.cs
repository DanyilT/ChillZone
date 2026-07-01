using System;
using System.Collections.Generic;
using ChillZone.Ball;
using ChillZone.Player;
using UnityEngine;

namespace ChillZone.Content.ContentPicker
{
    /// <summary>
    /// Bottom-sheet picker for balls. Supplies the ball list and the select/unlock
    /// behaviour to the reusable <see cref="ContentPickerView"/>. Selecting a ball sets
    /// which prefab the next spawn manager uses.
    /// </summary>
    public class BallPickerView : ContentPickerView
    {
        [Header("Ball Picker")]
        [SerializeField, Tooltip("Registry containing all available balls (BallRegistry).")]
        private UnlockableContentRegistry registry;

        protected override string TriggerObjectName => "BallPickerTrigger";
        protected override string SheetObjectName => "BallPickerSheet";
        protected override string HeaderObjectName => "BallPickerHeader";

        protected override IReadOnlyList<UnlockableContent> GetContentItems() => registry ? registry.content : Array.Empty<UnlockableContent>();

        protected override bool IsUnlocked(UnlockableContent item, PlayerProfileData profile)
        {
            var criteriaUnlocked = item.unlockCriteria == null || item.unlockCriteria.IsUnlocked(profile);
            var id = item.GetStableId();

            // Persist a criteria-met unlock so it sticks even if the criteria later changes.
            if (criteriaUnlocked) PlayerProfileManager.Instance?.UnlockBall(id);

            if (profile == null) return criteriaUnlocked;
            return profile.HasBallUnlocked(id) || criteriaUnlocked;
        }

        protected override bool IsSelected(UnlockableContent item)
        {
            var selected = ContentManager.Instance ? ContentManager.Instance.GetSelected(ContentTypes.Ball) : null;
            if (selected != null)
                return string.Equals(selected.GetStableId(), item.GetStableId(), StringComparison.OrdinalIgnoreCase);

            var profile = PlayerProfileManager.Instance ? PlayerProfileManager.Instance.EnsureProfile() : null;
            return profile != null && string.Equals(profile.selectedBallId, item.GetStableId(), StringComparison.OrdinalIgnoreCase);
        }

        protected override void OnContentSelected(UnlockableContent item)
        {
            if (item is not BallData ball) return;

            var profile = PlayerProfileManager.Instance ? PlayerProfileManager.Instance.EnsureProfile() : null;
            var unlocked = ball.unlockCriteria == null || ball.unlockCriteria.IsUnlocked(profile) || (profile != null && profile.HasBallUnlocked(ball.GetStableId()));
            if (!unlocked) return;

            if (PlayerProfileManager.Instance)
            {
                PlayerProfileManager.Instance.SelectBall(ball.GetStableId());
                PlayerProfileManager.Instance.UnlockBall(ball.GetStableId());
            }

            ContentManager.Instance?.Select(ball);
            if (BallSpawnManager.Instance) BallSpawnManager.Instance.ResetActiveBall();

            Close();
        }
    }
}
