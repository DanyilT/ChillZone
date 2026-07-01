using System.Collections.Generic;
using UnityEngine;

namespace ChillZone.UI.Utils
{
    /// <summary>
    /// Generates simple UI background sprites at runtime (no sprite assets needed): a rounded
    /// rectangle or a circle, drawn white so the Image's tint color shows through. Rounded-rect
    /// sprites carry a 9-slice border (= the radius) so they scale to any button size without
    /// distorting the corners. Generated sprites are cached and reused.
    /// </summary>
    public static class UIShapeFactory
    {
        private static readonly Dictionary<int, Sprite> RoundedCache = new();
        private static Sprite _circle;
        private static Sprite _softCircle;

        /// <summary>White rounded-rect sprite (9-sliced by <paramref name="cornerRadius"/>); use with Image.Type.Sliced.</summary>
        public static Sprite RoundedRect(float cornerRadius)
        {
            var radius = Mathf.Max(1, Mathf.RoundToInt(cornerRadius));
            if (RoundedCache.TryGetValue(radius, out var cached) && cached) return cached;

            var size = radius * 2 + 2; // two corner arcs + a 2px centre that the 9-slice stretches
            var tex = NewTexture(size);
            var pixels = new Color32[size * size];

            for (int y = 0; y < size; y++)
            {
                var cy = Mathf.Clamp(y, radius, size - 1 - radius);
                for (int x = 0; x < size; x++)
                {
                    var cx = Mathf.Clamp(x, radius, size - 1 - radius);
                    var dist = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
                    var a = (byte)(Mathf.Clamp01(radius - dist + 0.5f) * 255f); // 1px anti-aliased edge
                    pixels[y * size + x] = new Color32(255, 255, 255, a);
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply();

            var border = new Vector4(radius, radius, radius, radius);
            var sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border);
            sprite.name = $"RoundedRect_{radius}";
            RoundedCache[radius] = sprite;
            return sprite;
        }

        /// <summary>White circle sprite; use with Image.Type.Simple on a square button.</summary>
        public static Sprite Circle()
        {
            if (_circle) return _circle;

            const int size = 128;
            const float r = size * 0.5f;
            var tex = NewTexture(size);
            var pixels = new Color32[size * size];

            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                var dist = Mathf.Sqrt((x + 0.5f - r) * (x + 0.5f - r) + (y + 0.5f - r) * (y + 0.5f - r));
                var a = (byte)(Mathf.Clamp01(r - dist) * 255f);
                pixels[y * size + x] = new Color32(255, 255, 255, a);
            }

            tex.SetPixels32(pixels);
            tex.Apply();

            _circle = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
            _circle.name = "Circle";
            return _circle;
        }

        /// <summary>White circle with a solid core that feathers to transparent at the edge — a soft, non-sharp disc/glow.</summary>
        public static Sprite SoftCircle()
        {
            if (_softCircle) return _softCircle;

            const int size = 128;
            const float r = size * 0.5f;
            var tex = NewTexture(size);
            var pixels = new Color32[size * size];

            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                var dist = Mathf.Sqrt((x + 0.5f - r) * (x + 0.5f - r) + (y + 0.5f - r) * (y + 0.5f - r));
                var d = dist / r;  // 0 at centre → 1 at the edge
                var a = (byte)((1f - Mathf.SmoothStep(0.55f, 1f, d)) * 255f);  // solid core, feathered edge
                pixels[y * size + x] = new Color32(255, 255, 255, a);
            }

            tex.SetPixels32(pixels);
            tex.Apply();

            _softCircle = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
            _softCircle.name = "SoftCircle";
            return _softCircle;
        }

        private static Texture2D NewTexture(int size) => new(size, size, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear,
        };
    }
}
