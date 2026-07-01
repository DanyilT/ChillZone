using TMPro;
using UnityEngine;

namespace ChillZone.Scene.Settings.Cell
{
    /// <summary>
    /// Non-interactable text row whose value is resolved at build time from a
    /// <see cref="SettingsActions"/> text provider (by action key) — e.g. the app version
    /// (<c>settings.app.version</c> → <c>Application.version</c>). An optional label is prefixed.
    /// In edit mode (no actions) only the label shows.
    /// </summary>
    [CreateAssetMenu(fileName = "ValueTextCell", menuName = "ChillZone/Settings/Cell/Value Text", order = 28)]
    public class ValueTextCell : SettingsCell
    {
        [SerializeField] private string label = "Version";
        [SerializeField] private string actionKey = "settings.app.version";
        [SerializeField] private float  height = 40f;
        [SerializeField] private TextAlignmentOptions alignment = TextAlignmentOptions.Center;

        public override void Build(SettingsBuilder.SettingsCellBuilder builder, SettingsActions actions)
        {
            var value = actions ? actions.GetText(actionKey, string.Empty) : string.Empty;
            var text  = string.IsNullOrEmpty(label) ? value : $"{label} {value}".TrimEnd();
            builder.CreateInfoText(text, height, alignment);
        }
    }
}
