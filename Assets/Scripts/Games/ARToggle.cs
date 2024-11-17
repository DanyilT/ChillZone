using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

public class ARToggle : MonoBehaviour
{
    public ARPlaneManager arPlaneManager;
	public ARSession arSession; // Reference to the ARSession component
    public GameObject arCamera; // Reference to the AR camera GameObject
    public GameObject arModeOnIcon; // The icon to show when AR mode is on
    public GameObject arModeOffIcon; // The icon to show when AR mode is off

    private bool arModeEnabled; // Flag to track if AR mode is currently enabled

    [SerializeField] private GameObject offPanel;

    void Start()
	{
        arModeEnabled = true;

        // Change button icon based on AR mode
        arModeOnIcon.SetActive(!arModeEnabled);
        arModeOffIcon.SetActive(arModeEnabled);
    }

    public void ToggleARMode()
    {
        arModeEnabled = !arModeEnabled; // Invert the AR mode flag

        // Enable/disable AR components
        arSession.enabled = arModeEnabled;
        arCamera.SetActive(arModeEnabled);
        arPlaneManager.enabled = arModeEnabled;
        offPanel.SetActive(!arModeEnabled);

        // Change button icon based on AR mode
        arModeOnIcon.SetActive(!arModeEnabled);
        arModeOffIcon.SetActive(arModeEnabled);

    }
}
