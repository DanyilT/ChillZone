using UnityEngine;

namespace ChillZone.Utils.Vfx
{
    /// <summary>
    /// Destroys this GameObject a fixed time after it spawns — for one-shot VFX (e.g. the ball hit/miss
    /// prefabs) so a non-looping animation plays once and then cleans itself up instead of lingering.
    /// </summary>
    public class SelfDestruct : MonoBehaviour
    {
        [SerializeField, Tooltip("Seconds before this object destroys itself.")]
        private float lifetime = 1f;

        /// <summary>Set the lifetime (e.g. to the animation length) before/at spawn.</summary>
        public float Lifetime { get => lifetime; set => lifetime = value; }

        private void Start() => Destroy(gameObject, lifetime);
    }
}
