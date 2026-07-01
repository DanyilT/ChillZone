using UnityEngine;

namespace ChillZone.Config
{
    [CreateAssetMenu(fileName = "ScoringConfig", menuName = "ChillZone/Config/Scoring Config")]
    public class ScoringConfig : ScriptableObject
    {
        [Header("Base Score"), Tooltip("Points awarded for a basket before multipliers.")]
        public int basePoints = 10;

        [Header("Distance Multiplier")]
        [Tooltip("Throws shorter than this distance receive a 1× multiplier.")]
        public float minMultiplierDistance = 0.5f;
        [Tooltip("Throws at or beyond this distance receive the maximum distance multiplier.")]
        public float maxMultiplierDistance = 5.0f;
        [Tooltip("Maximum multiplier from distance alone.")]
        public float maxDistanceMultiplier = 7.0f;

        [Header("Throw Difficulty Multiplier")]
        [Tooltip("Flat multiplier applied when throw mode is DragPath.")]
        public float dragPathBonus = 1.15f;
        [Tooltip("Upper cap for the Enhanced (spin) difficulty multiplier.")]
        public float maxSpinBonus = 10.0f;
        [Tooltip("Converts absolute curvature (summed cross-product) into a bonus multiplier for Enhanced throws.")]
        public float spinBonusFactor = 0.5f;

        [Header("Score Flash Duration"), Tooltip("How long the '+N (×M)' label stays visible after a basket.")]
        public float scoreFlashDuration = 2.5f;
    }
}
