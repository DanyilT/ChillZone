using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class NotHit : MonoBehaviour
{
    public string targetTag = "Ball";

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.collider.CompareTag(targetTag))
        {
            PlayerPrefs.SetInt("ScoreKey", 0);

            GameObject reset = GameObject.FindGameObjectWithTag("Reset");
            reset.GetComponent<Button>().onClick.Invoke();
        }
    }
}
