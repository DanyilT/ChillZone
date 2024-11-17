using UnityEngine;
using TMPro;
using System.Numerics;
using Vector2 = UnityEngine.Vector2;

public class MarqueeController : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI marqueeText;
    [SerializeField] private float scrollSpeed = 100f;

    private RectTransform rectTransform;
    private float textWidth;

    private void Start()
    {
        rectTransform = marqueeText.GetComponent<RectTransform>();
        textWidth = marqueeText.preferredWidth;
        rectTransform.anchoredPosition = new Vector2(textWidth/2, 0);
    }

    private void Update()
    {
        float newX = rectTransform.anchoredPosition.x - scrollSpeed * Time.deltaTime;

        if (newX < -textWidth)
        {
            newX = textWidth/2;
        }

        rectTransform.anchoredPosition = new Vector2(newX, 0);
    }
}
