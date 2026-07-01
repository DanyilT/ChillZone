using System;

namespace ChillZone.UI.Window
{
    /// <summary>
    /// Runtime callbacks and button definitions passed when showing a window.
    /// All fields are optional — unset ones fall back to WindowConfig defaults.
    /// </summary>
    public class WindowShowOptions
    {
        /// <summary>Called when the user taps the backdrop (area outside the window panel).</summary>
        public Action OnBackdropClick { get; private set; }

        /// <summary>Called when the user taps the window panel itself.</summary>
        public Action OnPanelClick { get; private set; }

        /// <summary>Called when the user taps the header bar. If null, falls back to WindowConfig.closeOnHeaderClick.</summary>
        public Action OnHeaderClick { get; private set; }

        /// <summary>
        /// When true, this window will NOT register a back-navigation handler.
        /// Use for windows (like the pause screen) where you want the hardware back
        /// button to do something else (e.g., quit) rather than close this window.
        /// </summary>
        public bool SkipBackNavigation = false;

        /// <summary>Optional dynamic text a custom window can display (e.g. the best-score value). Ignored by standard windows.</summary>
        public string PrimaryText { get; private set; }

        // ── Fluent helpers ────────────────────────────────────────────────────────

        public WindowShowOptions SetOnBackdropClick(Action action)
        {
            OnBackdropClick = action;
            return this;
        }

        public WindowShowOptions SetOnPanelClick(Action action)
        {
            OnPanelClick = action;
            return this;
        }

        public WindowShowOptions SetOnHeaderClick(Action action)
        {
            OnHeaderClick = action;
            return this;
        }

        public WindowShowOptions SetPrimaryText(string text)
        {
            PrimaryText = text;
            return this;
        }
    }
}
