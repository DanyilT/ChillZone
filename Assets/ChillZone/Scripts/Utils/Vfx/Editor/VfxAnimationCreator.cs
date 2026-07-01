#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using ChillZone.Game;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.UI;

namespace ChillZone.Utils.Vfx.Editor
{
    /// <summary>
    /// Select VFX spritesheet textures (or folders that contain them) and run the menu item to get
    /// ready-to-use VFX prefabs — no further steps. For every sheet it:
    ///   1. slices the texture into frame sprites (grid by the "_WxH" cell-size tag in the file name),
    ///   2. builds both a one-shot World clip + prefab and a looping UI clip + prefab,
    ///   3. saves the .anim / .controller / .prefab next to the sheet's folder (the "&lt;fps&gt;" folder, so the
    ///      sheet stays in its own Spritesheets subfolder).
    /// The clip frame rate follows the sheet's folder: a sheet under ".../30fps/..." → 30fps clips,
    /// ".../60fps/..." → 60fps clips.
    ///   • World prefab → SpriteRenderer + Animator + <see cref="Billboard"/> + <see cref="SelfDestruct"/>
    ///     (one-shot). Assign to BallData.hitVFXPrefab/missVFXPrefab; size is driven by RealWorldScaler.
    ///   • UI prefab → Image + Animator (loops). Assign to GameHUD.scoreFlashVfxTiers.
    /// Re-running regenerates the assets in place (existing ones at the same path are replaced).
    /// </summary>
    public static class VfxAnimationCreator
    {
        private const float WorldSizeMeters = 0.3f; // RealWorldScaler default; the spawner (e.g. BallData) overrides it
        private const float FallbackFps = 30f;      // used when the path has no "<n>fps" folder
        private const int SpritePixelsPerUnit = 100;
        private const int MaxSlicingTextureSize = 8192; // big enough that no source sheet downscales while slicing
        private const string Menu = "Tools/ChillZone/VFX/Slice Spritesheets → Create World + UI VFX";

        // Trailing "_305x383" cell-size tag the sheets are named with (Effect_Impact_1_305x383).
        private static readonly Regex CellSizeTag = new(@"_(\d+)x(\d+)$");
        // "30fps" / "60fps" folder anywhere in the asset path.
        private static readonly Regex FpsFolder = new(@"(\d+)fps", RegexOptions.IgnoreCase);

        [MenuItem(Menu)]
        private static void CreateFromSheets()
        {
            var sheets = GetSelectedSpritesheets();
            if (sheets.Count == 0)
            {
                Debug.LogWarning("[VfxAnimationCreator] Select one or more spritesheet textures named '<name>_WxH' " +
                                 "(e.g. Effect_Impact_1_305x383), or folders that contain them.");
                return;
            }

            var made = 0;
            try
            {
                for (var i = 0; i < sheets.Count; i++)
                {
                    EditorUtility.DisplayProgressBar("Create VFX from spritesheets", sheets[i], (float)i / sheets.Count);
                    if (CreatePairForSheet(sheets[i])) made++;
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            Debug.Log($"[VfxAnimationCreator] Done — created World + UI VFX for {made}/{sheets.Count} spritesheet(s).");
        }

        [MenuItem(Menu, true)]
        private static bool Validate() => Selection.objects.Any(IsSheetCandidate);

        // Slice one sheet and build its World (one-shot) and UI (looping) VFX. Returns false if skipped.
        private static bool CreatePairForSheet(string sheetPath)
        {
            var fileName = Path.GetFileNameWithoutExtension(sheetPath);
            var tag = CellSizeTag.Match(fileName);
            if (!tag.Success)
            {
                Debug.LogWarning($"[VfxAnimationCreator] Skipped '{sheetPath}': file name has no '_WxH' cell-size tag.");
                return false;
            }

            var cellW = int.Parse(tag.Groups[1].Value);
            var cellH = int.Parse(tag.Groups[2].Value);

            var sprites = SliceSheet(sheetPath, cellW, cellH);
            if (sprites.Count == 0)
            {
                Debug.LogWarning($"[VfxAnimationCreator] Skipped '{sheetPath}': slicing produced no sprites.");
                return false;
            }

            var fps = FpsFromPath(sheetPath);
            var baseName = CellSizeTag.Replace(fileName, "");  // Effect_Impact_1_305x383 → Effect_Impact_1
            var outDir = OutputDir(sheetPath);  // .../60fps (parent of the Spritesheets folder)

            CreateVfxAsset(sprites, fps, baseName, outDir, ui: false);
            CreateVfxAsset(sprites, fps, baseName, outDir, ui: true);
            return true;
        }

        // Import the sheet as Multiple and grid-slice it into cellW×cellH frames, top-left → right → down.
        // Fully transparent cells (the empty tail of a non-square grid) are skipped so the clip has no blank
        // frames. Read/Write is enabled only while we sample the cells, then turned back off.
        private static List<Sprite> SliceSheet(string sheetPath, int cellW, int cellH)
        {
            if (AssetImporter.GetAtPath(sheetPath) is not TextureImporter importer) return new List<Sprite>();

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = SpritePixelsPerUnit;
            importer.mipmapEnabled = false;
            importer.isReadable = true;  // needed to detect empty cells
            // The grid math below uses the source cell size, so the imported texture must keep its source
            // resolution. The default max (2048) downscales the big 60fps sheets, which shifted every cell and
            // mis-cut the frames ("bottom of a frame showing at the top"). Raise the cap so nothing downscales;
            // a per-platform override can still shrink it on device (sprite rects scale with the texture).
            importer.maxTextureSize = MaxSlicingTextureSize;
            importer.SaveAndReimport();

            var tex  = AssetDatabase.LoadAssetAtPath<Texture2D>(sheetPath);
            var cols = tex.width / cellW;
            var rows = tex.height / cellH;
            if (tex.width % cellW != 0 || tex.height % cellH != 0)
                Debug.LogWarning($"[VfxAnimationCreator] '{sheetPath}' ({tex.width}x{tex.height}) is not an exact " +
                                 $"multiple of the {cellW}x{cellH} cell — it may still be downscaled (source > " +
                                 $"{MaxSlicingTextureSize}px); frames may be mis-cut.");

            var metas = new List<SpriteMetaData>();
            var index = 0;
            for (var r = 0; r < rows; r++)        // row 0 = top
            for (var c = 0; c < cols; c++)
            {
                var x = c * cellW;
                var y = tex.height - (r + 1) * cellH; // texture Y is bottom-up
                if (IsCellEmpty(tex, x, y, cellW, cellH)) continue;

                metas.Add(new SpriteMetaData
                {
                    name = $"{Path.GetFileNameWithoutExtension(sheetPath)}_{index:000}",
                    rect = new Rect(x, y, cellW, cellH),
                    alignment = (int)SpriteAlignment.Center,
                    pivot = new Vector2(0.5f, 0.5f),
                });
                index++;
            }

#pragma warning disable 0618 // TextureImporter.spritesheet is obsolete but is the simplest reliable slicer in 2022.3
            importer.spritesheet = metas.ToArray();
#pragma warning restore 0618
            importer.isReadable = false;          // don't keep the sheet in CPU memory at runtime
            importer.SaveAndReimport();

            return AssetDatabase.LoadAllAssetsAtPath(sheetPath)
                .OfType<Sprite>()
                .OrderBy(s => s.name, Comparer<string>.Create(EditorUtility.NaturalCompare))
                .ToList();
        }

        private static bool IsCellEmpty(Texture2D tex, int x, int y, int w, int h)
        {
            foreach (var p in tex.GetPixels(x, y, w, h))
                if (p.a > 0f) return false;
            return true;
        }

        // Build / overwrite the .anim, .controller and .prefab for one variant (World one-shot or UI loop).
        private static void CreateVfxAsset(IReadOnlyList<Sprite> sprites, float fps, string baseName, string dir, bool ui)
        {
            // No cell-size in the asset names; fps number at the end (e.g. Effect_Impact_1_Vfx_60fps).
            var name = $"{baseName}_{(ui ? "UiVfx" : "Vfx")}_{Mathf.RoundToInt(fps)}fps";
            var clipLength = sprites.Count / fps;
            var clipPath = $"{dir}/{name}.anim";
            var ctrlPath = $"{dir}/{name}.controller";
            var prefabPath = $"{dir}/{name}.prefab";

            DeleteIfExists(clipPath);
            DeleteIfExists(ctrlPath);

            // UI flash VFX loops (shown while the flash text is up); world hit/miss VFX is one-shot.
            var clip = BuildClip(sprites, fps, ui ? typeof(Image) : typeof(SpriteRenderer), loop: ui);
            AssetDatabase.CreateAsset(clip, clipPath);

            var go = ui ? BuildUiObject(name, sprites[0]) : BuildWorldObject(name, sprites[0], clipLength);
            go.GetComponent<Animator>().runtimeAnimatorController = AnimatorController.CreateAnimatorControllerAtPathWithClip(ctrlPath, clip);

            // SaveAsPrefabAsset over an existing path overwrites the prefab but keeps its GUID, so scene/SO
            // references survive a re-run.
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
            UnityEngine.Object.DestroyImmediate(go);

            Debug.Log($"[VfxAnimationCreator] {prefab.name} — {sprites.Count} frames @ {fps}fps → {dir}", prefab);
        }

        // Clip that steps `propertyName` (m_Sprite) of `componentType` through the frames. An extra trailing
        // key holds the last frame for its full duration. Looping is per-use (UI loops, world plays once).
        private static AnimationClip BuildClip(IReadOnlyList<Sprite> sprites, float fps, Type componentType, bool loop)
        {
            var clip = new AnimationClip { frameRate = fps };
            var binding = new EditorCurveBinding { path = "", type = componentType, propertyName = "m_Sprite" };

            var keys = new ObjectReferenceKeyframe[sprites.Count + 1];
            for (var i = 0; i < sprites.Count; i++)
                keys[i] = new ObjectReferenceKeyframe { time = i / fps, value = sprites[i] };
            keys[sprites.Count] = new ObjectReferenceKeyframe { time = sprites.Count / fps, value = sprites[^1] };

            AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);

            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = loop;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            return clip;
        }

        private static GameObject BuildWorldObject(string name, Sprite first, float lifetime)
        {
            var go = new GameObject(name, typeof(SpriteRenderer), typeof(Animator), typeof(Billboard));
            go.GetComponent<SpriteRenderer>().sprite = first;

            // Real-world sizing is handled by RealWorldScaler (sprites otherwise render at their huge ppu size).
            // The scale is NOT hard-coded here — the script that spawns this prefab (e.g. BallBehaviour from
            // BallData.hitVFXSize) sets the size; this is just a sensible default for standalone use.
            go.AddComponent<RealWorldScaler>().SetTargetSize(WorldSizeMeters);

            // One-shot: the clip doesn't loop, so destroy the instance once it has played.
            go.AddComponent<SelfDestruct>().Lifetime = lifetime;
            return go;
        }

        private static GameObject BuildUiObject(string name, Sprite first)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Animator));
            var image = go.GetComponent<Image>();
            image.sprite = first;
            image.raycastTarget = false;
            image.preserveAspect = true; // fit within its rect without stretching the frames
            return go;
        }

        #region selection / path helpers

        private static List<string> GetSelectedSpritesheets()
        {
            var paths = new HashSet<string>();
            foreach (var obj in Selection.objects)
            {
                var p = AssetDatabase.GetAssetPath(obj);
                if (string.IsNullOrEmpty(p)) continue;

                if (AssetDatabase.IsValidFolder(p))
                {
                    foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { p }))
                    {
                        var tp = AssetDatabase.GUIDToAssetPath(guid);
                        if (IsSheetPath(tp)) paths.Add(tp);
                    }
                }
                else if (obj is Texture2D && IsSheetPath(p))
                {
                    paths.Add(p);
                }
            }

            return paths.OrderBy(p => p, Comparer<string>.Create(EditorUtility.NaturalCompare)).ToList();
        }

        private static bool IsSheetCandidate(UnityEngine.Object o)
        {
            var p = AssetDatabase.GetAssetPath(o);
            if (string.IsNullOrEmpty(p)) return false;
            return AssetDatabase.IsValidFolder(p) || (o is Texture2D && IsSheetPath(p));
        }

        private static bool IsSheetPath(string path) =>
            CellSizeTag.IsMatch(Path.GetFileNameWithoutExtension(path));

        private static float FpsFromPath(string path)
        {
            var m = FpsFolder.Match(path.Replace('\\', '/'));
            return m.Success && float.TryParse(m.Groups[1].Value, out var fps) && fps > 0f ? fps : FallbackFps;
        }

        // Sheets live in ".../<fps>/Spritesheets" — write the generated assets one level up (the "<fps>"
        // folder) so the sheet keeps its own Spritesheets folder. If there's no Spritesheets folder, write
        // next to the sheet.
        private static string OutputDir(string sheetPath)
        {
            var dir = Path.GetDirectoryName(sheetPath)?.Replace('\\', '/') ?? "Assets";
            if (string.Equals(Path.GetFileName(dir), "Spritesheets", StringComparison.OrdinalIgnoreCase))
                dir = Path.GetDirectoryName(dir)?.Replace('\\', '/') ?? dir;
            return dir;
        }

        private static void DeleteIfExists(string assetPath)
        {
            if (!string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(assetPath)))
                AssetDatabase.DeleteAsset(assetPath);
        }

        #endregion
    }
}
#endif
