using UnityEngine;

namespace ChillZone.Scene.Settings.Cell
{
    [CreateAssetMenu(fileName = "InfoCell", menuName = "ChillZone/Settings/Cell/Info", order = 2)]
    public class InfoCell : SettingsCell
    {
        [SerializeField, TextArea(2, 6)] private string text = "Description";
        [SerializeField] private float height = 30f;

        public override void Build(SettingsBuilder.SettingsCellBuilder builder, SettingsActions actions) =>
            builder.CreateInfoText(text, height);
    }
}
