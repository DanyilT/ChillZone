namespace ChillZone.Core
{
    /// <summary>
    /// PlayerPrefs keys for gameplay/progression state that isn't owned by a dedicated
    /// service. (Audio keys live in AudioService, throw-mode in ThrowSettingsStore, and
    /// settings-screen keys in SettingsKeys — those stay encapsulated where they're used.)
    /// </summary>
    public static class PrefKeys
    {
        /// <summary>Stable id of the currently selected ball (mirror of the profile field).</summary>
        public const string SelectedBall = "selected_ball";

        /// <summary>Stable id of the currently selected basket (mirror of the profile field).</summary>
        public const string SelectedBasket = "selected_basket";

        /// <summary>Set once the first-run manual/help window has been dismissed.</summary>
        public const string ManualViewed = "ManualViewed";

        /// <summary>Set once the first-run "AR not supported → virtual mode" notice has been dismissed (shown only once, like the manual).</summary>
        public const string ArUnsupportedNoticeViewed = "ArUnsupportedNoticeViewed";

        /// <summary>1 while the user has the virtual (camera-off) environment toggled on, so the choice is restored across scene loads (back from Settings) and app restarts.</summary>
        public const string VirtualEnvironment = "VirtualEnvironment";

        /// <summary>1 when the qwerty easter-egg code was just redeemed, so the game shows the one-time max-score flourish on the next HUD entry, then clears it.</summary>
        public const string EasterEggScorePending = "EasterEggScorePending";

        /// <summary>
        /// Left-handed mode. Read per-scene by ButtonManager to mirror its buttons horizontally
        /// (ButtonManagers aren't DontDestroyOnLoad — each scene has its own — so the shared
        /// device pref is the source of truth). The value string is kept identical to the settings
        /// toggle's action key so no existing preference is lost.
        /// </summary>
        public const string LeftHanded = "settings.lefthanded";

        /// <summary>
        /// Developer mode. Toggled by entering the "dev" code in the settings code input; when set,
        /// the developer-only settings cells (e.g. the throw-mode selector) are shown. This is a
        /// device preference, NOT player progression — it is deliberately not stored in the profile.
        /// </summary>
        public const string DeveloperMode = "DeveloperMode";
    }
}
