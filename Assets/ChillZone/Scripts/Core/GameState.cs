namespace ChillZone.Core
{
    /// <summary>
    /// Top-level game-flow state. Lives in Core because it is carried by
    /// <c>GameStateChangedEvent</c> (a Core event) and observed across layers, not only by
    /// the Game-flow controller that drives it.
    /// </summary>
    public enum GameState
    {
        Welcome,
        Scanning,
        Placing,
        Manual,
        Playing,
        Paused,
    }
}
