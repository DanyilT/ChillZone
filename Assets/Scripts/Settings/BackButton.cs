using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class BackButton : MonoBehaviour
{
    //private int previousSceneIndex = SceneManager.GetActiveScene().buildIndex - 1;
    private string sceneName = "Game";

    private AsyncOperation asyncOperation;

    private void Start()
    {
        StartCoroutine(PreloadNextScene());
    }

    IEnumerator PreloadNextScene()
    {
        asyncOperation = SceneManager.LoadSceneAsync(sceneName);
        asyncOperation.allowSceneActivation = false;
        yield return null;
    }

    public void GoBack()
    {
        ActivateNextScene();
    }

    public void ActivateNextScene()
    {
        if (asyncOperation != null && !asyncOperation.allowSceneActivation)
        {
            asyncOperation.allowSceneActivation = true;
        }
    }
}
