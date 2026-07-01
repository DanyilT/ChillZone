#if UNITY_EDITOR
using System.Collections.Generic;
using ChillZone.UI.Button1;
using ChillZone.UI.Button1.Config;
using UnityEngine;

namespace ChillZone.UI.Preview
{
    /// <summary>
    /// Edit-mode preview for ButtonManager layouts. Choose a mode in the Inspector:
    ///
    ///   ButtonManager    — previews all button groups from the source ButtonManager
    ///   ButtonGroup      — previews one named group (set groupName)
    ///   SingleButton     — previews one button found by ID (set buttonId)
    ///
    /// Canvas and CanvasScaler settings are copied from the source, so the preview
    /// matches the exact layout the source would produce at runtime.
    /// </summary>
    [ExecuteAlways]
    public class ButtonPreview : RenderPreview
    {
        private enum PreviewMode { ButtonManager, ButtonGroup, SingleButton }

        [SerializeField, Tooltip("The ButtonManager whose config (groups, margins, canvas settings) will be used.")]
        private ButtonManager source;

        [SerializeField, Header("Preview Mode"), Tooltip("What to preview from the source ButtonManager.")]
        private PreviewMode mode = PreviewMode.ButtonManager;

        [SerializeField, Header("Group mode"), Tooltip("Name of the group to isolate. Must match ButtonGroup.groupName exactly.")]
        private string groupName;

        [Header("Single Button mode")]
        [SerializeField, Tooltip("buttonId of the button to preview. Searched across all groups in the source.")]
        private string buttonId;
        [SerializeField, Tooltip("Corner at which to place the single button.")]
        private TextAnchor singleCorner = TextAnchor.MiddleCenter;

        protected override string PreviewName => $"_ButtonPreview_{mode}";

        protected override void Rebuild()
        {
            base.Rebuild();

            if (source == null) return;
            switch (mode)
            {
                case PreviewMode.ButtonManager: BuildFull(); break;
                case PreviewMode.ButtonGroup: BuildGroup(); break;
                case PreviewMode.SingleButton: BuildSingle(); break;
            }
        }

        #region builders

        private void BuildFull()
        {
            var preview = PreviewRoot.AddComponent<ButtonManager>();
            preview.ApplyPreviewConfig(source);
            preview.Refresh();
        }

        private void BuildGroup()
        {
            if (string.IsNullOrEmpty(groupName)) return;

            var group = source.GetGroupByName(groupName);
            if (group == null)
            {
                Debug.LogWarning($"[ButtonPreview] Group '{groupName}' not found in '{source.name}'.", this);
                return;
            }

            var preview = PreviewRoot.AddComponent<ButtonManager>();
            preview.ApplyPreviewConfig(source);
            preview.SetPreviewGroups(new List<ButtonManager.ButtonGroup> { group });
            preview.Refresh();
        }

        private void BuildSingle()
        {
            if (string.IsNullOrEmpty(buttonId)) return;

            var config = source.GetButtonById(buttonId);
            if (config == null)
            {
                Debug.LogWarning($"[ButtonPreview] No button with ID '{buttonId}' found in '{source.name}'.", this);
                return;
            }

            var preview = PreviewRoot.AddComponent<ButtonManager>();
            preview.ApplyPreviewConfig(source);
            preview.SetPreviewGroups(new List<ButtonManager.ButtonGroup>
            {
                new()
                {
                    groupName = "preview_single",
                    buttons   = new List<ButtonConfig> { config },
                    corner    = singleCorner,
                }
            });
            preview.Refresh();
        }

        #endregion
    }
}
#endif
