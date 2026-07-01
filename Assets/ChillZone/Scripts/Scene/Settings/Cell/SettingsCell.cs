using ChillZone.Core;
using UnityEngine;

namespace ChillZone.Scene.Settings.Cell
{
    /// <summary>
    /// Base ScriptableObject for one settings row. Concrete cells build their view through
    /// the <see cref="ChillZone.Scene.Settings.SettingsBuilder.SettingsCellBuilder"/> and bind behaviour to <see cref="SettingsActions"/>
    /// by key. Behaviour binds lazily, so in edit mode (actions == null) cells still render.
    ///
    /// NOTE: every concrete cell must live in its own file named after the class — Unity only
    /// creates a stable script asset for the type matching the file name.
    /// </summary>
    public abstract class SettingsCell : ScriptableObject
    {
        [SerializeField, Tooltip("When on, this cell is only built while developer mode is enabled (entering the \"dev\" code toggles it). Used for the throw-mode selector and other developer-only rows.")]
        private bool developerOnly;

        /// <summary>Whether this cell should be built for the current screen. Developer-only cells build only while <see cref="DeveloperMode.IsEnabled"/>.</summary>
        public bool ShouldBuild => !developerOnly || DeveloperMode.IsEnabled;

        public abstract void Build(SettingsBuilder.SettingsCellBuilder builder, SettingsActions actions);
    }
}
