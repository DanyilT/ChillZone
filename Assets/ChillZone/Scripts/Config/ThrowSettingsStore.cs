using ChillZone.Core;
using ChillZone.Core.Events;
using UnityEngine;

namespace ChillZone.Config
{
    /// <summary>
    /// Persists the developer-facing throw mode to PlayerPrefs and applies it onto
    /// the shared <see cref="ThrowConfig"/>.
    ///
    /// ScriptableObject edits made at runtime do NOT survive an app restart in a
    /// build, so PlayerPrefs is the source of truth. Call <see cref="ApplyTo"/> once
    /// per scene load (e.g. from BallSpawnManager.Awake) to push the saved value back
    /// onto the config before gameplay reads it.
    /// </summary>
    public static class ThrowSettingsStore
    {
        private const string ModeKey = "dev.throw.mode";

        #region public api

        /// <summary>Pushes the persisted throw mode onto the config. No-op if never set.</summary>
        public static void ApplyTo(ThrowConfig config)
        {
            if (config == null) return;
            if (PlayerPrefs.HasKey(ModeKey))
                config.mode = (ThrowMode)PlayerPrefs.GetInt(ModeKey, (int)config.mode);
        }

        public static void SetMode(ThrowConfig config, ThrowMode mode)
        {
            if (config != null) config.mode = mode;
            PlayerPrefs.SetInt(ModeKey, (int)mode);
            PlayerPrefs.Save();

            EventBus<ThrowSettingsChangedEvent>.Raise(new ThrowSettingsChangedEvent { Mode = mode });
        }

        #endregion
    }
}
