using UnityEngine;
using UnityEngine.Video;
using UnityEngine.InputSystem;

namespace ChillZone.Scene.IntroScene
{
    public class IntroSceneController : MonoBehaviour
    {
        [SerializeField, Header("Video Configuration"), Tooltip("Maximum time (in seconds) the intro video will play before automatically skipping")]
        private float playTime = 2.0f;
        [SerializeField, Header("Debug"), Tooltip("Enable to show debug logs in the console")]
        private bool showDebugLogs = true;

        private VideoPlayer _videoPlayer;
        private float _elapsedTime;
        private bool _isTransitioning;

        private void Awake()
        {
            _videoPlayer = GetComponent<VideoPlayer>();
            Init();
        }

        private void Update()
        {
            // Only count time if video is actually playing
            if (_videoPlayer && _videoPlayer.isPlaying)
                _elapsedTime += Time.deltaTime;

            // Check for skip input (touch/click or max time reached)
            if (_elapsedTime >= playTime || (Pointer.current != null && Pointer.current.press.wasPressedThisFrame))
                SkipIntro();
        }

        /// <summary>Initialize video player settings and start playback; And preload the next scene in the background</summary>
        private void Init()
        {
            if (!_videoPlayer)
            {
                Debug.LogWarning("[IntroSceneController] No VideoPlayer assigned!");
                SkipIntro();
                return;
            }

            _videoPlayer.playOnAwake = false;
            _videoPlayer.waitForFirstFrame = true;
            _videoPlayer.isLooping = false;
            _videoPlayer.skipOnDrop = true;
            if (showDebugLogs)
                Debug.Log($"[IntroSceneController] VideoPlayer initialized with settings: playOnAwake={_videoPlayer.playOnAwake}, waitForFirstFrame={_videoPlayer.waitForFirstFrame}, isLooping={_videoPlayer.isLooping}, skipOnDrop={_videoPlayer.skipOnDrop}");

            _videoPlayer.loopPointReached += OnVideoFinished;
            if (!_videoPlayer.isPlaying) _videoPlayer.Play();
            if (showDebugLogs)
                Debug.Log("[IntroSceneController] Video playback started");

            SceneLoader.PreloadNext();
            if (showDebugLogs)
                Debug.Log("[IntroSceneController] Next scene preloading started");
        }

        /// <summary>Called when video finishes playing</summary>
        private void OnVideoFinished(VideoPlayer vp)
        {
            if (_isTransitioning) return;
            if (showDebugLogs)
                Debug.Log("[IntroSceneController] Video finished");

            StartTransition();
        }

        /// <summary>Skip Intro manually</summary>
        private void SkipIntro()
        {
            if (_isTransitioning) return;
            if (showDebugLogs)
                Debug.Log("[IntroSceneController] Intro skipped manually");

            StartTransition();
        }

        /// <summary>Start transition to next scene</summary>
        private void StartTransition()
        {
            if (_isTransitioning) return;
            _isTransitioning = true;

            if (showDebugLogs)
                Debug.Log("[IntroSceneController] Starting transition");

            // Stop video
            if (_videoPlayer && _videoPlayer.isPlaying) _videoPlayer.Stop();

            // Load scene
            SceneLoader.LoadNext();
        }

        private void OnDestroy()
        {
            if (_videoPlayer != null) _videoPlayer.loopPointReached -= OnVideoFinished;
        }
    }
}
