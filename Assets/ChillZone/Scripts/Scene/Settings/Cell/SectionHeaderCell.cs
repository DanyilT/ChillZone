using UnityEngine;

namespace ChillZone.Scene.Settings.Cell
{
    [CreateAssetMenu(fileName = "SectionHeaderCell", menuName = "ChillZone/Settings/Cell/Section Header", order = 1)]
    public class SectionHeaderCell : SettingsCell
    {
        [SerializeField] private string text = "SECTION";
        [SerializeField] private float height = 40f;

        public override void Build(SettingsBuilder.SettingsCellBuilder builder, SettingsActions actions) =>
            builder.CreateSectionHeader(text, height);
    }
}
