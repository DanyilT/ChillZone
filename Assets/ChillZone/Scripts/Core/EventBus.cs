using System;

namespace ChillZone.Core
{
    /// <summary>
    /// Lightweight typed event bus. Systems subscribe, unsubscribe, and raise
    /// events without needing direct references to each other.
    ///
    /// Usage:
    ///   <example>EventBus&lt;BallScoredEvent&gt;.Subscribe(OnBallScored);  // in OnEnable</example>
    ///   <example>EventBus&lt;BallScoredEvent&gt;.Unsubscribe(OnBallScored);  // in OnDisable</example>
    ///   <example>EventBus&lt;BallScoredEvent&gt;.Raise(new BallScoredEvent { ... });</example>
    /// </summary>
    public static class EventBus<T>
    {
        private static event Action<T> Handlers;

        public static void Subscribe(Action<T> handler)   => Handlers += handler;
        public static void Unsubscribe(Action<T> handler) => Handlers -= handler;
        public static void Raise(T evt)                   => Handlers?.Invoke(evt);

        /// <summary>Clears all subscribers. Call on scene unload to avoid stale refs.</summary>
        public static void Clear() => Handlers = null;
    }
}
