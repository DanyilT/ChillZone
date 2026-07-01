using UnityEngine;

namespace ChillZone.Scene.Settings.Cell
{
    /// <summary>
    /// Hosts an arbitrary prefab as a settings row — e.g. the three-button DeveloperOptions
    /// panel (throw-mode selector). The prefab is self-contained (its own DeveloperOptionsController
    /// wires the buttons at runtime), so the cell just instantiates it into the scroll content.
    /// </summary>
    [CreateAssetMenu(fileName = "CustomCell", menuName = "ChillZone/Settings/Cell/Custom", order = 40)]
    public class CustomCell : SettingsCell
    {
        [SerializeField] private GameObject prefab;
        [SerializeField] private float height = -1f;

        public override void Build(SettingsBuilder.SettingsCellBuilder builder, SettingsActions actions) =>
            builder.CreateCustomCell(prefab, height);
    }
}
