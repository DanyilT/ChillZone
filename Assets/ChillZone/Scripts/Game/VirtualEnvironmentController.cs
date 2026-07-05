using System.Collections.Generic;
using ChillZone.Core;
using UnityEngine;
using UnityEngine.InputSystem.XR;
using UnityEngine.XR.ARFoundation;

namespace ChillZone.Game
{
    /// <summary>
    /// Owns the virtual (camera-off) environment: hides the AR camera feed, shows a solid/skybox
    /// background and a code-generated BOX (floor + four walls, optional ceiling) centred on the origin,
    /// and parks the camera at a fixed vantage inside it. The walls carry bouncy colliders so thrown balls
    /// rebound into the play area instead of flying off. Toggled by GameFlowController; lets the game be
    /// played on devices without ARCore (and as an opt-in mode on ones that support it).
    ///
    /// Place this on a ROOT GameObject in the Game scene (used as a child of AR_Rig prefab).
    /// </summary>
    public class VirtualEnvironmentController : MonoBehaviour
    {
        [Header("Camera")]
        [SerializeField, Tooltip("AR camera. If empty, resolved via CameraProvider at enable time.")]
        private Camera arCamera;
        [SerializeField, Tooltip("Use the scene skybox as the background instead of a solid colour.")]
        private bool useSkybox;
        [SerializeField, Tooltip("Background colour when not using the skybox.")]
        private Color backgroundColor = new (0.16f, 0.18f, 0.22f, 1f);
        [SerializeField, Tooltip("Fixed camera position (world space) while virtual mode is on — the device pose isn't tracked here, so the view is static. Tune so the box is nicely framed.")]
        private Vector3 cameraPosition = new (0f, 1.5f, -1.5f);
        [SerializeField, Tooltip("Fixed camera rotation (euler, world space). Default looks forward and slightly down at the floor.")]
        private Vector3 cameraEuler = new (20f, 0f, 0f);

        [Header("Box")]
        [SerializeField, Tooltip("Floor size in metres (square), centred on the origin — also the box footprint.")]
        private float groundSize = 10f;
        [SerializeField, Tooltip("Wall height in metres (box height).")]
        private float wallHeight = 8f;
        [SerializeField, Tooltip("Wall thickness in metres.")]
        private float wallThickness = 0.3f;
        [SerializeField, Tooltip("Add a ceiling to fully close the box. Off by default so high-arc shots aren't clipped.")]
        private bool includeCeiling;

        [Header("Surfaces")]
        [SerializeField, Tooltip("Material for the floor/walls (e.g. the AR dot-plane material M_AR_DotPlane). If empty, the primitive default is kept.")]
        private Material groundMaterial;
        [SerializeField, Tooltip("Optional separate material for the walls/ceiling. If empty, the floor material is used.")]
        private Material wallMaterial;
        [SerializeField, Tooltip("World size (metres) of one repeat of the surface texture — smaller = more, tinier dots. Fixes the 'dots too big' stretch on the large surfaces.")]
        private float dotWorldSize = 0.5f;
        [SerializeField, Range(0f, 1f), Tooltip("Wall/ceiling bounciness so thrown balls rebound into the play area.")]
        private float wallBounciness = 0.6f;

        private GameObject _environment;
        private ARCameraBackground _cameraBackground;
        private TrackedPoseDriver _poseDriver;

        // Runtime-created assets to clean up (per-surface material instances + the shared bouncy physic material).
        private readonly List<Object> _createdAssets = new();

        // Cached AR-mode camera state, restored when virtual mode turns off.
        private CameraClearFlags _cachedClearFlags;
        private Color _cachedBackgroundColor;
        private bool _cachedCameraBackgroundEnabled;
        private bool _active;

        #region public api

        /// <summary>Turns the virtual environment on/off. On: hides the AR feed, shows the box + background, parks the camera. Off: restores AR rendering + tracking.</summary>
        public void SetEnabled(bool on)
        {
            if (on == _active) return;

            var cam = ResolveCamera();
            if (!cam) return;

            if (on) Enable(cam);
            else Disable(cam);
            _active = on;
        }

        /// <summary>Half the floor footprint (metres) minus a small margin — used to clamp basket placement/move so the basket stays on the ground (off the walls) in virtual mode.</summary>
        public float GroundHalfExtent => Mathf.Max(0f, groundSize * 0.5f - 0.5f);

        #endregion

        #region lifecycle

        private void OnDestroy()
        {
            if (_environment) Destroy(_environment);
            foreach (var asset in _createdAssets)
                if (asset) Destroy(asset);
            _createdAssets.Clear();
        }

        #endregion

        #region camera (internal)

        private void Enable(Camera cam)
        {
            // Hide the AR camera feed and show a background colour/skybox in its place.
            _cameraBackground = cam.GetComponent<ARCameraBackground>();
            if (_cameraBackground)
            {
                _cachedCameraBackgroundEnabled = _cameraBackground.enabled;
                _cameraBackground.enabled = false;
            }

            _cachedClearFlags = cam.clearFlags;
            _cachedBackgroundColor = cam.backgroundColor;
            cam.clearFlags = useSkybox ? CameraClearFlags.Skybox : CameraClearFlags.SolidColor;
            if (!useSkybox) cam.backgroundColor = backgroundColor;

            // The device pose isn't driving the camera in virtual mode, so disable the pose driver (it would
            // otherwise write a zero/last pose once the AR session stops) and park the camera at a fixed vantage.
            _poseDriver = cam.GetComponent<TrackedPoseDriver>();
            if (_poseDriver) _poseDriver.enabled = false;
            cam.transform.SetPositionAndRotation(cameraPosition, Quaternion.Euler(cameraEuler));

            EnsureEnvironment();
            _environment.SetActive(true);
        }

        private void Disable(Camera cam)
        {
            if (_cameraBackground) _cameraBackground.enabled = _cachedCameraBackgroundEnabled;
            cam.clearFlags = _cachedClearFlags;
            cam.backgroundColor = _cachedBackgroundColor;
            if (_poseDriver) _poseDriver.enabled = true;  // AR tracking drives the camera again
            if (_environment) _environment.SetActive(false);
        }

        private Camera ResolveCamera()
        {
            if (!arCamera) arCamera = CameraProvider.Current;
            return arCamera;
        }

        #endregion

        #region box (internal)

        // Builds the box once (kept at the scene root so a scaled XR Origin can't shrink it): a floor plane and
        // four walls (+ optional ceiling), each a solid collider so thrown balls collide (a miss) and — on the
        // walls — bounce back into the play area. Reused across toggles.
        private void EnsureEnvironment()
        {
            if (_environment) return;

            _environment = new GameObject("VirtualEnvironment");
            _environment.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

            var bouncy = new PhysicMaterial("VirtualWallMat") { bounciness = wallBounciness, bounceCombine = PhysicMaterialCombine.Maximum };
            _createdAssets.Add(bouncy);

            var walls = wallMaterial ? wallMaterial : groundMaterial;
            var half = groundSize * 0.5f;

            BuildFloor();
            BuildWall("Wall+X", new Vector3(half, wallHeight * 0.5f, 0f),  new Vector3(wallThickness, wallHeight, groundSize), groundSize, wallHeight, walls, bouncy);
            BuildWall("Wall-X", new Vector3(-half, wallHeight * 0.5f, 0f), new Vector3(wallThickness, wallHeight, groundSize), groundSize, wallHeight, walls, bouncy);
            BuildWall("Wall+Z", new Vector3(0f, wallHeight * 0.5f, half),  new Vector3(groundSize, wallHeight, wallThickness), groundSize, wallHeight, walls, bouncy);
            BuildWall("Wall-Z", new Vector3(0f, wallHeight * 0.5f, -half), new Vector3(groundSize, wallHeight, wallThickness), groundSize, wallHeight, walls, bouncy);

            if (includeCeiling)
                BuildWall("Ceiling", new Vector3(0f, wallHeight, 0f), new Vector3(groundSize, wallThickness, groundSize), groundSize, groundSize, walls, bouncy);

            _environment.SetActive(false);
        }

        // Floor — a Plane (up-facing) so the ball settles on it like the real floor; no bounce material.
        private void BuildFloor()
        {
            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Floor";
            floor.transform.SetParent(_environment.transform, false);
            floor.transform.localScale = Vector3.one * (groundSize / 10f); // a Unity Plane is 10×10 units
            ApplyTiledMaterial(floor, groundMaterial, groundSize, groundSize);
        }

        // Wall/ceiling — a thin Cube with a solid BoxCollider (reliable against fast balls, unlike a flat plane)
        // plus the bouncy physic material so the ball rebounds inward.
        private void BuildWall(string wallName, Vector3 localPosition, Vector3 localScale, float faceW, float faceH, Material material, PhysicMaterial physic)
        {
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = wallName;
            wall.transform.SetParent(_environment.transform, false);
            wall.transform.localPosition = localPosition;
            wall.transform.localScale = localScale;

            if (wall.TryGetComponent<Collider>(out var wallCollider)) wallCollider.material = physic;
            wall.AddComponent<BounceWall>();  // rebounds the ball without a miss; a score after a bounce earns the wall-bounce multiplier
            ApplyTiledMaterial(wall, material, faceW, faceH);
        }

        // Assigns a per-surface material INSTANCE (never the shared asset, which real AR planes use) whose texture
        // tiling is set from the surface's world size, so the dot texture repeats every ~dotWorldSize metres
        // instead of a single stretched copy across the whole surface (the "dots too big" fix).
        private void ApplyTiledMaterial(GameObject surface, Material source, float worldW, float worldH)
        {
            if (!source || !surface.TryGetComponent<MeshRenderer>(out var meshRenderer)) return;

            var instance = new Material(source);
            var tile = Mathf.Max(0.0001f, dotWorldSize);
            instance.mainTextureScale = new Vector2(worldW / tile, worldH / tile);
            meshRenderer.sharedMaterial = instance;
            _createdAssets.Add(instance);
        }

        #endregion
    }
}
