using System;
using UnityEngine;

namespace ChillZone.Scene.Settings.Cell
{
    [CreateAssetMenu(fileName = "TextLinkButtonCell", menuName = "ChillZone/Settings/Cell/Text + Button (url)", order = 23)]
    public class TextLinkButtonCell : SettingsCell
    {
        [SerializeField] private string text = "Label";
        [SerializeField] private string buttonText = "Open";
        [SerializeField] private string url;

        public override void Build(SettingsBuilder.SettingsCellBuilder builder, SettingsActions actions)
        {
            Action onClick = actions ? () => actions.OpenUrl(url) : null;
            builder.CreateTextButtonCell(text, buttonText, onClick);
        }
    }
}
