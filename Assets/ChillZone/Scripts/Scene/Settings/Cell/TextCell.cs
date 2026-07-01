using System;
using UnityEngine;

namespace ChillZone.Scene.Settings.Cell
{
    [CreateAssetMenu(fileName = "TextCell", menuName = "ChillZone/Settings/Cell/Text", order = 21)]
    public class TextCell : SettingsCell
    {
        [SerializeField] private string text = "Label";
        [SerializeField] private bool clickable;
        [SerializeField] private string actionKey;

        public override void Build(SettingsBuilder.SettingsCellBuilder builder, SettingsActions actions)
        {
            if (actionKey == "settings.app.version")
            {
                var value = actions ? actions.GetText(actionKey, string.Empty) : string.Empty;
                text = string.IsNullOrEmpty(text) ? value : $"{text} {value}".TrimEnd();
            }

            Action onClick = clickable && actions ? () => actions.InvokeButton(actionKey) : null;
            builder.CreateTextCell(text, clickable, onClick);
        }
    }
}
