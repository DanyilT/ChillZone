using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class Counting : MonoBehaviour
{
    private Coroutine restartCheckCoroutine;

    [SerializeField] public GameObject ball;

    private void Update()
    {
        if (!ball.activeInHierarchy)
        {
            if (restartCheckCoroutine == null)
            {
                restartCheckCoroutine = StartCoroutine(CheckRestartCall());
            }
        }
        else
        {
            if (restartCheckCoroutine != null)
            {
                StopCoroutine(restartCheckCoroutine);
                restartCheckCoroutine = null;
            }
        }

    }
    public IEnumerator CheckRestartCall()
    {
        yield return new WaitForSeconds(2);

        Restart();
    }

    public void Restart()
    {
        PlayerPrefs.SetInt("ScoreKey", 0);

        GameObject reset = GameObject.FindGameObjectWithTag("Reset");
        reset.GetComponent<Button>().onClick.Invoke();
    }
}

