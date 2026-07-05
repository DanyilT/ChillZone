using System;
using System.Collections.Generic;
using System.Linq;
using ChillZone.Content;
using ChillZone.Core;
using ChillZone.Player;
using ChillZone.Utils.Native;
using ChillZone.UI.Button1;
using ChillZone.UI.Settings.Credits;
using ChillZone.UI.Window;
using ChillZone.UI.Window.Config;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace ChillZone.Scene.Settings
{
    /// <summary>
    /// Single scene-side hub for every settings action, keyed by id. Holds the generic
    /// key→handler map (button / toggle / slider) AND the concrete implementations (audio via
    /// <see cref="AudioService"/>, credits, manual window, reset, left-handed, back). Cells
    /// (SO assets) bind by string key, resolved lazily so registration order is irrelevant and
    /// unknown keys are safe no-ops — which is why the screen still renders in edit mode.
    ///
    /// Link cells don't use keys: they call <see cref="OpenUrl"/> with the URL stored on the
    /// asset, and the code cell calls <see cref="SubmitCode"/>.
    /// </summary>
    public class SettingsActions : MonoBehaviour
    {
        [Header("Header / Navigation")]
        [SerializeField] private Button creditsButton;
        [SerializeField] private Button backButton;

        [SerializeField, Header("Manual")]
        private WindowConfig manualWindowConfig;

        [SerializeField, Header("Rebuild after reset (optional — found automatically)")]
        private SettingsBuilder.SettingsBuilder settingsBuilder;

        [SerializeField, Header("Credits")]
        private CreditsConfig creditsConfig;

        [Header("Unlock dialog (optional — to name the ball/basket a code unlocks)")]
        [SerializeField, Tooltip("Ball content registry. Used to look up the display name of a ball unlocked by a code. Falls back to any loaded ball registry; if unresolved the dialog just says 'a new ball'.")]
        private UnlockableContentRegistry ballRegistry;
        [SerializeField, Tooltip("Basket content registry. Same as the ball registry, for baskets.")]
        private UnlockableContentRegistry basketRegistry;

        #region action map

        private readonly Dictionary<string, Action> _buttons = new();
        private readonly Dictionary<string, (Func<bool> get, Action<bool> set)> _toggles = new();
        private readonly Dictionary<string, (Func<float> get, Action<float> set)> _sliders = new();
        private readonly Dictionary<string, Func<string>> _texts = new();

        public void RegisterButton(string key, Action onClick)
        {
            if (!string.IsNullOrEmpty(key)) _buttons[key] = onClick;
        }

        public void RegisterToggle(string key, Func<bool> getter, Action<bool> setter)
        {
            if (!string.IsNullOrEmpty(key)) _toggles[key] = (getter, setter);
        }

        public void RegisterSlider(string key, Func<float> getter, Action<float> setter)
        {
            if (!string.IsNullOrEmpty(key)) _sliders[key] = (getter, setter);
        }

        public void InvokeButton(string key)
        {
            if (key != null && _buttons.TryGetValue(key, out var action)) action?.Invoke();
        }

        public bool GetToggle(string key, bool fallback = false) =>
            key != null && _toggles.TryGetValue(key, out var t) ? t.get() : fallback;

        public void SetToggle(string key, bool value)
        {
            if (key != null && _toggles.TryGetValue(key, out var t)) t.set(value);
        }

        public float GetSlider(string key, float fallback = 0f) =>
            key != null && _sliders.TryGetValue(key, out var s) ? s.get() : fallback;

        public void SetSlider(string key, float value)
        {
            if (key != null && _sliders.TryGetValue(key, out var s)) s.set(value);
        }

        /// <summary>Read-only text provider (e.g. app version), resolved by display cells.</summary>
        public void RegisterText(string key, Func<string> getter)
        {
            if (!string.IsNullOrEmpty(key)) _texts[key] = getter;
        }

        public string GetText(string key, string fallback = "") =>
            key != null && _texts.TryGetValue(key, out var get) ? get() ?? fallback : fallback;

        #endregion

        #region lifecycle

        private void Awake()
        {
            // WindowManager is DontDestroyOnLoad, so a window left open in the Game scene (e.g. the
            // shared manual) leaks into Settings — close them all so nothing covers the UI or
            // swallows the back press (HandleNativeBack would otherwise close the leak, not navigate).
            WindowManager.Instance?.CloseAll();
            RegisterDefaults();
        }

        private void Start()
        {
            if (creditsButton)
            {
                creditsButton.onClick.AddListener(OpenCredits);
                creditsButton.onClick.AddListener(AudioService.PlayButtonClick);
            }
            if (backButton)
            {
                backButton.onClick.AddListener(GoBack);
                backButton.onClick.AddListener(AudioService.PlayButtonClick);
            }
        }

        private void OnDestroy()
        {
            if (creditsButton) creditsButton.onClick.RemoveListener(OpenCredits);
            if (backButton) backButton.onClick.RemoveListener(GoBack);
        }

        // Android hardware / native back arrives as Escape on the Keyboard device (new Input System).
        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
                HandleNativeBack();
        }

        #endregion

        #region concrete actions

        private CreditsOverlay _credits;

        /// <summary>Code that toggles developer mode (case-insensitive).</summary>
        private const string DeveloperCode = "dev";
        private const string EasterEggCode = "qwerty";

        private void RegisterDefaults()
        {
            // Volume is now a developer-tuned serialized field on AudioService (sliders were fiddly on
            // mobile), so audio settings expose only the per-bus enabled toggles.
            RegisterToggle(SettingsKeys.MusicEnabled, () => AudioService.MusicEnabled, AudioService.SetMusicEnabled);
            RegisterToggle(SettingsKeys.SfxEnabled,   () => AudioService.SfxEnabled,   AudioService.SetSfxEnabled);
            RegisterToggle(SettingsKeys.UiSfxEnabled, () => AudioService.UiSfxEnabled, AudioService.SetUiSfxEnabled);

            RegisterToggle(SettingsKeys.LeftHanded, () => GetBool(PrefKeys.LeftHanded, false), SetLeftHanded);

            RegisterText(SettingsKeys.AppVersion, () => Application.version);

            RegisterButton(SettingsKeys.OpenCredits,   OpenCredits);
            RegisterButton(SettingsKeys.OpenManual,    OpenManual);
            RegisterButton(SettingsKeys.ResetProgress, ConfirmReset);
            RegisterButton(SettingsKeys.Back,          GoBack);
        }

        /// <summary>Single link handler — the URL comes from the cell asset.</summary>
        public void OpenUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return;
            AudioService.PlayButtonClick();
            Application.OpenURL(url);
        }

        /// <summary>Code/easter-egg input → unlocks via the player profile. Returns true if accepted. On a new
        /// unlock it pops a native info dialog (achievement details, or a ball/basket unlock + picker hint).</summary>
        public bool SubmitCode(string code)
        {
            if (string.IsNullOrWhiteSpace(code)) return false;

            var trimmed = code.Trim();

            // The "dev" code is not player progression — it just toggles the developer-mode device pref
            // (which reveals the throw-mode selector), so it's handled here and never reaches the profile.
            if (string.Equals(trimmed, DeveloperCode, StringComparison.OrdinalIgnoreCase))
                return ToggleDeveloperMode();

            if (!PlayerProfileManager.Instance) return false;

            var result = PlayerProfileManager.Instance.RegisterCode(trimmed);
            if (result.Accepted)
            {
                AnnounceCodeUnlock(result);
                // Redeeming the easter-egg code arms the one-time max-score flourish shown next time in the game HUD.
                if (string.Equals(trimmed, EasterEggCode, StringComparison.OrdinalIgnoreCase))
                {
                    PlayerPrefs.SetInt(PrefKeys.EasterEggScorePending, 1);
                    PlayerPrefs.Save();
                }
            }
            return result.Accepted;
        }

        // Flip developer mode on/off, tell the user, and rebuild the screen so the developer-only cells
        // (throw-mode selector) appear or disappear immediately. Entering "dev" again disables it.
        private bool ToggleDeveloperMode()
        {
            var enabled = DeveloperMode.Toggle();
            NativeConfirmDialog.ShowInfo(
                "Developer Mode",
                enabled ? "Developer options are now shown in settings." : "Developer options are now hidden.");

            if (!settingsBuilder) settingsBuilder = FindObjectOfType<SettingsBuilder.SettingsBuilder>();
            settingsBuilder?.Rebuild();
            return true;
        }

        // Confirm a redeemed code with a native dialog. A ball/basket reward wins (it's the actionable unlock,
        // with a picker hint); otherwise show the achievement's own title + description.
        private void AnnounceCodeUnlock(CodeRedeemResult result)
        {
            if (result.UnlockedBallId != null)
            {
                AnnounceContentUnlock("ball", result.UnlockedBallId, ballRegistry, ContentTypes.Ball);
            }
            else if (result.UnlockedBasketId != null)
            {
                AnnounceContentUnlock("basket", result.UnlockedBasketId, basketRegistry, ContentTypes.Basket);
            }
            else if (result.HasAchievement)
            {
                NativeConfirmDialog.ShowInfo(result.AchievementTitle, result.AchievementDescription);
            }
            else
            {
                NativeConfirmDialog.ShowInfo("Code Accepted", "Nice — your code was accepted.");
            }
        }

        private static void AnnounceContentUnlock(string kind, string contentId, UnlockableContentRegistry registry, ContentTypes type)
        {
            var contentName = ResolveContentName(contentId, registry, type);
            var what = string.IsNullOrEmpty(contentName) ? $"a new {kind}" : $"the {contentName} {kind}";
            NativeConfirmDialog.ShowInfo(
                $"{char.ToUpperInvariant(kind[0])}{kind[1..]} Unlocked!",
                $"Congratulations! You've unlocked {what}. You can find and select it in the {kind} picker.");
        }

        // Resolve a content display name from the wired registry, falling back to any loaded registry of the
        // right type. Returns null if it can't be resolved (the dialog then uses a generic "a new ball/basket").
        private static string ResolveContentName(string contentId, UnlockableContentRegistry registry, ContentTypes type)
        {
            registry = registry ? registry : Resources.FindObjectsOfTypeAll<UnlockableContentRegistry>().FirstOrDefault(r => r && r.contentType == type);
            var content = registry ? registry.GetById(contentId) : null;
            return content && !string.IsNullOrWhiteSpace(content.displayName) ? content.displayName : null;
        }

        // Persist the left-handed pref and re-render every ButtonManager in the scene so the mirrored layout
        // shows immediately (ButtonManagers read the pref per-render), rather than only after the next scene load.
        private static void SetLeftHanded(bool on)
        {
            SetBool(PrefKeys.LeftHanded, on);
            foreach (var manager in FindObjectsOfType<ButtonManager>())
                manager.Refresh();
        }

        public static void GoBack() => SceneLoader.LoadPrevious();

        // Native/hardware back: dismiss the topmost overlay first (credits, then the manual window),
        // otherwise leave the Settings scene. Mirrors what the BackNavigationController stack would do.
        private void HandleNativeBack()
        {
            if (_credits && _credits.isActiveAndEnabled) { _credits.Hide(); return; }

            if (manualWindowConfig && WindowManager.Instance && WindowManager.Instance.IsOpen(manualWindowConfig.windowId))
            {
                WindowManager.Instance.Close(manualWindowConfig.windowId);
                return;
            }

            GoBack();
        }

        public void OpenCredits()
        {
            if (!_credits) _credits = CreditsOverlay.Create(creditsConfig);
            _credits.Show();
        }

        private void OpenManual() => WindowManager.Instance?.Show(manualWindowConfig);

        private void ConfirmReset()
        {
            NativeConfirmDialog.Show(
                "Reset Progress?",
                "This permanently erases your scores, unlocks and codes.",
                "Reset", "Cancel",
                onConfirm: () =>
                {
                    PlayerProfileManager.Instance?.ResetProfile();
                    if (!settingsBuilder) settingsBuilder = FindObjectOfType<SettingsBuilder.SettingsBuilder>();
                    settingsBuilder?.Rebuild();
                });
        }

        #endregion

        #region prefs helpers

        private static bool GetBool(string key, bool fallback) => PlayerPrefs.GetInt(key, fallback ? 1 : 0) == 1;
        private static void SetBool(string key, bool value) { PlayerPrefs.SetInt(key, value ? 1 : 0); PlayerPrefs.Save(); }

        #endregion
    }

    /// <summary>Canonical action keys; cell assets reference these strings in the inspector.</summary>
    public static class SettingsKeys
    {
        public const string MusicEnabled  = "settings.music.enabled";
        public const string SfxEnabled    = "settings.sfx.enabled";
        public const string UiSfxEnabled  = "settings.uisfx.enabled";
        public const string LeftHanded    = "settings.lefthanded";
        public const string OpenCredits   = "settings.credits.open";
        public const string OpenManual    = "settings.manual.open";
        public const string ResetProgress = "settings.reset";
        public const string Back          = "settings.back";
        public const string AppVersion    = "settings.app.version";
    }
}
