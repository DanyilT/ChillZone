using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;

public class PlaneDetectionController : MonoBehaviour
{
    public ARPlaneManager arPlaneManager;
    public GameObject scanImage;

    public bool IsPlaneDetected { get; private set; } = false;

    private void Start()
    {
        // Subscribe to plane detection events
        arPlaneManager.planesChanged += OnPlanesChanged;
    }

    private void OnDestroy()
    {
        // Unsubscribe from plane detection events
        arPlaneManager.planesChanged -= OnPlanesChanged;
    }

    private void OnPlanesChanged(ARPlanesChangedEventArgs args)
    {
        // Check if there are any detected planes
        if (arPlaneManager.trackables.count > 0)
        {
            // Hide the scanning image
            scanImage.SetActive(false);
            IsPlaneDetected = true;
        }
        else
        {
            // Show the scanning image
            scanImage.SetActive(true);
            IsPlaneDetected = false;
        }
    }
}
