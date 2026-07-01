using UnityEngine;
using UnityEngine.UI;

namespace ChillZone.UI.Button1
{
    /// <summary>
    /// Swaps a button's icon between an ON and OFF state. Added at runtime by
    /// <see cref="ButtonManager"/> only for buttons whose config has <c>isToggle</c>
    /// enabled. Single-event buttons don't get this component (and need no OFF icon).
    ///
    /// The toggle is self-contained: it flips on every click. Call <see cref="SetState"/>
    /// to sync it to external state if something other than the button can change it.
    /// </summary>
    public class ButtonToggleVisual : MonoBehaviour
    {
        private Image _icon;
        private Color _onColor;
        private Sprite _onSprite;
        private Color _offColor;
        private Sprite _offSprite;

        private bool IsOn { get; set; } = true;

        public void Init(Image icon, Color onColor, Sprite onSprite, Color offColor, Sprite offSprite, bool startsOn = true)
        {
            _icon = icon;
            _onColor = onColor;
            _onSprite = onSprite;
            _offColor = offColor;
            _offSprite = offSprite ? offSprite : onSprite; // fall back to ON icon if no OFF icon supplied
            IsOn = startsOn;
            Apply();
        }

        /// <summary>Flip the toggle. Wired to the button's onClick by ButtonManager.</summary>
        public void Toggle() => SetState(!IsOn);

        public void SetState(bool isOn) { IsOn = isOn; Apply(); }

        private void Apply()
        {
            if (!_icon) return;
            _icon.sprite = IsOn ? _onSprite : _offSprite;
            _icon.color = IsOn ? _onColor : _offColor;
        }
    }
}
