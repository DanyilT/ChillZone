using UnityEngine;

namespace ChillZone.Ball
{
    /// <summary>
    /// Inspector-authored description of the ball's flight trail. <see cref="BallBehaviour"/> builds a
    /// <see cref="TrailRenderer"/> from these values at runtime — no prefab required — and only emits
    /// while the ball is in flight.
    /// </summary>
    [System.Serializable]
    public class BallTrailSettings
    {
        [Tooltip("Generate a trail while the ball is in flight.")]
        public bool enabled = false;

        [Tooltip("Seconds the trail lingers behind the ball.")]
        public float time = 0.35f;

        [Tooltip("Trail width at the ball.")]
        public float startWidth = 0.12f;

        [Tooltip("Trail width at the tail end.")]
        public float endWidth = 0f;

        [Tooltip("Override the trail colour with the ball's accent colour (BallData.uiAccentColor). The gradient's alpha (fade) below is kept.")]
        public bool useAccentColor = true;

        [Tooltip("Colour along the trail (left = at the ball, right = tail). Use alpha to fade out. Ignored when 'Use Accent Color' is on.")]
        public Gradient color = DefaultGradient();

        [Tooltip("Minimum world distance between recorded trail points. Larger = cheaper but blockier.")]
        public float minVertexDistance = 0.02f;

        [Tooltip("Optional material override. Leave empty to use a built-in unlit, vertex-coloured material.")]
        public Material material;

        /// <summary>
        /// Configure a <see cref="TrailRenderer"/> from these settings. When <see cref="useAccentColor"/>
        /// is on, <paramref name="accentColor"/> (the ball's accent color) overrides the gradient's RGB
        /// while keeping its alpha (fade) keys.
        /// </summary>
        public void ApplyTo(TrailRenderer trail, Color accentColor)
        {
            trail.time = time;
            trail.startWidth = startWidth;
            trail.endWidth = endWidth;
            trail.minVertexDistance = minVertexDistance;
            trail.autodestruct = false;
            trail.numCapVertices = 2;
            trail.numCornerVertices = 2;
            trail.alignment = LineAlignment.View;

            var gradient = color ?? DefaultGradient();
            trail.colorGradient = useAccentColor ? TintGradient(gradient, accentColor) : gradient;
            trail.material = material ? material : SharedMaterial;
        }

        // Re-color a gradient to a single RGB while preserving its alpha (fade) keys.
        private static Gradient TintGradient(Gradient source, Color rgb)
        {
            var tinted = new Gradient();
            tinted.SetKeys(
                new[] { new GradientColorKey(rgb, 0f), new GradientColorKey(rgb, 1f) },
                source.alphaKeys);
            return tinted;
        }

        private static Gradient DefaultGradient()
        {
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
            return gradient;
        }

        private static Material _sharedMaterial;
        private static Material SharedMaterial
        {
            get
            {
                if (!_sharedMaterial)
                    _sharedMaterial = new Material(Shader.Find("Sprites/Default")) { name = "BallTrailMat" };
                return _sharedMaterial;
            }
        }
    }
}
