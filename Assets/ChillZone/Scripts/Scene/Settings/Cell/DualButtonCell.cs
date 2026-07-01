using System;
using UnityEngine;

namespace ChillZone.Scene.Settings.Cell
{
    [CreateAssetMenu(fileName = "DualButtonCell", menuName = "ChillZone/Settings/Cell/Dual Button (url)", order = 26)]
    public class DualButtonCell : SettingsCell
    {
        [SerializeField] private string title;
        [SerializeField] private string labelA = "Left";
        [SerializeField] private string urlA;
        [SerializeField] private string labelB = "Right";
        [SerializeField] private string urlB;

        public override void Build(SettingsBuilder.SettingsCellBuilder builder, SettingsActions actions)
        {
            Action onA = actions ? () => actions.OpenUrl(urlA) : null;
            Action onB = actions ? () => actions.OpenUrl(urlB) : null;
            builder.CreateDualButtonCell(title, (labelA, onA), (labelB, onB));
        }
    }
}
