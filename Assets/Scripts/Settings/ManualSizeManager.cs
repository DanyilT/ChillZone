using UnityEngine;
using System.Collections;

public class ManualSizeManager : MonoBehaviour
{
    [SerializeField] private GameObject score;
    void Start()
    {
        if (!IsTabletOrIPad())
        {
            Debug.Log("The device is not a tablet or iPad.");

            score.SetActive(true);
        }
        else
        {
            Debug.Log("The device is likely a tablet or iPad.");

            score.SetActive(false);
        }
    }

    private bool IsTabletOrIPad()
    {
        float aspectRatio = (float)Screen.width / Screen.height;
        float aspectRatioInverted = (float)Screen.height / Screen.width;

        // Check if the aspect ratio is close to 3:4 or 9:10
        return (Mathf.Approximately(aspectRatio, 0.75f) || Mathf.Approximately(aspectRatioInverted, 0.75f) ||
                Mathf.Approximately(aspectRatio, 0.9f) || Mathf.Approximately(aspectRatioInverted, 0.9f));
    }
}
