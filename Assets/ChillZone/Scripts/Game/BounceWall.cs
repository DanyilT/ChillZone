using UnityEngine;

namespace ChillZone.Game
{
    /// <summary>
    /// Marks a collider in the virtual environment (the box walls / ceiling) as a bounce surface: the ball
    /// rebounds off it WITHOUT counting as a miss, and a basket scored after a bounce earns the wall-bounce
    /// multiplier. Added at runtime by <see cref="VirtualEnvironmentController"/> — never on a prefab.
    /// </summary>
    public class BounceWall : MonoBehaviour { }
}
