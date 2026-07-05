using System.Collections.Generic;
using ChillZone.Basket;
using ChillZone.Content;
using ChillZone.Core;
using ChillZone.Core.Events;
using ChillZone.Gameplay;
using ChillZone.UI.Utils.Config;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ChillZone.UI.Game
{
    /// <summary>
    /// Subscribes to scoring events and drives the in-game HUD.
    /// Attach to a Canvas in the Game scene and wire up the TMP references
    /// in the Inspector.
    ///
    /// Required Inspector wiring:
    ///   scoreLabel            — shows "0"
    ///   scoreFlashLabel       — flashes "+18" with a bonus breakdown after a basket, auto-hides
    ///   multiplierBannerLabel — persistent banner of the active static multipliers (throw mode + basket)
    ///   throwModeLabel        — shows current throw mode chip (optional, when developerOptionMode true)
    ///   scoringConfig         — the ScoringConfig asset
    ///   throwConfig           — the ThrowConfig asset
    /// </summary>
    public class GameHUD : MonoBehaviour
    {
        [Header("Labels")]
        [SerializeField, Tooltip("Shows the current score. Required.")]
        private TextMeshProUGUI scoreLabel;
        [SerializeField, Tooltip("Optional — flashes the last score earned. Auto-hides after a few seconds.")]
        private TextMeshProUGUI scoreFlashLabel;
        [SerializeField, Tooltip("Optional — shows current throw mode chip. When developerOptionMode is true.")]
        private TextMeshProUGUI throwModeLabel;
        [SerializeField, Tooltip("Optional — persistent banner of the active static multipliers (throw mode + basket). Auto-hides when none apply.")]
        private TextMeshProUGUI multiplierBannerLabel;

        [Header("Score Flash — pin to basket")]
        [SerializeField, Tooltip("Move the score-flash label's rect to the basket's on-screen position (it stays a HUD/canvas element at its authored size — only its position changes; the basket stays in the world, a separate layer).")]
        private bool pinScoreFlashToBasket;
        [SerializeField, Tooltip("When pinned: which point of the flash text sits at the basket's screen point. e.g. LowerCenter makes the text appear above the basket.")]
        private TextAnchor scoreFlashBasketAnchor = TextAnchor.LowerCenter;
        [SerializeField, Tooltip("When pinned: extra screen-space offset (pixels) from the basket point.")]
        private Vector2 scoreFlashPinOffset = Vector2.zero;
        [SerializeField, Tooltip("When pinned: pick a fresh random spot near the basket for every flash.")]
        private bool randomizeScoreFlashPosition;
        [SerializeField, Tooltip("Radius (m) around the basket used for the random flash position.")]
        private float scoreFlashRandomRadius = 0.25f;

        [Header("VFX")]
        [SerializeField, Tooltip("Score-flash VFX prefabs by score tier (UGUI prefabs whose Animator plays an Image animation). The highest tier whose Min Score <= the points scored is shown behind the flash text. Leave empty for none.")]
        private ScoreFlashVfxTier[] scoreFlashVfxTiers;

        [System.Serializable]
        private struct ScoreFlashVfxTier
        {
            [Tooltip("Used when the points scored are >= this. The highest matching tier wins.")]
            public int minScore;
            public GameObject prefab;
        }

        [Header("Config")]
        [SerializeField] private ChillZone.Config.ScoringConfig scoringConfig;
        [SerializeField] private ChillZone.Config.ThrowConfig throwConfig;

        // Reads the developer-mode device pref (toggled by the "dev" code in Settings). When on, the throw-mode
        // label is shown; otherwise it stays hidden. Read live so re-entering the Game scene reflects a change.
        private static bool DeveloperOptionMode => DeveloperMode.IsEnabled;
        private float _flashTimer;
        private Vector3 _flashWorldPoint;

        // Easter egg: after the qwerty code is redeemed (PrefKeys.EasterEggScorePending), the HUD shows a ONE-TIME
        // int.MaxValue flourish via a clone of the score label (real label hidden, still updated underneath). It
        // reverts on the next basket/miss and never re-shows on reopen (the pending flag is consumed when shown).
        private TextMeshProUGUI _maxScoreClone;
        private bool _easterEggActive;

        // A multiplier must exceed this to count as a shown bonus (keeps "×1.0" noise out of the flash/banner).
        private const float MinShownMultiplier = 1.05f;

        // Last static multipliers rendered on the persistent banner — change-detection so we only rebuild on change.
        private float _bannerBasketMultiplier = -1f;
        private float _bannerThrowMultiplier = -1f;

        // The score-flash VFX prefab instance lives inside a RectMask2D (clipped to the text's rect) ordered
        // just behind the flash text. The prefab's own Animator drives the animation — no frame code here.
        private RectTransform _flashVfxMask;
        private GameObject _flashVfx;
        private GameObject _flashVfxSource; // which tier prefab the current instance was made from

        #region lifecycle

        private void OnEnable()
        {
            EventBus<ScoreUpdatedEvent>.Subscribe(OnScoreUpdated);
            EventBus<BallScoredEvent>.Subscribe(OnBallScored);
            EventBus<BallMissedEvent>.Subscribe(OnBallMissed);
            EventBus<BallThrownEvent>.Subscribe(OnBallThrown);
        }

        private void OnDisable()
        {
            EventBus<ScoreUpdatedEvent>.Unsubscribe(OnScoreUpdated);
            EventBus<BallScoredEvent>.Unsubscribe(OnBallScored);
            EventBus<BallMissedEvent>.Unsubscribe(OnBallMissed);
            EventBus<BallThrownEvent>.Unsubscribe(OnBallThrown);
        }

        private void Start()
        {
            if (scoreFlashLabel) scoreFlashLabel.gameObject.SetActive(false);
            RefreshThrowModeLabel();
            RefreshMultiplierBanner();
            // Show the current run score — non-zero when a run from a previous session was restored on load.
            UpdateScoreLabel(ScoringSystem.Instance ? ScoringSystem.Instance.CurrentRunScore : 0, 0);
            EnsureEasterEggScore();
        }

        private void Update()
        {
            RefreshMultiplierBanner();

            if (_flashTimer <= 0f) return;
            _flashTimer -= Time.deltaTime;

            // Keep the flash glued to the basket as the device moves (its world point is fixed).
            if (pinScoreFlashToBasket && scoreFlashLabel && scoreFlashLabel.gameObject.activeSelf)
                PositionFlashAtBasket();

            if (_flashVfxMask && _flashVfxMask.gameObject.activeSelf)
                PositionFlashVfx();

            if (_flashTimer <= 0f)
            {
                if (scoreFlashLabel) scoreFlashLabel.gameObject.SetActive(false);
                if (_flashVfxMask) _flashVfxMask.gameObject.SetActive(false);
            }
        }

        #endregion

        #region event handlers

        private void OnScoreUpdated(ScoreUpdatedEvent evt) =>
            UpdateScoreLabel(evt.TotalScore, evt.ThrowCount);

        private void OnBallScored(BallScoredEvent evt)
        {
            HideEasterEggScore();  // reverts on the first hit
            FlashScore(evt);
        }

        private void OnBallMissed(BallMissedEvent evt) => HideEasterEggScore();  // ...or the first miss

        private void OnBallThrown(BallThrownEvent evt) =>
            RefreshThrowModeLabel();

        #endregion

        #region private helpers

        private void UpdateScoreLabel(int score, int throws)
        {
            if (scoreLabel) scoreLabel.text = score.ToString();
        }

        // One-time flourish after the qwerty code is redeemed: overlay int.MaxValue via a clone of the score label
        // (real label hidden but still updated underneath). The pending flag is CONSUMED here, so it shows once and
        // never re-appears on reopen; HideEasterEggScore reverts it on the next basket/miss.
        private void EnsureEasterEggScore()
        {
            if (!scoreLabel) return;
            if (PlayerPrefs.GetInt(PrefKeys.EasterEggScorePending, 0) != 1) return;

            PlayerPrefs.SetInt(PrefKeys.EasterEggScorePending, 0);
            PlayerPrefs.Save();

            if (!_maxScoreClone)
            {
                _maxScoreClone = Instantiate(scoreLabel, scoreLabel.transform.parent);
                _maxScoreClone.name = "ScoreLabel_MaxEasterEgg";

                // Match the original's placement so the clone sits exactly over it.
                var src = scoreLabel.rectTransform;
                var dst = _maxScoreClone.rectTransform;
                dst.anchorMin = src.anchorMin;
                dst.anchorMax = src.anchorMax;
                dst.pivot = src.pivot;
                dst.anchoredPosition = src.anchoredPosition;
                dst.sizeDelta = src.sizeDelta;
            }

            _maxScoreClone.text = int.MaxValue.ToString();
            _maxScoreClone.gameObject.SetActive(true);
            scoreLabel.gameObject.SetActive(false);
            _easterEggActive = true;
        }

        // Revert the flourish to the real score (on the next basket/miss). The real label was kept updated while
        // hidden, so it shows the current score immediately.
        private void HideEasterEggScore()
        {
            if (!_easterEggActive) return;
            _easterEggActive = false;
            if (_maxScoreClone) _maxScoreClone.gameObject.SetActive(false);
            if (scoreLabel) scoreLabel.gameObject.SetActive(true);
        }

        private void FlashScore(BallScoredEvent evt)
        {
            if (!scoreFlashLabel) return;

            scoreFlashLabel.text = BuildFlashText(evt);
            scoreFlashLabel.gameObject.SetActive(true);
            _flashTimer = scoringConfig ? scoringConfig.scoreFlashDuration : 2.5f;

            var worldPoint = evt.HitPoint;
            _flashWorldPoint = pinScoreFlashToBasket && randomizeScoreFlashPosition ? worldPoint + Random.insideUnitSphere * scoreFlashRandomRadius : worldPoint;

            if (pinScoreFlashToBasket)
            {
                // Center the anchors so the placement below is unaffected by the authored anchoring.
                scoreFlashLabel.rectTransform.anchorMin = scoreFlashLabel.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                PositionFlashAtBasket();
            }

            PlayFlashVfx(PickFlashVfxPrefab(evt.FinalPoints));
        }

        // The score plus a breakdown of every bonus above 1× that stacked on this shot (distance, throw
        // difficulty, basket), e.g. "+240\nDist ×1.5 · Spin ×1.8 · Basket ×2". Just "+points" when none applied.
        private static string BuildFlashText(BallScoredEvent evt)
        {
            var bonuses = new List<string>(4);
            AppendBonus(bonuses, "Dist", evt.DistanceMultiplier);
            AppendBonus(bonuses, DifficultyName(evt.DifficultyLabel), evt.DifficultyMultiplier);
            AppendBonus(bonuses, "Basket", evt.BasketMultiplier);
            AppendBonus(bonuses, "Bounce", evt.WallBounceMultiplier);

            return bonuses.Count > 0
                ? $"+{evt.FinalPoints}\n<size=60%>{string.Join(" · ", bonuses)}</size>"
                : $"+{evt.FinalPoints}";
        }

        // Appends a "Name ×1.8" segment when the multiplier is a real bonus (above the show threshold).
        private static void AppendBonus(List<string> segments, string name, float multiplier)
        {
            if (multiplier > MinShownMultiplier && !string.IsNullOrEmpty(name))
                segments.Add($"{name} ×{multiplier:0.#}");
        }

        // The difficulty label is "Spin ×1.8" / "Aimed" / "Straight"; its leading word is the bonus name.
        private static string DifficultyName(string label) =>
            string.IsNullOrWhiteSpace(label) ? "Bonus" : label.Split(' ')[0];

        #region score-flash text VFX (UI, behind the text, clipped to its rect)

        // Highest-tier prefab whose minScore <= the points scored (null = no VFX for this score).
        private GameObject PickFlashVfxPrefab(int points)
        {
            GameObject best = null;
            var bestMin = int.MinValue;
            if (scoreFlashVfxTiers != null)
                foreach (var tier in scoreFlashVfxTiers)
                    if (tier.prefab && points >= tier.minScore && tier.minScore >= bestMin)
                        (best, bestMin) = (tier.prefab, tier.minScore);
            return best;
        }

        // Ensure the RectMask2D container (behind the label) exists, and that it holds an instance of the given
        // prefab — re-instantiating only when the tier prefab changes.
        private void EnsureFlashVfx(GameObject prefab)
        {
            if (!scoreFlashLabel || scoreFlashLabel.transform.parent is not RectTransform parent) return;

            if (!_flashVfxMask)
            {
                var maskObject = new GameObject("ScoreFlashTextVfxMask", typeof(RectTransform), typeof(RectMask2D));
                maskObject.transform.SetParent(parent, false);
                _flashVfxMask = (RectTransform)maskObject.transform;
                _flashVfxMask.anchorMin = _flashVfxMask.anchorMax = _flashVfxMask.pivot = new Vector2(0.5f, 0.5f);
                maskObject.transform.SetSiblingIndex(scoreFlashLabel.transform.GetSiblingIndex()); // behind the label
            }

            if (_flashVfxSource == prefab) return;
            if (_flashVfx) Destroy(_flashVfx);

            _flashVfx = Instantiate(prefab, _flashVfxMask);
            _flashVfxSource = prefab;

            // Fill the text WIDTH (stretch X); height is set proportionally each frame (see PositionFlashVfx),
            // centred vertically, and the RectMask2D clips anything taller than the text rect.
            if (_flashVfx.transform is RectTransform rt)
            {
                rt.anchorMin = new Vector2(0f, 0.5f);
                rt.anchorMax = new Vector2(1f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.offsetMin = new Vector2(0f, rt.offsetMin.y);
                rt.offsetMax = new Vector2(0f, rt.offsetMax.y);
            }
        }

        private void PlayFlashVfx(GameObject prefab)
        {
            if (!prefab) { if (_flashVfxMask) _flashVfxMask.gameObject.SetActive(false); return; }

            EnsureFlashVfx(prefab);
            if (!_flashVfxMask) return;

            _flashVfxMask.gameObject.SetActive(true);
            PositionFlashVfx();
        }

        // Keep the mask on the text's rect (so it clips to the text's delta size) and size the VFX to fill the
        // text WIDTH with a proportional (un-stretched) height — anything taller is hidden by the mask.
        private void PositionFlashVfx()
        {
            if (!_flashVfxMask || !scoreFlashLabel) return;

            var labelRect = scoreFlashLabel.rectTransform;
            _flashVfxMask.position  = labelRect.TransformPoint(labelRect.rect.center);
            _flashVfxMask.sizeDelta = labelRect.rect.size;

            if (_flashVfx && _flashVfx.transform is RectTransform rt)
            {
                var aspect = FlashVfxAspect();
                var width = _flashVfxMask.rect.width;
                rt.sizeDelta = new Vector2(0f, aspect > 0f ? width / aspect : width); // width follows the X-stretch
            }
        }

        // Aspect (w/h) of the VFX's current sprite, so the width-filled VFX keeps its proportions.
        private float FlashVfxAspect()
        {
            if (_flashVfx && _flashVfx.TryGetComponent<Image>(out var image) && image.sprite)
            {
                var rect = image.sprite.rect;
                if (rect.height > 0f) return rect.width / rect.height;
            }
            return 1f;
        }

        #endregion

        // Move the flash label's rect so the chosen anchor point sits at the basket's on-screen position. The
        // label stays a HUD/canvas element at its authored size — only its rect position changes (the basket
        // lives in the world, a separate layer; the text just lines up over it on screen).
        private void PositionFlashAtBasket()
        {
            var cam = CameraProvider.Current;
            if (!cam || !scoreFlashLabel) return;

            var screen = cam.WorldToScreenPoint(_flashWorldPoint);
            if (screen.z <= 0f) { scoreFlashLabel.gameObject.SetActive(false); return; } // basket is behind the camera

            var rect = scoreFlashLabel.rectTransform;
            rect.pivot = UIRenderUtils.GetPivot(scoreFlashBasketAnchor);

            if (rect.parent is not RectTransform parent) return;
            var canvas = scoreFlashLabel.GetComponentInParent<Canvas>();
            var uiCamera = canvas && canvas.renderMode == RenderMode.ScreenSpaceCamera ? canvas.worldCamera : null;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, (Vector2)screen + scoreFlashPinOffset, uiCamera, out var local))
                rect.anchoredPosition = local;
        }

        private void RefreshThrowModeLabel()
        {
            if (!DeveloperOptionMode && throwModeLabel)
            {
                throwModeLabel.text = "";
                throwModeLabel.gameObject.SetActive(false);
                return;
            }

            if (!throwModeLabel || !throwConfig) return;
            throwModeLabel.text = throwConfig.mode switch
            {
                ThrowMode.Straight => "Straight",
                ThrowMode.DragPath => "Aimed",
                ThrowMode.Enhanced => "Enhanced",
                _ => ""
            };
        }

        // Persistent banner of the STATIC multipliers active right now — the throw-mode bonus (Aimed's
        // dragPathBonus) and the selected basket's multiplier. Distance and Enhanced spin are per-throw
        // (dynamic), so they show only in the score flash, not here. Hidden when no static bonus applies.
        private void RefreshMultiplierBanner()
        {
            if (!multiplierBannerLabel) return;

            var basketMult = CurrentBasketMultiplier();
            var throwMult = CurrentThrowModeMultiplier();
            if (Mathf.Approximately(basketMult, _bannerBasketMultiplier) && Mathf.Approximately(throwMult, _bannerThrowMultiplier))
                return; // unchanged since last rebuild
            _bannerBasketMultiplier = basketMult;
            _bannerThrowMultiplier = throwMult;

            var segments = new List<string>(2);
            AppendBonus(segments, ThrowModeName(), throwMult);
            AppendBonus(segments, "Basket", basketMult);

            multiplierBannerLabel.text = string.Join(" · ", segments);
            multiplierBannerLabel.gameObject.SetActive(segments.Count > 0);
        }

        // The selected basket's score multiplier (1× if none selected or unset). Mirrors ScoringSystem.
        private static float CurrentBasketMultiplier()
        {
            var basket = ContentManager.Instance ? ContentManager.Instance.GetSelected<BasketData>(ContentTypes.Basket) : null;
            return basket && basket.scoreMultiplier > 0f ? basket.scoreMultiplier : 1f;
        }

        // The STATIC multiplier the current throw mode contributes: Aimed → dragPathBonus; Straight → 1×;
        // Enhanced → 1× here (its spin bonus is earned per-throw, so it appears on the flash instead).
        private float CurrentThrowModeMultiplier()
        {
            if (!throwConfig || !scoringConfig) return 1f;
            return throwConfig.mode == ThrowMode.DragPath ? scoringConfig.dragPathBonus : 1f;
        }

        private string ThrowModeName() => !throwConfig ? "" : throwConfig.mode switch
        {
            ThrowMode.Straight => "Straight",
            ThrowMode.DragPath => "Aimed",
            ThrowMode.Enhanced => "Spin",
            _ => ""
        };

        #endregion
    }
}
