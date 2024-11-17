using UnityEngine;
using System.Collections;
using TMPro;

public class ToggleTextAnimator : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI onText;
    [SerializeField] private TextMeshProUGUI offText;

    [SerializeField] private Color onColor;
    [SerializeField] private Color offColor;

    [SerializeField] private float animationDuration = 0.3f;

    public void AnimateToggleText(bool isOn)
    {
        Color targetOnTextColor = isOn ? onColor : offColor;
        Color targetOffTextColor = isOn ? offColor : onColor;

        onText.CrossFadeColor(targetOnTextColor, animationDuration, true, true);
        offText.CrossFadeColor(targetOffTextColor, animationDuration, true, true);
    }
}

