using UnityEngine;
using System.Collections;
using UnityEngine.XR.OpenXR.Input;

public class iPadButtonPosition : MonoBehaviour
{
    [SerializeField] private RectTransform arToggle, settingsButton;

	// Use this for initialization
	void Start()
	{
        if (SystemInfo.deviceModel.Contains("iPad"))
        {
            float arToggleXPos = arToggle.position.x;
            float settingsButtonXPos = settingsButton.position.x;

            // Anchor
            arToggle.anchorMin = new Vector2(arToggle.anchorMin.x, 0);
            arToggle.anchorMax = new Vector2(arToggle.anchorMin.x, 0);

            // Pivot
            arToggle.pivot = new Vector2(arToggle.pivot.x, 0);

            // Position
            arToggle.anchoredPosition = new Vector2(0, 300);
            arToggle.position = new Vector2(arToggleXPos, arToggle.position.y);

            // Anchor
            settingsButton.anchorMin = new Vector2(settingsButton.anchorMin.x, 0);
            settingsButton.anchorMax = new Vector2(settingsButton.anchorMin.x, 0);

            // Pivot
            settingsButton.pivot = new Vector2(settingsButton.pivot.x, 0);

            // Position
            settingsButton.anchoredPosition = new Vector2(0, 300);
            settingsButton.position = new Vector2(settingsButtonXPos, settingsButton.position.y);
        }
    }
}

