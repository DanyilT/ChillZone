using ChillZone.Core;
using UnityEngine;

namespace ChillZone.Config
{
    [CreateAssetMenu(fileName = "GameConfig", menuName = "ChillZone/Config/Game Config")]
    public class GameConfig : ScriptableObject
    {
        public enum ScoreContextMode
        {
            Custom,        // use scoreContextId verbatim
            ThrowModeBased // use "<throw-mode>" + scoreContextSuffix, e.g. "enhanced-run"
        }

        [Header("AR Surface"), Tooltip("Minimum detected plane area (m²) before basket placement is allowed.")]
        public float minimumPlaneArea = 0.8f;

        [Header("Ball Out-of-Bounds"), Tooltip("World-axis magnitude beyond which a ball is considered out of bounds.")]
        public float maxWorldAxisMagnitude = 1000f;

        [Header("Score Context"), Tooltip("Custom = use scoreContextId as-is. ThrowModeBased = '<throw-mode>' + scoreContextSuffix (e.g. enhanced-run).")]
        public ScoreContextMode scoreContextMode = ScoreContextMode.ThrowModeBased;
        [Tooltip("Identifier used when scoreContextMode = Custom.")]
        public string scoreContextId = "basket-run";
        [Tooltip("Appended to the lowercased throw-mode name when scoreContextMode = ThrowModeBased.")]
        public string scoreContextSuffix = "-run";

        /// <summary>Resolves the leaderboard/profile context id for a run thrown in the given mode.</summary>
        public string ResolveScoreContextId(ThrowMode mode) => scoreContextMode == ScoreContextMode.ThrowModeBased
            ? mode.ToString().ToLowerInvariant() + scoreContextSuffix
            : scoreContextId;
    }
}
