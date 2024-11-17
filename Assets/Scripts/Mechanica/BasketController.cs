using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

public class BasketController : MonoBehaviour
{
    private Camera arCamera;
    private bool isDragging = false;
    private ARRaycastManager raycastManager;

    private void Awake()
    {
        arCamera = Camera.main;
        raycastManager = FindObjectOfType<ARRaycastManager>();
    }

    private void Update()
    {
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            Vector2 touchPosition = touch.position;
            Ray ray = arCamera.ScreenPointToRay(touchPosition);
            RaycastHit hit;

            if (touch.phase == TouchPhase.Began)
            {
                if (Physics.Raycast(ray, out hit))
                {
                    if (hit.collider.gameObject == gameObject)
                    {
                        isDragging = true;
                    }
                }
            }
            else if (touch.phase == TouchPhase.Moved && isDragging)
            {
                List<ARRaycastHit> hits = new List<ARRaycastHit>();
                if (raycastManager.Raycast(touchPosition, hits, UnityEngine.XR.ARSubsystems.TrackableType.Planes))
                {
                    Pose hitPose = hits[0].pose;
                    transform.position = hitPose.position;
                }
            }
            else if (touch.phase == TouchPhase.Ended)
            {
                isDragging = false;
            }
        }
    }
}
