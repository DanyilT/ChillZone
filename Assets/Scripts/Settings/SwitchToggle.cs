using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System.Numerics;
using Vector2 = UnityEngine.Vector2;

public class SwitchToggle : MonoBehaviour
{
    [SerializeField] private RectTransform handle;
    [SerializeField] private RectTransform background;
    [SerializeField] private Image backgroundImg;

    [SerializeField] private Color onColor;
    [SerializeField] private Color offColor;

    [SerializeField] private float animationDuration = 0.3f;

    private bool isOn;
    private Vector2 onPosition;
    private Vector2 offPosition;

    private void Start()
    {
        isOn = gameObject.GetComponent<Toggle>().isOn;

        onPosition = new Vector2(24, -8);
        offPosition = new Vector2(-24, -8);

        if (isOn)
        {
            handle.anchoredPosition = onPosition;
            backgroundImg.color = onColor;
        }
        else
        {
            handle.anchoredPosition = offPosition;
            backgroundImg.color = offColor;
        }
    }

    public void ToggleSwitch()
    {
        isOn = !isOn;
        if (isOn)
        {
            handle.DOAnchorPos(onPosition, animationDuration).SetEase(Ease.InOutSine);
            backgroundImg.DOColor(onColor, animationDuration).SetEase(Ease.InOutSine);
        }
        else
        {
            handle.DOAnchorPos(offPosition, animationDuration).SetEase(Ease.InOutSine);
            backgroundImg.DOColor(offColor, animationDuration).SetEase(Ease.InOutSine);
        }
    }
}
