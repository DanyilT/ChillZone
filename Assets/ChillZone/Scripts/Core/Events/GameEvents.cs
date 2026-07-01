namespace ChillZone.Core.Events
{
    /// <summary>Raised by GameFlowController on every state transition.</summary>
    public struct GameStateChangedEvent
    {
        public GameState Previous;
        public GameState Current;
    }

    /// <summary>Raised by UI (e.g. a pause button) to request a pause toggle; handled by GameFlowController. Keeps UI decoupled from the gameplay assembly.</summary>
    public struct TogglePauseRequestedEvent
    {
    }

    /// <summary>Raised by UI to clear scanned AR surfaces and return to the scanning state; handled by GameFlowController.</summary>
    public struct ResetScanRequestedEvent
    {
    }

    /// <summary>Raised by UI to reset/respawn the active ball; handled by BallSpawnManager (score is reset only when the previous ball wasn't resting at the spawn point).</summary>
    public struct ResetBallRequestedEvent
    {
    }
}
