using System.Collections.Generic;
using ChillZone.Config;
using ChillZone.Core;
using UnityEngine;

namespace ChillZone.Gameplay
{
    /// <summary>
    /// Pure throw math. Converts a finished screen-space drag into a world-space launch that flies
    /// FORWARD into the scene (toward a basket ahead): forward speed comes from how fast the swipe was
    /// flicked at let-go, the arc from how far UP it was dragged, and a left/right lean from the flick's
    /// horizontal direction. Enhanced mode adds cosmetic spin from a curved swipe. Stateless / no
    /// side-effects so it is trivial to unit-test.
    /// </summary>
    public static class ThrowCalculator
    {
        public readonly struct Solution
        {
            /// <summary>Initial rigidbody velocity (m/s, world space) — apply with ForceMode.VelocityChange.</summary>
            public readonly Vector3 LinearVelocity;
            /// <summary>Initial angular velocity (cosmetic spin, Enhanced mode only).</summary>
            public readonly Vector3 AngularVelocity;
            /// <summary>Signed swipe curvature, forwarded to scoring for the Enhanced difficulty bonus.</summary>
            public readonly float TotalCurvature;

            public Solution(Vector3 linearVelocity, Vector3 angularVelocity, float totalCurvature)
            {
                LinearVelocity = linearVelocity;
                AngularVelocity = angularVelocity;
                TotalCurvature = totalCurvature;
            }
        }

        /// <summary>Solve the launch for a finished drag.</summary>
        /// <param name="screenHistory">Recent drag points in screen space (oldest → newest).</param>
        /// <param name="times">Unscaled timestamps aligned with <paramref name="screenHistory"/>.</param>
        /// <param name="dragVector">Net drag from the true start to release (screen px) — drives the arc.</param>
        /// <param name="cam">Release-time camera — supplies the forward/up/right aim axes.</param>
        /// <param name="config">Tuning. Null falls back to sane defaults.</param>
        public static Solution Solve(IReadOnlyList<Vector2> screenHistory, IReadOnlyList<float> times, Vector2 dragVector, Camera cam, ThrowConfig config)
        {
            if (cam == null || screenHistory == null || times == null || screenHistory.Count < 2)
                return new Solution(Vector3.zero, Vector3.zero, 0f);

            ThrowMode mode = config ? config.mode : ThrowMode.Enhanced;

            // The flick at let-go: screen-space velocity (px/s) of the last few samples.
            Vector2 flick = ComputeReleaseFlick(screenHistory, times, config);
            float flickSpeed = flick.magnitude;

            float fwdScale = config ? config.forwardSpeedScale : 0.004f;
            float upScale = config ? config.upSpeedScale : 0.008f;
            float maxFwd = config ? config.maxThrowSpeed : 10f;
            float lateral = config ? config.lateralInfluence : 0.75f;

            // Forward speed from HOW FAST the swipe was flicked; up speed from HOW FAR UP it was dragged.
            float forwardSpeed = Mathf.Min(flickSpeed * fwdScale, maxFwd);
            float upSpeed = Mathf.Max(0f, dragVector.y) * upScale;

            // Aim straight into the scene, leaning left/right by the flick direction (Straight ignores lean).
            Vector3 forwardDir = cam.transform.forward;
            if (mode != ThrowMode.Straight && flickSpeed > 1f)
                forwardDir += cam.transform.right * (flick.x / flickSpeed * lateral);
            forwardDir.Normalize();

            Vector3 linear = forwardDir * forwardSpeed + cam.transform.up * upSpeed;

            float curvature = 0f;
            Vector3 angular = Vector3.zero;
            if (mode == ThrowMode.Enhanced)
            {
                curvature = ComputeCurvature(screenHistory);
                // Cosmetic side-spin about the camera-up axis (Unity physics doesn't curve the flight via
                // Magnus); the curvature magnitude also feeds ScoringSystem's Enhanced difficulty bonus.
                float spinMult = config ? config.curvatureSpinMultiplier : 25f;
                angular = cam.transform.up * (curvature * spinMult);
            }

            return new Solution(linear, angular, curvature);
        }

        // Screen-space velocity (px/sec) over the last few drag samples — the speed and direction of the
        // flick at let-go. Using the recent samples (not start→end) is what stops a curved/looping drag
        // from launching backwards.
        private static Vector2 ComputeReleaseFlick(IReadOnlyList<Vector2> screenHistory, IReadOnlyList<float> times, ThrowConfig config)
        {
            int count = screenHistory.Count;
            int samples = config ? config.releaseSampleCount : 4;
            int from = Mathf.Max(0, count - samples);

            Vector2 delta = screenHistory[count - 1] - screenHistory[from];
            float dt = times[count - 1] - times[from];
            return dt > 0.0001f ? delta / dt : Vector2.zero;
        }

        // Signed screen-space curvature of the swipe: sum of normalised cross products of consecutive
        // segments. Near-zero for a flick, large for a looping/curved drag.
        private static float ComputeCurvature(IReadOnlyList<Vector2> screenHistory)
        {
            float total = 0f;
            for (int i = 1; i < screenHistory.Count - 1; i++)
            {
                Vector2 d1 = screenHistory[i] - screenHistory[i - 1];
                Vector2 d2 = screenHistory[i + 1] - screenHistory[i];
                if (d1.sqrMagnitude < 1f || d2.sqrMagnitude < 1f) continue;
                total += (d1.x * d2.y - d1.y * d2.x) / (d1.magnitude * d2.magnitude);
            }
            return total;
        }
    }
}
