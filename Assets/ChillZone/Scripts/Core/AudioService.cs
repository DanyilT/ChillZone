using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ChillZone.Core
{
    /// <summary>
    /// DontDestroyOnLoad audio hub bootstrapped in the Game scene. It owns three buses, all children
    /// of this GameObject (which sits at the world origin) so every sound plays from a single fixed,
    /// non-spatial place: music, gameplay SFX (ball throw/hit/miss), and UI SFX (button clicks).
    /// Gameplay and UI SFX are SEPARATE buses with their own enabled/volume settings — so the player
    /// can, say, keep button clicks while muting ball sounds. Ball sounds route here rather than to an
    /// AudioSource on the ball, so they stay audible even though the ball moves and is pooled/destroyed.
    ///
    /// The static enabled/volume API persists each setting to PlayerPrefs and applies it to the
    /// live source only when an instance exists — so the Settings scene can drive audio even when
    /// opened standalone (no instance): the values are saved and applied the next time the Game
    /// scene boots and AudioService.Awake runs. The SFX/click playback calls are likewise safe
    /// no-ops when no instance exists.
    /// </summary>
    public class AudioService : MonoBehaviour
    {
        private static AudioService Instance { get; set; }

        [Header("Sources")]
        [SerializeField] private AudioSource musicSource;
        [SerializeField, Tooltip("Gameplay SFX bus (ball throw/hit/miss).")]
        private AudioSource sfxSource;
        [SerializeField, Tooltip("UI SFX bus (button clicks) — kept separate from gameplay SFX. Created at runtime on this GameObject if left unassigned.")]
        private AudioSource uiSfxSource;

        [Header("Volumes")]
        [SerializeField, Range(0f, 1f), Tooltip("Music bus volume. Kept low by default so gameplay SFX stay audible over the music.")]
        private float musicVolume = 0.2f;
        [SerializeField, Range(0f, 1f), Tooltip("Gameplay SFX bus volume (ball throw/hit/miss).")]
        private float sfxVolume = 1f;
        [SerializeField, Range(0f, 1f), Tooltip("UI SFX bus volume (button clicks).")]
        private float uiSfxVolume = 1f;

        [Header("Music")]
        [SerializeField, Tooltip("Background music tracks. Played in order; loops back to the first after the last finishes.")]
        private List<AudioClip> musicPlaylist = new();

        [Header("UI SFX")]
        [SerializeField, Tooltip("Generic UI click sound. Played for ordinary button clicks AND used as the fallback for any UI action below whose clip is left empty. Uses the UI SFX bus.")]
        private AudioClip buttonClickClip;
        [SerializeField, Tooltip("Per-action UI sounds (open/close/toggle/pick-item/pause/unpause/reset). Each one falls back to the generic click sound above when its clip is empty.")]
        private List<UiSoundEntry> uiSounds = new();

        [Serializable]
        private struct UiSoundEntry
        {
            public UiSound action;
            public AudioClip clip;
        }

        private readonly Dictionary<UiSound, AudioClip> _uiSoundMap = new();

        private int _musicIndex;

        private const string MusicEnabledKey = "audio.music.enabled";
        private const string SfxEnabledKey   = "audio.sfx.enabled";
        private const string UiSfxEnabledKey = "audio.uisfx.enabled";

        #region static api (prefs-backed)

        // Per-bus enabled toggles are user-facing (PlayerPrefs); per-bus volume is a developer-tuned
        // serialized field (see the Volumes header) — sliders proved fiddly on mobile, so volume is fixed.
        public static bool MusicEnabled => GetBool(MusicEnabledKey, true);
        public static bool SfxEnabled   => GetBool(SfxEnabledKey, true);
        public static bool UiSfxEnabled => GetBool(UiSfxEnabledKey, true);

        public static void SetMusicEnabled(bool on) { SetBool(MusicEnabledKey, on); Instance?.ApplyMusic(); }
        public static void SetSfxEnabled(bool on)   { SetBool(SfxEnabledKey, on);   Instance?.ApplySfx(); }
        public static void SetUiSfxEnabled(bool on) { SetBool(UiSfxEnabledKey, on); Instance?.ApplyUiSfx(); }

        #endregion

        #region static playback api

        /// <summary>Play a one-shot SFX (e.g. a ball's throw/hit/miss clip) on the shared central source. Respects the SFX enabled/volume settings; safe no-op with no instance or null clip.</summary>
        public static void PlaySfx(AudioClip clip) => Instance?.PlaySfxInternal(clip);

        /// <summary>Play the UI sound mapped to <paramref name="action"/> on the UI SFX bus (falls back to the generic click sound when that action has no clip assigned).</summary>
        public static void PlayUi(UiSound action) => Instance?.PlayUiSfxInternal(Instance.ResolveUiClip(action));

        /// <summary>Play the generic UI button-click sound. UnityAction-compatible, so it can be added directly to a Button's onClick (for buttons not built by ButtonManager — score button, settings buttons, …).</summary>
        public static void PlayButtonClick() => PlayUi(UiSound.Click);

        #endregion

        #region lifecycle

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            EnsureUiSfxSource();
            BuildUiSoundMap();
            ApplyMusic();
            ApplySfx();
            ApplyUiSfx();
            StartMusic();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Update() => AdvanceMusicIfFinished();

#if UNITY_EDITOR
        // Live-apply volume tweaks while tuning in the inspector (each Apply* no-ops if its source is null).
        private void OnValidate()
        {
            ApplyMusic();
            ApplySfx();
            ApplyUiSfx();
        }
#endif

        #endregion

        #region apply

        private void ApplyMusic()
        {
            if (!musicSource) return;
            musicSource.mute   = !MusicEnabled;
            musicSource.volume = musicVolume;
        }

        private void ApplySfx()
        {
            if (!sfxSource) return;
            sfxSource.mute   = !SfxEnabled;
            sfxSource.volume = sfxVolume;
        }

        private void ApplyUiSfx()
        {
            if (!uiSfxSource) return;
            uiSfxSource.mute   = !UiSfxEnabled;
            uiSfxSource.volume = uiSfxVolume;
        }

        #endregion

        #region music playlist

        private void StartMusic()
        {
            if (!musicSource || musicPlaylist == null || musicPlaylist.Count == 0) return;
            musicSource.loop = false;   // looping is handled here so the playlist can advance between tracks
            PlayMusicTrack(0);
        }

        private void PlayMusicTrack(int index)
        {
            if (!musicSource || musicPlaylist == null || musicPlaylist.Count == 0) return;
            _musicIndex = ((index % musicPlaylist.Count) + musicPlaylist.Count) % musicPlaylist.Count;
            var clip = musicPlaylist[_musicIndex];
            if (!clip) return;
            musicSource.clip = clip;
            musicSource.Play();
        }

        // Advance to the next track once the current one finishes, cycling back to the first at the end.
        // Skipped while muted (music disabled) so a disabled playlist doesn't churn; re-enabling resumes
        // playback on the next Update.
        private void AdvanceMusicIfFinished()
        {
            if (!musicSource || musicSource.mute || musicSource.isPlaying) return;
            if (musicPlaylist == null || musicPlaylist.Count == 0) return;
            PlayMusicTrack(_musicIndex + 1);
        }

        #endregion

        #region sfx

        // PlayOneShot layers over the source's own volume (set by Apply*), so it follows the bus volume
        // setting without double-scaling. Routing SFX through these fixed sources keeps every sound coming
        // from the same place — the whole point of not putting an AudioSource on the moving ball.
        private void PlaySfxInternal(AudioClip clip)
        {
            if (!clip || !sfxSource || !SfxEnabled) return;
            sfxSource.PlayOneShot(clip);
        }

        private void PlayUiSfxInternal(AudioClip clip)
        {
            if (!clip || !uiSfxSource || !UiSfxEnabled) return;
            uiSfxSource.PlayOneShot(clip);
        }

        // The clip for a UI action, or the generic click clip when that action has none assigned.
        private AudioClip ResolveUiClip(UiSound action) =>
            _uiSoundMap.TryGetValue(action, out var clip) && clip ? clip : buttonClickClip;

        private void BuildUiSoundMap()
        {
            _uiSoundMap.Clear();
            foreach (var entry in uiSounds.Where(entry => entry.clip)) _uiSoundMap[entry.action] = entry.clip;   // last one wins on duplicate actions
        }

        // The UI SFX bus needs its own source so its volume/mute are independent of gameplay SFX. If the
        // prefab doesn't supply one, create it on this GameObject (2D, at the origin like the other buses).
        private void EnsureUiSfxSource()
        {
            if (uiSfxSource) return;
            uiSfxSource = gameObject.AddComponent<AudioSource>();
            uiSfxSource.playOnAwake = false;
            uiSfxSource.spatialBlend = 0f;
        }

        #endregion

        #region prefs helpers

        private static bool GetBool(string key, bool fallback) => PlayerPrefs.GetInt(key, fallback ? 1 : 0) == 1;
        private static void SetBool(string key, bool value) { PlayerPrefs.SetInt(key, value ? 1 : 0); PlayerPrefs.Save(); }

        #endregion
    }
}
