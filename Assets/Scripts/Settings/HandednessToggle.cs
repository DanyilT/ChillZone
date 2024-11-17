using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HandednessToggle : MonoBehaviour
{
    [SerializeField] private GameObject onToHide;
    [SerializeField] private GameObject offToHide;
    [SerializeField] private TextMeshProUGUI onText;
    [SerializeField] private TextMeshProUGUI offText;

    [SerializeField] private Toggle toggle;

    private Color onColor;
    private Color offColor;

    [SerializeField] private float animationDuration = 0.3f;

    private void Start()
    {
        onColor = onToHide.GetComponent<Image>().color;
        offColor = offToHide.GetComponent<Image>().color;

        // Load user preference from PlayerPrefs, default to left-handed if not set
        string handedPreference = PlayerPrefs.GetString(ButtonPositionManager.navigationPreferenceKey, "Right");

        // Set toggle state based on user preference
        toggle.isOn = handedPreference == "Right";

        onToHide.SetActive(toggle.isOn);
        offToHide.SetActive(!toggle.isOn);

        AnimateToggle(toggle.isOn);

        // Subscribe to toggle value changed event
        toggle.onValueChanged.AddListener(OnToggleValueChanged);
    }

    private void OnToggleValueChanged(bool isRightHanded)
    {
        onToHide.SetActive(isRightHanded);
        offToHide.SetActive(!isRightHanded);

        string handedPreference = isRightHanded ? "Right" : "Left";

        // Save user preference to PlayerPrefs
        PlayerPrefs.SetString(ButtonPositionManager.navigationPreferenceKey, handedPreference);

        // Apply changes to all button prefabs in the scene
        ButtonPositionManager[] buttonManagers = FindObjectsOfType<ButtonPositionManager>();
        foreach (ButtonPositionManager manager in buttonManagers)
        {
            manager.SetButtonPositions(handedPreference);
        }

        AnimateToggle(isRightHanded);
    }

    private void AnimateToggle(bool isRightHanded)
    {
        Color targetOnTextColor = isRightHanded ? onColor : offColor;
        Color targetOffTextColor = isRightHanded ? offColor : onColor;

        onText.CrossFadeColor(targetOnTextColor, animationDuration, true, true);
        offText.CrossFadeColor(targetOffTextColor, animationDuration, true, true);
    }
}
