using UnityEngine;

namespace ChillZone.Core
{
    /// <summary>
    /// Developer mode flag, persisted as a device preference (<see cref="PrefKeys.DeveloperMode"/>).
    /// It is intentionally NOT part of the player profile — it's not achievement/unlock progress,
    /// just a local toggle that reveals developer-only settings (e.g. the throw-mode selector).
    /// Enabled/disabled by entering the "dev" code in the settings code input, which calls
    /// <see cref="Toggle"/>.
    /// </summary>
    public static class DeveloperMode
    {
        /// <summary>True when developer mode is currently enabled on this device.</summary>
        public static bool IsEnabled => PlayerPrefs.GetInt(PrefKeys.DeveloperMode, 0) == 1;

        /// <summary>Flips developer mode on/off and persists it. Returns the new state.</summary>
        public static bool Toggle() => SetEnabled(!IsEnabled);

        /// <summary>Sets developer mode explicitly and persists it. Returns the new state.</summary>
        public static bool SetEnabled(bool enabled)
        {
            PlayerPrefs.SetInt(PrefKeys.DeveloperMode, enabled ? 1 : 0);
            PlayerPrefs.Save();
            return enabled;
        }
    }
}
