using System.Collections.Generic;
using UnityEngine;

namespace ChillZone.Scene.Settings
{
    /// <summary>
    /// Ordered list of <see cref="Cell.SettingsCell"/> assets that defines one settings screen.
    /// The SettingsBuilder iterates this to construct the screen. Make several registries
    /// for several screens.
    /// </summary>
    [CreateAssetMenu(fileName = "SettingsCellRegistry", menuName = "ChillZone/Settings/Cell Registry")]
    public class SettingsCellRegistry : ScriptableObject
    {
        [Tooltip("Cells are built top-to-bottom in this order.")]
        public List<Cell.SettingsCell> cells = new();
    }
}
