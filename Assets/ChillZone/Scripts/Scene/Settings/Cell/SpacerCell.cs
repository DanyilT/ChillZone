using UnityEngine;

namespace ChillZone.Scene.Settings.Cell
{
    [CreateAssetMenu(fileName = "SpacerCell", menuName = "ChillZone/Settings/Cell/Spacer", order = 3)]
    public class SpacerCell : SettingsCell
    {
        [SerializeField] private float height = 20f;

        public override void Build(SettingsBuilder.SettingsCellBuilder builder, SettingsActions actions) =>
            builder.CreateSpacer(height);
    }
}
