using UnityEngine;
using UnityEngine.UI;
using ChillZone.Utils.Native;

namespace ChillZone.UI.Helpers
{
    /// <summary>
    /// Converts between screen pixels and canvas units, and exposes safe-area measurements.
    /// All methods accept optional Canvas / CanvasScaler — if null, they search the scene.
    /// </summary>
    public static class CanvasScalerHelper
    {
        #region pixel ↔ canvas-unit conversion

        public static float PixelsToCanvasUnits(float pixels, Canvas canvas = null, CanvasScaler scaler = null)
        {
            EnsureScaler(ref canvas, ref scaler);
            if (!scaler) return pixels;

            return scaler.uiScaleMode switch
            {
                CanvasScaler.ScaleMode.ScaleWithScreenSize =>
                    pixels / ScaleFactor(scaler.screenMatchMode, scaler.referenceResolution, scaler.matchWidthOrHeight),
                CanvasScaler.ScaleMode.ConstantPixelSize =>
                    pixels / scaler.scaleFactor,
                CanvasScaler.ScaleMode.ConstantPhysicalSize =>
                    pixels / scaler.scaleFactor,
                _ => pixels
            };
        }

        public static float CanvasUnitsToPixels(float units, Canvas canvas = null, CanvasScaler scaler = null)
        {
            EnsureScaler(ref canvas, ref scaler);
            return units * GetScaleFactor(canvas, scaler);
        }

        public static float GetScaleFactor(Canvas canvas = null, CanvasScaler scaler = null)
        {
            EnsureScaler(ref canvas, ref scaler);
            if (!canvas) return 1f;
            if (!scaler) return canvas.scaleFactor;
            return scaler.uiScaleMode == CanvasScaler.ScaleMode.ScaleWithScreenSize
                ? ScaleFactor(scaler.screenMatchMode, scaler.referenceResolution, scaler.matchWidthOrHeight)
                : scaler.scaleFactor;
        }

        public static float ScaleFactor(CanvasScaler.ScreenMatchMode mode, Vector2 refRes, float match) =>
            mode switch
            {
                CanvasScaler.ScreenMatchMode.MatchWidthOrHeight =>
                    Mathf.Lerp(Screen.width / refRes.x, Screen.height / refRes.y, match),
                CanvasScaler.ScreenMatchMode.Expand =>
                    Mathf.Min(Screen.width / refRes.x, Screen.height / refRes.y),
                CanvasScaler.ScreenMatchMode.Shrink =>
                    Mathf.Max(Screen.width / refRes.x, Screen.height / refRes.y),
                _ => Mathf.Lerp(Screen.width / refRes.x, Screen.height / refRes.y, match),
            };

        #endregion

        #region safe area measurements (canvas units)

        public static Rect SafeArea(Canvas canvas = null)
        {
            var safe = Screen.safeArea;
            if (!canvas) canvas = Object.FindObjectOfType<Canvas>();
            if (!canvas) return safe;

            // rect.size, NOT sizeDelta: a stretched canvas (anchored to fill) has sizeDelta 0 but a real
            // rect — using sizeDelta there returns a zero safe area and collapses dependent layouts.
            var sz = canvas.GetComponent<RectTransform>().rect.size;
            var sx = sz.x / Screen.width;
            var sy = sz.y / Screen.height;
            return new Rect(safe.x * sx, safe.y * sy, safe.width * sx, safe.height * sy);
        }

        public static float StatusBarHeight(Canvas canvas = null, CanvasScaler scaler = null) =>
            PixelsToCanvasUnits(Screen.height - Screen.safeArea.yMax, canvas, scaler);

        // Screen.safeArea omits the Android nav bar, so fall back to the real nav-bar height when larger.
        public static float NavigationBarHeight(Canvas canvas = null, CanvasScaler scaler = null) =>
            PixelsToCanvasUnits(Mathf.Max(Screen.safeArea.y, AndroidSystemBars.NavigationBarHeightPx), canvas, scaler);

        public static float LeftCutout(Canvas canvas = null, CanvasScaler scaler = null) =>
            PixelsToCanvasUnits(Screen.safeArea.x, canvas, scaler);

        public static float RightCutout(Canvas canvas = null, CanvasScaler scaler = null) =>
            PixelsToCanvasUnits(Screen.width - Screen.safeArea.xMax, canvas, scaler);

        #endregion

        #region internal

        private static void EnsureScaler(ref Canvas canvas, ref CanvasScaler scaler)
        {
            if (!canvas) canvas = Object.FindObjectOfType<Canvas>();
            if (canvas && !scaler) scaler = canvas.GetComponent<CanvasScaler>();
        }

        #endregion

        #region debug

        /// <summary> Logs debug information about canvas scaling</summary>
        /// <remarks>Useful for debugging and verifying that conversions are working correctly, especially on devices with cutouts/notches</remarks>
        public static void ShowInfo(Canvas canvas = null, CanvasScaler scaler = null)
        {
            Debug.Log("=== CanvasScalerHelper Debug Info ===");
            Debug.Log($"Screen Size: {Screen.width}x{Screen.height}\n" +
                      $"Safe Area: {SafeArea()}");

            EnsureScaler(ref canvas, ref scaler);
            if (!canvas)
                Debug.LogWarning("[CanvasScalerHelper] No Canvas found");
            if (!scaler)
                Debug.LogWarning("[CanvasScalerHelper] No CanvasScaler found on Canvas");
    
            if (!canvas) return;
            Debug.Log("--- Canvas Info ---\n" +
                      $"Canvas Size: {canvas.GetComponent<RectTransform>().sizeDelta}\n" +
                      $"Status Bar Height (canvas units): {StatusBarHeight(canvas, scaler)}\n" +
                      $"Navigation Bar Height (canvas units): {NavigationBarHeight(canvas, scaler)}\n" +
                      $"Left Cutout (canvas units): {LeftCutout(canvas, scaler)}\n" +
                      $"Right Cutout (canvas units): {RightCutout(canvas, scaler)}");

            if (!scaler) return;
            Debug.Log("--- Scaler Info ---\n" +
                      $"Scale Factor: {GetScaleFactor(canvas, scaler)}\n" +
                      $"Canvas Scaler Mode: {scaler.uiScaleMode}\n" +
                      $"Reference Resolution: {scaler.referenceResolution}\n" +
                      $"Screen Match Mode: {scaler.screenMatchMode}\n" +
                      $"Match Width/Height: {scaler.matchWidthOrHeight}");
            Debug.Log("=====================================");
        }

        #endregion
    }
}
