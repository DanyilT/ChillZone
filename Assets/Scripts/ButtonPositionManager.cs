using UnityEngine;
using UnityEngine.UI;

public class ButtonPositionManager : MonoBehaviour
{
    public const string navigationPreferenceKey = "NavigationPreference";

    public RectTransform[] buttons; // Array of UI buttons

    private void Start()
    {
        // Load user preference from PlayerPrefs, default to left-handed if not set
        string handedPreference = PlayerPrefs.GetString(navigationPreferenceKey, "Right");

        // Set button positions based on user preference
        SetButtonPositions(handedPreference);
    }

    public void SetButtonPositions(string handedPreference)
    {
        bool hended = handedPreference == "Right" ? true : false;
        if (hended)
        {
            // Move the UI elements that are on the right side
            foreach (RectTransform button in buttons)
            {
                float posY = button.position.y;
                if (gameObject.CompareTag("Reset") || gameObject.CompareTag("Back"))
                {
                    // Anchor
                    button.anchorMin = new Vector2(1, gameObject.GetComponent<RectTransform>().anchorMin.y);
                    button.anchorMax = new Vector2(1, gameObject.GetComponent<RectTransform>().anchorMax.y);

                    // Pivot
                    button.pivot = new Vector2(1, gameObject.GetComponent<RectTransform>().pivot.y);

                    // Position
                    button.anchoredPosition = new Vector2(-120, 0);
                    button.position = new Vector2(gameObject.GetComponent<RectTransform>().position.x, posY);
                }
                else
                {
                    // Anchor
                    button.anchorMin = new Vector2(0, gameObject.GetComponent<RectTransform>().anchorMin.y);
                    button.anchorMax = new Vector2(0, gameObject.GetComponent<RectTransform>().anchorMax.y);

                    // Pivot
                    button.pivot = new Vector2(0, gameObject.GetComponent<RectTransform>().pivot.y);

                    // Position
                    button.anchoredPosition = new Vector2(120, 0);
                    button.position = new Vector2(gameObject.GetComponent<RectTransform>().position.x, posY);
                }
            }
        }
        else
        {
            // Move the UI elements that are on the left side
            foreach (RectTransform button in buttons)
            {
                float posY = button.position.y;

                if (gameObject.CompareTag("Reset") || gameObject.CompareTag("Back"))
                {
                    // Anchor
                    button.anchorMin = new Vector2(0, gameObject.GetComponent<RectTransform>().anchorMin.y);
                    button.anchorMax = new Vector2(0, gameObject.GetComponent<RectTransform>().anchorMax.y);

                    // Pivot
                    button.pivot = new Vector2(0, gameObject.GetComponent<RectTransform>().pivot.y);

                    // Position
                    button.anchoredPosition = new Vector2(120, 0);
                    button.position = new Vector2(gameObject.GetComponent<RectTransform>().position.x, posY);
                }
                else
                {
                    // Anchor
                    button.anchorMin = new Vector2(1, gameObject.GetComponent<RectTransform>().anchorMin.y);
                    button.anchorMax = new Vector2(1, gameObject.GetComponent<RectTransform>().anchorMax.y);

                    // Pivot
                    button.pivot = new Vector2(1, gameObject.GetComponent<RectTransform>().pivot.y);

                    // Position
                    button.anchoredPosition = new Vector2(-120, 0);
                    button.position = new Vector2(gameObject.GetComponent<RectTransform>().position.x, posY);
                }
            }
        }
    }
}
