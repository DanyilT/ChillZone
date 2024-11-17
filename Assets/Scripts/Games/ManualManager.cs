using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class ManualManager : MonoBehaviour
{
	[SerializeField] private GameObject manualWindow;
    [SerializeField] private GameObject maxScore;

    private bool isOpen = false;

	void Start()
	{
        manualWindow.SetActive(isOpen);
    }

    public void OpenManual()
	{
		isOpen = !isOpen;
		manualWindow.SetActive(isOpen);

        if (maxScore.activeInHierarchy && isOpen)
        {
            GameObject score = GameObject.FindGameObjectWithTag("Score");
            score.GetComponent<Button>().onClick.Invoke();
        }

        //if (prewiew.activeInHierarchy)
        //{
        //    prewiewActive = true;
        //    prewiew.SetActive(!isOpen);
        //}
        //else
        //{
        //    if (prewiewActive)
        //    {
        //        prewiew.SetActive(!isOpen);
        //    }
        //    prewiewActive = false;
        //}

        //if (maxScore.activeInHierarchy)
        //{
        //    maxScore.SetActive(!isOpen);
        //}
    }
}
