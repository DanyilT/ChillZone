namespace ChillZone.Core.Events
{
    /// <summary>
    /// Raised by ThrowSettingsStore whenever the developer changes the active
    /// throw mode. Lets HUD / dev panels reflect the change without polling the
    /// ThrowConfig.
    /// </summary>
    public struct ThrowSettingsChangedEvent
    {
        public ThrowMode Mode;
    }
}
