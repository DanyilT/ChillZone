using UnityEngine;
using UnityEngine.EventSystems;
using ChillZone.Ball;
using ChillZone.Core;

namespace ChillZone.Gameplay
{
    [RequireComponent(typeof(Transform)), RequireComponent(typeof(CircleCollider2D))]
    public class BallSpawnPoint : MonoBehaviour
    {
        public static BallSpawnPoint Instance { get; private set; }

        [Tooltip("Leave to ZERO to automatically calculate the best spawn position")]
        [SerializeField] private Vector3 spawnPosition;
        [Tooltip("Distance in front of the camera to spawn the ball when auto-calculating (in meters)")]
        [SerializeField] private float spawnDistance = 1f;
        [Tooltip("The target camera to use for this ball spawn point. If empty, will find main camera")]
        [SerializeField] private Camera targetCamera;
        [Tooltip("Balls pool tag. Optional pool root for thrown balls. If no objects with the tag found, one will be created at runtime.")]
        [SerializeField] private  string poolTag = GameTags.BallPool;
        [Tooltip("Keep the ready (un-thrown) ball glued to its on-screen spot by re-anchoring the spawn point to the camera each frame. Disable to leave the ball fixed in AR world space once spawned.")]
        [SerializeField] private bool attachBallToScreen = true;

        private void Awake()
        {
            Instance = this;

            if (SpawnCollider != null) SpawnCollider.isTrigger = true;
            if (targetCamera == null) targetCamera = Camera.main;
            if (targetCamera != null && targetCamera.GetComponent<PhysicsRaycaster>() == null)
                targetCamera.gameObject.AddComponent<PhysicsRaycaster>();

            var poolObject = GameObject.FindWithTag(poolTag) ?? new GameObject(poolTag) { tag = poolTag };
            PoolParent = poolObject.transform;
            SpawnParent = transform;
        }

        // Keeps the spawn anchor (and the ready ball parented under it) glued to the screen as the
        // device moves, instead of sticking to a fixed point in AR world space. The thrown ball is
        // un-parented to the pool, so this never disturbs a ball in flight.
        private void LateUpdate()
        {
            if (!attachBallToScreen) return;

            var cam = ResolveCamera();
            if (!cam) return;

            // While the player is dragging the held ball to aim, the finger owns it
            // (BallController writes its world position), so leave the anchor alone.
            var ball = BallSpawnManager.Instance ? BallSpawnManager.Instance.ActiveBall : null;
            if (ball && ball.TryGetComponent<BallController>(out var drag) && drag.IsDragging) return;

            AttachToCamera(cam);
        }

        // Rigidly parents the spawn anchor to the camera so it (and the ready ball) follow the device
        // at render time with zero lag — far smoother than rewriting the world position each frame,
        // which trails the AR camera's late pose update and looks glitchy. The local scale is
        // neutralised so a scaled camera rig (e.g. a 0.3 XR Origin) doesn't shrink the ball.
        private void AttachToCamera(Camera cam)
        {
            var camTransform = cam.transform;
            if (transform.parent != camTransform)
            {
                transform.SetParent(camTransform, false);
                var camScale = camTransform.lossyScale;
                transform.localScale = new Vector3(SafeInverse(camScale.x), SafeInverse(camScale.y), SafeInverse(camScale.z));
            }

            transform.localPosition = camTransform.InverseTransformPoint(ResolveWorldSpawnPosition(cam));
            transform.localRotation = Quaternion.identity;
        }

        private static float SafeInverse(float value) => Mathf.Approximately(value, 0f) ? 1f : 1f / value;

        public Vector3 Position
        {
            get
            {
                var cam = ResolveCamera();

                // No camera → don't teleport the ball to world origin; keep it at the spawn transform.
                if (!cam)
                    return SpawnParent ? SpawnParent.position : transform.position;

                var worldPos = ResolveWorldSpawnPosition(cam);
                // Update the transform so gizmos and parenting remain consistent.
                if (SpawnParent) SpawnParent.position = worldPos;
                transform.position = worldPos;
                return worldPos;
            }
        }

        // The camera-relative world point where the ball rests: a fixed viewport spot (auto mode,
        // when spawnPosition is zero) or the configured screen position. Pure — no transform writes,
        // so both the Position getter and the per-frame LateUpdate re-anchor can share it.
        private Vector3 ResolveWorldSpawnPosition(Camera cam)
        {
            // Auto: center horizontally, lower-half vertically, spawnDistance metres in front.
            if (spawnPosition == Vector3.zero)
                return cam.ViewportToWorldPoint(new Vector3(0.5f, 0.25f, spawnDistance));

            // Explicit screen-pixel position (z treated as distance from the camera).
            var screenPos = spawnPosition;
            if (screenPos.z <= 0f)
                screenPos.z = spawnDistance;
            return cam.ScreenToWorldPoint(screenPos);
        }

        // Camera.main can be null mid-session in AR; fall back to any camera in the scene.
        private Camera ResolveCamera()
        {
            if (targetCamera) return targetCamera;
            targetCamera = Camera.main;
            if (!targetCamera) targetCamera = FindObjectOfType<Camera>();
            return targetCamera;
        }

        public Quaternion Rotation => SpawnParent != null ? SpawnParent.rotation : Quaternion.identity;

        // Call this from throw mechanic; parent to spawn transform so spawned ball appears at the intended local frame
        public GameObject SpawnCurrentBall() => BallSpawnManager.Instance.SpawnBall(Position, SpawnParent);

        public Transform SpawnParent { get; private set; }

        public Transform PoolParent { get; private set; }

        public CircleCollider2D SpawnCollider => GetComponent<CircleCollider2D>();

        public bool IsInsideSpawnTrigger(Vector3 worldPosition) => SpawnCollider != null && SpawnCollider.bounds.Contains(worldPosition);

#if UNITY_EDITOR
        [Header("Ball Spawn Point Gizmo")]
        [SerializeField] private bool visualizeInEditor = true;
        [SerializeField] private Color gizmoColor = Color.cyan;
        [SerializeField] private float gizmoRadius = 1f;

        private void OnDrawGizmos()
        {
            if (!visualizeInEditor) return;
            Gizmos.color = gizmoColor;
            Gizmos.DrawWireSphere(transform.position, gizmoRadius);
        }
#endif
    }
}
