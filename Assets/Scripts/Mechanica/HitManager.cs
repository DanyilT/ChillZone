using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HitManager: MonoBehaviour
{
    public string targetTag = "Ball";

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.collider.CompareTag(targetTag))
        {
            // Perform your action here
            Debug.Log("Collision detected between objects with tags: " + gameObject.tag + " and " + targetTag);

            PlayerPrefs.SetInt("ScoreKey", PlayerPrefs.GetInt("ScoreKey", 0) + 1);

            GameObject reset = GameObject.FindGameObjectWithTag("Reset");
            reset.GetComponent<Button>().onClick.Invoke();
        }
    }
}
