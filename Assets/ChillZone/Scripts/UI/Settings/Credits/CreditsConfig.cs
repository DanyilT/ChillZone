using System;
using UnityEngine;

namespace ChillZone.UI.Settings.Credits
{
    /// <summary>
    /// Credits content shown by <see cref="CreditsOverlay"/>. Each line has a left "definition" and a right
    /// "author"; leave the author empty to center the definition full-width (e.g. "Made with Unity").
    /// </summary>
    [CreateAssetMenu(fileName = "CreditsConfig", menuName = "ChillZone/Settings/Credits Config")]
    public class CreditsConfig : ScriptableObject
    {
        public string title = "Credits";
        public string bottomHint = "Tap to close";

        public CreditEntry[] lines =
        {
            new () { definition = "ChillZone — DanyT" },
            new () { definition = "Design & Code", author = "Dany" },
            new () { definition = "IconPack (hand-drown)", author = "Dany" },
            new () { definition = "3d Models", author = "Poly by Google" },
            new () { definition = "Made with Unity" },
        };
    }

    [Serializable]
    public struct CreditEntry
    {
        [Tooltip("Left side — what it is. Centered full-width when Author is empty.")]
        public string definition;
        [Tooltip("Right side — who / what. Leave EMPTY to center the definition with no right column.")]
        public string author;
    }
}
