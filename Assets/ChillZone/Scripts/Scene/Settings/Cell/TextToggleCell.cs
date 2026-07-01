using System;
using UnityEngine;

namespace ChillZone.Scene.Settings.Cell
{
    [CreateAssetMenu(fileName = "TextToggleCell", menuName = "ChillZone/Settings/Cell/Text + Toggle", order = 24)]
    public class TextToggleCell : SettingsCell
    {
        [SerializeField] private string text = "Label";
        [SerializeField] private bool defaultValue = true;
        [SerializeField] private string actionKey;

        public override void Build(SettingsBuilder.SettingsCellBuilder builder, SettingsActions actions)
        {
            var initial = actions ? actions.GetToggle(actionKey, defaultValue) : defaultValue;
            Action<bool> onChanged = actions ? value => actions.SetToggle(actionKey, value) : null;
            builder.CreateTextToggleCell(text, initial, onChanged);
        }
    }
}
