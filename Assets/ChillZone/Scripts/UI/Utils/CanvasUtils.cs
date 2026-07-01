using System;
using ChillZone.UI.Window.Utils;
using UnityEngine;
using UnityEngine.UI;

namespace ChillZone.UI.Utils
{
    /// <summary>
    /// Creates and configures self-contained UI canvases from a preset. Used by anything that wants its
    /// OWN canvas (ButtonManager, AdaptiveHeader, …) so it can be sorted / shown / hidden independently.
    /// </summary>
    public static class CanvasUtils
    {
        /// <summary>Pick-list of ready-made canvas presets (no further config needed).</summary>
        public enum Preset
        {
            Overlay,
            Camera,
        }

        public static CanvasPreset Get(Preset preset) => preset switch
        {
            Preset.Camera => CanvasPreset.Camera(),
            _ => CanvasPreset.Overlay(),
        };

        [Serializable]
        public struct CanvasPreset
        {
            [Tooltip("ScreenSpaceOverlay for a normal HUD; ScreenSpaceCamera to place it in front of a camera.")]
            public RenderMode renderMode;
            [Tooltip("Higher = drawn on top.")]
            public int sortingOrder;
            [Tooltip("CanvasScaler reference resolution.")]
            public Vector2 referenceResolution;
            [Tooltip("CanvasScaler match width-or-height."), Range(0f, 1f)]
            public float matchWidthOrHeight;
            [Tooltip("ScreenSpaceCamera only. Empty = Camera.main.")]
            public Camera renderCamera;
            [Tooltip("ScreenSpaceCamera only. 0 = default (10).")]
            public float planeDistance;

            /// <summary>Standard full-screen overlay HUD.</summary>
            public static CanvasPreset Overlay(int sortingOrder = 0) => new()
            {
                renderMode = RenderMode.ScreenSpaceOverlay,
                sortingOrder = sortingOrder,
                referenceResolution = new Vector2(1080f, 1920f),
                matchWidthOrHeight = 0.5f,
            };

            /// <summary>Screen-space camera canvas — defaults to Camera.main at a plane distance of 10.</summary>
            public static CanvasPreset Camera(int sortingOrder = 0, Camera camera = null) => new()
            {
                renderMode = RenderMode.ScreenSpaceCamera,
                sortingOrder = sortingOrder,
                referenceResolution = new Vector2(1080f, 1920f),
                matchWidthOrHeight = 0.5f,
                renderCamera = camera ?? UnityEngine.Camera.main,
                planeDistance = 10f,
            };
        }

        /// <summary>Create a child canvas using a ready-made <see cref="Preset"/>.</summary>
        public static Canvas CreateChildCanvas(Transform parent, string name, Preset preset) => CreateChildCanvas(parent, name, Get(preset));

        /// <summary>Apply a ready-made <see cref="Preset"/> to an existing canvas.</summary>
        public static void Apply(Canvas canvas, Preset preset) => Apply(canvas, Get(preset));

        /// <summary>Create a child canvas (Canvas + CanvasScaler + GraphicRaycaster) and apply <paramref name="preset"/>.</summary>
        public static Canvas CreateChildCanvas(Transform parent, string name, CanvasPreset preset)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            go.transform.SetParent(parent, false);
            var canvas = go.GetComponent<Canvas>();
            Apply(canvas, preset);
            return canvas;
        }

        /// <summary>Apply a preset to an existing canvas (+ its scaler) and stretch its rect to fill the parent.</summary>
        public static void Apply(Canvas canvas, CanvasPreset preset)
        {
            // ScreenSpaceCamera defaults: Camera.main + distance 10 (resolved inside SetupCanvas / here).
            var canvasScaler = canvas.GetComponent<CanvasScaler>() ?? canvas.gameObject.AddComponent<CanvasScaler>();
            var distance = preset.planeDistance > 0f ? preset.planeDistance : 10f;
            RenderUtils.SetupCanvas(canvas, canvasScaler, preset.renderMode, preset.renderCamera, distance, preset.referenceResolution, preset.matchWidthOrHeight, preset.sortingOrder);

            // Only a NESTED canvas needs stretching to fill its parent — without it it keeps a zero/default
            // rect and nothing shows. A ROOT canvas is auto-sized to the screen by Unity (its RectTransform
            // is driven); touching it there breaks that sizing, so leave it alone.
            if (!canvas.isRootCanvas)
            {
                var rt = (RectTransform)canvas.transform;
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.sizeDelta = Vector2.zero;
                rt.anchoredPosition = Vector2.zero;
            }
        }
    }
}
