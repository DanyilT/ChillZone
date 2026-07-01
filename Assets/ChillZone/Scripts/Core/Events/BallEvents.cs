using UnityEngine;

namespace ChillZone.Core.Events
{
    /// <summary>Raised by BallController when the ball is launched.</summary>
    public struct BallThrownEvent
    {
        public ThrowMode Mode;
        public float TotalCurvature;   // signed sum of cross-product curvature along drag path
        public Vector3 ReleasePosition;
    }

    /// <summary>Raised by ScoringSystem when a basket score is confirmed.</summary>
    public struct BallScoredEvent
    {
        public int FinalPoints;
        public float DistanceMultiplier;
        public float DifficultyMultiplier;
        public float BasketMultiplier;
        public string DifficultyLabel;   // e.g. "Straight", "Aimed", "Spin ×1.8"
        public Vector3 HitPoint;
    }

    /// <summary>Raised by ScoringSystem when the ball misses and the run ends.</summary>
    public struct BallMissedEvent
    {
        public Vector3 HitPoint;
    }
}
