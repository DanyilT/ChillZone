using ChillZone.Core;
using UnityEngine;

namespace ChillZone.Config
{
    [CreateAssetMenu(fileName = "ThrowConfig", menuName = "ChillZone/Config/Throw Config")]
    public class ThrowConfig : ScriptableObject
    {
        [Header("Throw Mode — developer option"), Tooltip("Straight: always forward. DragPath: forward + lateral lean from the swipe. Enhanced: DragPath + spin from a curved swipe.")]
        public ThrowMode mode = ThrowMode.Enhanced;

        [Header("Throw")]
        [Tooltip("Minimum screen-space drag distance (pixels) before a throw registers.")]
        public float minDragDistance = 80f;
        [Tooltip("Forward launch speed (m/s) per unit of flick speed (pixels/sec) at release. Drives how far the ball travels INTO the scene.")]
        public float forwardSpeedScale = 0.004f;
        [Tooltip("Upward launch speed (m/s) per pixel of upward drag distance. Higher = higher arcs.")]
        public float upSpeedScale = 0.008f;
        [Tooltip("Clamp on the forward launch speed (m/s) so fast flicks don't over-throw.")]
        public float maxThrowSpeed = 10f;

        [Header("DragPath + Enhanced")]
        [Tooltip("How much the horizontal flick direction leans the throw left/right."), Range(0f, 2f)]
        public float lateralInfluence = 0.75f;
        [Tooltip("How many of the most-recent drag samples define the flick speed/direction at let-go."), Range(2, 10)]
        public int releaseSampleCount = 4;

        [Header("Enhanced Only")]
        [Tooltip("How strongly the detected swipe curvature translates to (cosmetic) spin.")]
        public float curvatureSpinMultiplier = 25f;
        [Tooltip("Number of drag samples kept for curvature analysis."), Range(5, 40)]
        public int dragHistorySize = 15;
    }
}
