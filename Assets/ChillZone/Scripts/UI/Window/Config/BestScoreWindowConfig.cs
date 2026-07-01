using System;
using ChillZone.UI.Utils.Config;
using TMPro;
using UnityEngine;

namespace ChillZone.UI.Window.Config
{
    /// <summary>
    /// A <see cref="WindowConfig"/> that displays one large dynamic value (the best score), styled
    /// independently of the shared <see cref="TextConfig"/>. The value is supplied at show time via
    /// <c>WindowShowOptions.PrimaryText</c>. Rendered by WindowObject's best-score branch — the header,
    /// backdrop, panel background and animation still come from the base config.
    /// </summary>
    [CreateAssetMenu(fileName = "BestScoreWindowConfig", menuName = "ChillZone/UI/Best Score Window Config", order = 023)]
    public class BestScoreWindowConfig : WindowConfig
    {
        public BestScoreConfig bestScoreConfig;

        // Render above the pause overlay (sort 1000) and the HUD (~1020) so the best-score window is visible and
        // on top when the player opens it while paused — not hidden behind the overlay until they resume.
        public override int CanvasSortingOrder => 1100;

        protected override void Reset()
        {
            base.Reset();
            windowId = "best-score";
            bestScoreConfig = BestScoreConfig.Default();
        }
    }

    [Serializable]
    public struct BestScoreConfig
    {
        [Header("Best Score — Value")]
        [Tooltip("How the value is formatted; {0} is the score. e.g. \"{0}\" or \"Best: {0}\".")]
        public string scoreFormat;
        [Tooltip("Base styling (font size, colour, style, alignment) for the score value. Alignment Center recommended.")]
        public TextConfig scoreTextConfig;

        [Header("Best Score — Value TMP overrides")]
        [Tooltip("Padding / offset around the score text (canvas units), applied as the TMP margin.")]
        public RectOffset scorePadding;
        [Tooltip("Minimum height of the score text box (canvas units).")]
        public float scoreMinHeight;
        [Tooltip("TMP character spacing for the score value.")]
        public float scoreCharacterSpacing;
        [Tooltip("TMP overflow mode for the score value (word-wrapping is always off).")]
        public TextOverflowModes scoreOverflow;

        public static BestScoreConfig Default() => new ()
        {
            scoreFormat = "{0}",
            scoreTextConfig = TextConfig.ScoreTextDefault(),
            scorePadding = new RectOffset(20, 20, 20, 20),
            scoreMinHeight = 400f,
            scoreCharacterSpacing = 40f,
            scoreOverflow = TextOverflowModes.Overflow
        };
    }
}
