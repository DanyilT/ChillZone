using UnityEngine;

namespace ChillZone.Utils.Native
{
    /// <summary>
    /// Android edge-to-edge helper. Makes the status + navigation bars transparent and lays the game out
    /// BEHIND them — the bars stay visible and functional (back navigation keeps working), the content
    /// just shows through. Also exposes the real navigation-bar height: <see cref="Screen.safeArea"/> on
    /// Android only reports the display cutout (notch), NOT the nav bar, so without this the bottom
    /// safe-area inset comes back as 0 and UI slides under the nav bar.
    ///
    /// No-op in the editor and on every non-Android platform.
    /// </summary>
    public static class AndroidSystemBars
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Init()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            MakeBarsTransparent();
#endif
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        private static void MakeBarsTransparent()
        {
            try
            {
                // NOTE: do NOT 'using'-dispose activity/player here — runOnUiThread runs the body later,
                // so they must outlive this method. They're disposed inside the runnable instead.
                var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                var activity = player.GetStatic<AndroidJavaObject>("currentActivity");
                if (activity == null) { player.Dispose(); return; }

                activity.Call("runOnUiThread", new AndroidJavaRunnable(() =>
                {
                    try { ApplyTransparentBars(activity); }
                    catch (System.Exception e) { Debug.LogWarning($"[AndroidSystemBars] apply: {e.Message}"); }
                    finally { activity.Dispose(); player.Dispose(); }
                }));
            }
            catch (System.Exception e) { Debug.LogWarning($"[AndroidSystemBars] {e.Message}"); }
        }

        private static void ApplyTransparentBars(AndroidJavaObject activity)
        {
            using var window = activity.Call<AndroidJavaObject>("getWindow");
            using var decor = window.Call<AndroidJavaObject>("getDecorView");

            // Draw the bar backgrounds ourselves (needed for setXBarColor to apply) and drop the old
            // translucent scrims.
            const int FLAG_TRANSLUCENT_STATUS           = 0x04000000;
            const int FLAG_TRANSLUCENT_NAVIGATION       = 0x08000000;
            const int FLAG_DRAWS_SYSTEM_BAR_BACKGROUNDS = unchecked((int)0x80000000);
            window.Call("clearFlags", FLAG_TRANSLUCENT_STATUS | FLAG_TRANSLUCENT_NAVIGATION);
            window.Call("addFlags", FLAG_DRAWS_SYSTEM_BAR_BACKGROUNDS);

            // LAYOUT_* flags lay the content out behind the bars WITHOUT hiding them — nav stays usable.
            const int LAYOUT_STABLE          = 0x00000100;
            const int LAYOUT_HIDE_NAVIGATION = 0x00000200;
            const int LAYOUT_FULLSCREEN      = 0x00000400;
            decor.Call("setSystemUiVisibility", LAYOUT_STABLE | LAYOUT_HIDE_NAVIGATION | LAYOUT_FULLSCREEN);

            window.Call("setStatusBarColor", 0);      // ARGB 0 = transparent
            window.Call("setNavigationBarColor", 0);

            // API 29+ otherwise paints a protective scrim behind a transparent bar — opt out so it's truly clear.
            if (ApiLevel() >= 29)
            {
                window.Call("setNavigationBarContrastEnforced", false);
                window.Call("setStatusBarContrastEnforced", false);
            }
        }

        private static int ApiLevel()
        {
            using var version = new AndroidJavaClass("android.os.Build$VERSION");
            return version.GetStatic<int>("SDK_INT");
        }

        private static int _navBarPx = -1;

        /// <summary>Navigation-bar height in pixels, from the system dimension resource. 0 if unavailable.</summary>
        public static int NavigationBarHeightPx
        {
            get
            {
                if (_navBarPx < 0) _navBarPx = SystemDimenPx("navigation_bar_height");
                return _navBarPx;
            }
        }

        private static int SystemDimenPx(string resName)
        {
            try
            {
                using var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                using var activity = player.GetStatic<AndroidJavaObject>("currentActivity");
                using var resources = activity.Call<AndroidJavaObject>("getResources");
                int id = resources.Call<int>("getIdentifier", resName, "dimen", "android");
                return id > 0 ? resources.Call<int>("getDimensionPixelSize", id) : 0;
            }
            catch { return 0; }
        }
#else
        /// <summary>Navigation-bar height in pixels. Always 0 off Android.</summary>
        public static int NavigationBarHeightPx => 0;
#endif
    }
}
