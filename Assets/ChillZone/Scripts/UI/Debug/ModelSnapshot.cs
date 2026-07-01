using UnityEngine;
using System.IO;

namespace ChillZone.Tools
{
    // ModelSnapshot > Camera > Model
    // Display 512:512
    public class ModelSnapshot : MonoBehaviour
    {
        [SerializeField] private Camera snapshotCamera;
        [SerializeField] private int size = 512;
        [SerializeField] private string fileName = "icon.png";

        [ContextMenu("Take Snapshot")]
        public void TakeSnapshot()
        {
            var rt = new RenderTexture(size, size, 24, RenderTextureFormat.ARGB32)
            {
                antiAliasing = 8
            };

            var prevTarget = snapshotCamera.targetTexture;
            var prevActive = RenderTexture.active;

            snapshotCamera.clearFlags = CameraClearFlags.SolidColor;
            snapshotCamera.backgroundColor = new Color(0, 0, 0, 0);
            snapshotCamera.targetTexture = rt;
            snapshotCamera.Render();

            RenderTexture.active = rt;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.ReadPixels(new Rect(0, 0, size, size), 0, 0);
            tex.Apply();

            snapshotCamera.targetTexture = prevTarget;
            RenderTexture.active = prevActive;

            var bytes = tex.EncodeToPNG();
            var path = Path.Combine(Application.dataPath, fileName);
            File.WriteAllBytes(path, bytes);
            Debug.Log($"Saved snapshot to {path}");

            DestroyImmediate(tex);
            rt.Release();
            DestroyImmediate(rt);
        }
    }
}
