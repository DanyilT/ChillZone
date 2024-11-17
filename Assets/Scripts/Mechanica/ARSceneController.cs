using System.Collections.Generic;
using DG.Tweening;
using Unity.VisualScripting.Antlr3.Runtime;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using DG.Tweening;

public class ARSceneController : MonoBehaviour
{
    public ARRaycastManager raycastManager;
    public GameObject basketPrefab;
    public GameObject previewPanel;

    private GameObject spawnedBasket;

    private bool isPlaced = false;

    public PlaneDetectionController planeDetectionController;

    [SerializeField] private float animationDuration = 0.3f;
    ScoreManager scoreManager;

    private void Start()
    {
        scoreManager = GameObject.FindAnyObjectByType<ScoreManager>();
        previewPanel.GetComponent<RectTransform>().localScale = Vector3.zero;
    }

    private void Update()
    {
        // Show the previewPanel only when a plane is detected and the basket is not placed
        if (planeDetectionController.IsPlaneDetected && !isPlaced)
        {
            previewPanel.SetActive(!scoreManager.show);

            previewPanel.GetComponent<RectTransform>().DOScale(Vector3.one, animationDuration).SetEase(Ease.OutBack);
        }
        else
        {
            previewPanel.GetComponent<RectTransform>().localScale = Vector3.zero;
        }

        //previewPanel.SetActive(planeDetectionController.IsPlaneDetected && !isPlaced);

        if (planeDetectionController.IsPlaneDetected && !isPlaced)
        {
            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);
                if (touch.phase == TouchPhase.Began)
                {
                    Vector2 touchPosition = touch.position;
                    List<ARRaycastHit> hits = new List<ARRaycastHit>();
                    if (raycastManager.Raycast(touchPosition, hits, TrackableType.Planes))
                    {
                        Pose hitPose = hits[0].pose;
                        if (spawnedBasket == null)
                        {
                            spawnedBasket = Instantiate(basketPrefab, hitPose.position, basketPrefab.transform.rotation);
                            isPlaced = true;

                            previewPanel.GetComponent<RectTransform>().DOScale(Vector3.zero, animationDuration).SetEase(Ease.InBack);
                        }
                        else
                        {
                            spawnedBasket.transform.position = hitPose.position;
                        }
                    }
                }
            }
        }
    }
}
