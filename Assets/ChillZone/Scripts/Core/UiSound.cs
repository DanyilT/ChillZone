namespace ChillZone.Core
{
    /// <summary>
    /// A distinct UI action that can have its own click sound (see <see cref="AudioService.PlayUi"/>).
    /// Each maps to a clip in the AudioService UI-SFX list; any action without its own clip falls back
    /// to the generic <see cref="Click"/> sound.
    /// </summary>
    public enum UiSound
    {
        /// <summary>Generic button click / the fallback for every other action.</summary>
        Click,
        /// <summary>A panel/window/content-picker opened.</summary>
        Open,
        /// <summary>A panel/window/content-picker closed.</summary>
        Close,
        /// <summary>A toggle switch flipped.</summary>
        Toggle,
        /// <summary>An item was picked in a content picker.</summary>
        PickItem,
        /// <summary>The game was paused.</summary>
        Pause,
        /// <summary>The game was resumed.</summary>
        Unpause,
        /// <summary>A reset button (reset ball / reset scanning) was pressed.</summary>
        Reset,
    }
}
