using ChillZone.Gameplay;
using ChillZone.UI.Utils;
using UnityEngine;

namespace ChillZone.UI.Game
{
    /// <summary>
    /// World-space marker at the ball spawn point so the player can see where the ready ball rests (where to
    /// bring a ball to snap it). Self-contained — add it to a GameObject in the Game scene: it parents itself
    /// under the spawn anchor (<see cref="BallSpawnPoint"/>) so it follows the on-screen spawn spot, billboards
    /// toward the camera, and pulses its alpha.
    /// </summary>
    public class SpawnIndicator : MonoBehaviour
    {
        [Header("Appearance")]
        [SerializeField, Tooltip("Marker sprite. Leave empty to use the generated soft (feathered) circle.")]
        private Sprite sprite;
        [SerializeField, Tooltip("Marker tint. Alpha is driven by the pulse.")]
        private Color color = Color.white;
        [SerializeField, Tooltip("If 1f will match the spawn transform's size")]
        private float size = 0.8f;

        [Header("Alpha pulse")]
        [SerializeField, Tooltip("Pulse speed (alpha oscillation).")]
        private float pulseSpeed = 2f;
        [SerializeField, Range(0f, 1f), Tooltip("Alpha at the dim end of the pulse.")]
        private float minAlpha = 0.05f;
        [SerializeField, Range(0f, 1f), Tooltip("Alpha at the bright end of the pulse.")]
        private float maxAlpha = 0.2f;

        private SpriteRenderer _renderer;
        private Transform _spriteTransform;
        private Transform _anchor;

        private void Awake()
        {
            // Sprite on a child so sizing/anything visual never touches THIS object's transform.
            var spriteObject = new GameObject("SpawnIndicatorSprite");
            _spriteTransform = spriteObject.transform;
            _spriteTransform.SetParent(transform, false);

            _renderer = spriteObject.AddComponent<SpriteRenderer>();
            _renderer.sprite = sprite ? sprite : UIShapeFactory.SoftCircle();
            _renderer.color = color;
        }

        private void OnEnable() => TryAttachToSpawnPoint();

        private void LateUpdate()
        {
            if (!_anchor)
            {
                TryAttachToSpawnPoint();
                if (!_anchor) return; // spawn point not ready yet
            }

            var t = (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f;
            var c = color;
            c.a = Mathf.Lerp(minAlpha, maxAlpha, t);
            _renderer.color = c;
        }

        // Parent under the spawn anchor (so it follows the on-screen spot) and size the child sprite to the collider.
        private void TryAttachToSpawnPoint()
        {
            if (_anchor) return;
            var spawn = BallSpawnPoint.Instance;
            if (!spawn || !spawn.SpawnParent) return;

            _anchor = spawn.SpawnParent;
            transform.SetParent(_anchor, false);
            transform.localPosition = Vector3.zero;
            _spriteTransform.localScale = Vector3.one * size;
        }
    }
}
