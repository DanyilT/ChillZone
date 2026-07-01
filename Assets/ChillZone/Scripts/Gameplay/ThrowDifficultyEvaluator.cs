using ChillZone.Config;
using ChillZone.Core;
using UnityEngine;

namespace ChillZone.Gameplay
{
    /// <summary>
    /// Pure static utility that converts throw mode + drag curvature into a
    /// difficulty multiplier and a short human-readable label.
    /// </summary>
    public static class ThrowDifficultyEvaluator
    {
        public struct Result
        {
            public float Multiplier;
            public string Label;
        }

        public static Result Evaluate(ThrowMode mode, float totalCurvature, ScoringConfig config)
        {
            switch (mode)
            {
                case ThrowMode.Straight:
                    return new Result { Multiplier = 1.0f, Label = "Straight" };

                case ThrowMode.DragPath:
                    return new Result { Multiplier = config.dragPathBonus, Label = "Aimed" };

                case ThrowMode.Enhanced:
                {
                    var bonus = Mathf.Clamp(
                        1.0f + Mathf.Abs(totalCurvature) * config.spinBonusFactor,
                        1.0f,
                        config.maxSpinBonus);
                    var label = bonus > 1.05f ? $"Spin ×{bonus:F1}" : "Enhanced";
                    return new Result { Multiplier = bonus, Label = label };
                }

                default:
                    return new Result { Multiplier = 1.0f, Label = "" };
            }
        }
    }
}
