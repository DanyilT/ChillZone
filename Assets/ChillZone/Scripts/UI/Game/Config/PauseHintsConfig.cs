using UnityEngine;

namespace ChillZone.UI.Game.Config
{
    /// <summary>
    /// Authoring asset for the rotating hint lines shown at the bottom of the pause overlay
    /// (see <see cref="PauseOverlay"/>), so the hints can be edited as data instead of inline arrays.
    /// </summary>
    [CreateAssetMenu(fileName = "PauseHints", menuName = "ChillZone/UI/Pause Hints")]
    public class PauseHintsConfig : ScriptableObject
    {
        [Tooltip("Hint lines cycled at the bottom of the pause overlay."), TextArea(1, 2)]
        public string[] hints =
        {
            "Tap anywhere to resume",
            "Swipe to aim — curve the swipe for spin",
            "Farther baskets are worth more points",
            "You can pause the game by navigating back",
            "Press back again to quit",
        };

        [Tooltip("Seconds each hint stays before switching to the next.")]
        public float hintInterval = 3f;
    }
}
