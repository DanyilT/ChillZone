namespace ChillZone.Core
{
    /// <summary>
    /// How a throw's direction (and spin) is derived from the drag. Lives in Core because it is a
    /// shared vocabulary referenced by Config, gameplay, events, and UI.
    /// </summary>
    public enum ThrowMode
    {
        /// <summary>Ball always flies straight forward from the camera. No lateral/vertical influence.</summary>
        Straight,

        /// <summary>Throw direction follows the drag vector on screen (lateral + vertical influence).</summary>
        DragPath,

        /// <summary>DragPath plus curvature analysis: curved swipe adds spin angular velocity.</summary>
        Enhanced,
    }
}
