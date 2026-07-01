using UnityEngine;

namespace ChillZone.Ball
{
    [CreateAssetMenu(fileName = "Ball_New", menuName = "ChillZone/Content/Ball Data")]
    public class BallData : Content.UnlockableContent
    {
        public override Content.ContentTypes ContentType => Content.ContentTypes.Ball;

        #region Visuals

        [Header("Ball Visuals")]
        [Tooltip("The actual 3D ball prefab.")]
        public GameObject prefab;

        [Tooltip("Real-world size (metres, longest axis) this ball is scaled to at spawn. 0 = use BallSpawnManager's default. Use this to fix balls whose model auto-scales wrong (e.g. the tennis ball).")]
        public float realWorldSizeMeters = 0f;

        #endregion

        #region Effects

        [Header("Hit / Miss VFX")]
        [Tooltip("VFX prefab spawned at the basket on a score. Use a prefab with a SpriteRenderer + Animator (sprite animation) + Billboard for a frame animation.")]
        public GameObject hitVFXPrefab;
        [Tooltip("Real-world size (metres, longest axis) the hit VFX is scaled to via its RealWorldScaler. The prefab's own default is overridden by this.")]
        public float hitVFXSize = 0.8f;
        [Tooltip("VFX prefab spawned at the impact point on a miss.")]
        public GameObject missVFXPrefab;
        [Tooltip("Real-world size (metres, longest axis) the miss VFX is scaled to via its RealWorldScaler. The prefab's own default is overridden by this.")]
        public float missVFXSize = 0.3f;

        [Header("Flight Trail (generated in code)")]
        [Tooltip("A TrailRenderer is built from these settings while the ball is in flight — no prefab needed.")]
        public BallTrailSettings trail = new BallTrailSettings();

        #endregion

        #region Audio

        [Header("Audio")]
        public AudioClip throwSound;
        public AudioClip hitSound;
        public AudioClip missSound;

        #endregion

        #region Physics

        [Header("Physics Overrides")]  // optional per-ball feel tuning
        public float mass = 1f;
        public float bounciness = 0.3f;
        public float drag = 0.1f;

        #endregion

#if UNITY_EDITOR
        private void Reset() => id = "ball-";
#endif
    }
}
