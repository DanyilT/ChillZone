using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ChillZone.Gameplay
{
    /// <summary>
    /// Tests whether a screen-space pointer is over interactive HUD (the ball picker sheet,
    /// buttons, header, …) so the raw-pointer AR input — basket placement / move / delete and
    /// the ball drag — can ignore taps that land on the UI instead of letting them fall through
    /// to the world behind it.
    ///
    /// Uses an EventSystem raycast at the exact pointer position and keeps only
    /// <see cref="GraphicRaycaster"/> hits, so 3D physics hits (the ball, the basket, AR planes)
    /// never count as "UI". This is more reliable for touch than the parameterless
    /// <c>IsPointerOverGameObject()</c>, which can report a stale pointer.
    /// </summary>
    public static class PointerOverUI
    {
        private static readonly List<RaycastResult> Hits = new();

        public static bool At(Vector2 screenPosition)
        {
            var eventSystem = EventSystem.current;
            if (!eventSystem) return false;

            var data = new PointerEventData(eventSystem) { position = screenPosition };
            Hits.Clear();
            eventSystem.RaycastAll(data, Hits);

            return Hits.Any(hit => hit.module is GraphicRaycaster);
        }
    }
}
