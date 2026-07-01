using System;
using UnityEngine;

namespace ChillZone.Scene.Settings.Cell
{
    [CreateAssetMenu(fileName = "CodeInputCell", menuName = "ChillZone/Settings/Cell/Code Input", order = 27)]
    public class CodeInputCell : SettingsCell
    {
        [SerializeField] private string text = "Code";
        [SerializeField] private string placeholder = "Enter code...";

        public override void Build(SettingsBuilder.SettingsCellBuilder builder, SettingsActions actions)
        {
            Func<string, bool> onSubmit = actions ? actions.SubmitCode : null;
            builder.CreateCodeInputCell(text, placeholder, onSubmit);
        }
    }
}
