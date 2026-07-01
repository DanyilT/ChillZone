using ChillZone.Core;
using UnityEngine;

namespace ChillZone.Utils.Vfx
{
    /// <summary>
    /// Rotates this object to face the active camera every frame. Put it on a world-space sprite/quad VFX
    /// prefab (e.g. an Animator-driven frame animation assigned to BallData.hitVFXPrefab/missVFXPrefab) so it
    /// stays face-on in AR. Unity has no built-in billboard, so this is the one small helper that remains.
    /// </summary>
    public class Billboard : MonoBehaviour
    {
        private void LateUpdate()
        {
            var cam = CameraProvider.Current;
            if (cam) transform.rotation = Quaternion.LookRotation(cam.transform.forward, cam.transform.up);
        }
    }
}
