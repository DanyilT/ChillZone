namespace ChillZone.Core
{
    /// <summary>
    /// Central registry of Unity tag strings used in code. Keeps tag literals out of
    /// gameplay scripts so a rename happens in exactly one place (and is greppable).
    /// The matching tags must still exist in the Tags &amp; Layers project settings.
    /// </summary>
    public static class GameTags
    {
        /// <summary>Thin trigger collider at the basket hoop; a live ball entering it scores.</summary>
        public const string ScoreZone = "ScoreZone";

        /// <summary>Optional root object that thrown balls are parented under.</summary>
        public const string BallPool = "BallPool";
    }
}
