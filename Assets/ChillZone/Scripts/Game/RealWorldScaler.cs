using System.Collections.Generic;
using UnityEngine;

namespace ChillZone.Game
{
    [ExecuteAlways, RequireComponent(typeof(Transform))]
    public class RealWorldScaler : MonoBehaviour
    {
        [SerializeField, Tooltip("Target size in meters for the chosen reference axis. The object will be scaled so that its reference axis matches this size.")]
        private float targetSizeMeters = 1f;
        [SerializeField, Tooltip("Reference axis to use for scaling. The object will be scaled so that this axis matches the target size.")]
        private ReferenceAxis referenceAxis = ReferenceAxis.LongestAxis;

        private enum ReferenceAxis { LongestAxis, ShortestAxis, Width, Height, Depth }

        private void Awake() => ApplyScale();

        /// <summary>Sets the target real-world size and re-applies the scale (used when added at runtime).</summary>
        public void SetTargetSize(float meters)
        {
            targetSizeMeters = meters;
            ApplyScale();
        }

        private void ApplyScale()
        {
            // Size from the model meshes (and sprites, for sprite VFX). Trail / line / particle renderers
            // (e.g. the ball's flight trail) must NOT count — their bounds inflate the measurement.
            var renderers = new List<Renderer>();
            renderers.AddRange(GetComponentsInChildren<MeshRenderer>());
            renderers.AddRange(GetComponentsInChildren<SkinnedMeshRenderer>());
            renderers.AddRange(GetComponentsInChildren<SpriteRenderer>());
            if (renderers.Count == 0)
            {
                Debug.LogWarning($"[RealWorldScaler] No mesh/sprite Renderer found on {name} or its children.", this);
                return;
            }

            // Measure with localScale reset to 1 AND world rotation neutralised. renderer.bounds is a WORLD
            // axis-aligned box, so a rotated object reports a larger, orientation-dependent size — and the
            // ball's spawn point follows the camera, so without this the measured size (and the resulting
            // scale) changed every reset depending on where the player was aiming. Identity rotation gives the
            // true model extent. Both happen in one synchronous call, so the intermediate transform is never
            // rendered — no visible pop.
            var originalRotation = transform.rotation;
            transform.localScale = Vector3.one;
            transform.rotation = Quaternion.identity;

            var bounds = renderers[0].bounds;
            foreach (var r in renderers)
                bounds.Encapsulate(r.bounds);

            transform.rotation = originalRotation;

            var referenceSize = referenceAxis switch
            {
                ReferenceAxis.Width => bounds.size.x,
                ReferenceAxis.Height => bounds.size.y,
                ReferenceAxis.Depth => bounds.size.z,
                ReferenceAxis.ShortestAxis => Mathf.Min(bounds.size.x, bounds.size.y, bounds.size.z),
                _ => Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z),
            };

            if (referenceSize <= 0f)
            {
                Debug.LogWarning($"[RealWorldScaler] Computed reference size is zero on {name}.", this);
                return;
            }

            transform.localScale = Vector3.one * (targetSizeMeters / referenceSize);
        }
    }
}
