using System;
using UnityEngine;

namespace ChillZone.Scene.Settings.Cell
{
    [CreateAssetMenu(fileName = "TextButtonCell", menuName = "ChillZone/Settings/Cell/Text + Button", order = 22)]
    public class TextButtonCell : SettingsCell
    {
        [SerializeField] private string text = "Label";
        [SerializeField] private string buttonText = "Open";
        [SerializeField] private string actionKey;

        public override void Build(SettingsBuilder.SettingsCellBuilder builder, SettingsActions actions)
        {
            Action onClick = actions ? () => actions.InvokeButton(actionKey) : null;
            builder.CreateTextButtonCell(text, buttonText, onClick);
        }
    }
}
