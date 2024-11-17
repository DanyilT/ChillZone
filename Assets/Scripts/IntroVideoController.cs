using UnityEngine;
using UnityEngine.Video;
using UnityEngine.SceneManagement;
using System.Collections;

public class IntroVideoController : MonoBehaviour
{
    public VideoPlayer videoPlayer;
    public string nextSceneName = "Game";
    public float maxPlayTime = 2.0f;

    private float elapsedTime;
    private AsyncOperation asyncOperation;

    void Start()
    {
        videoPlayer.loopPointReached += OnVideoFinished;
        StartCoroutine(PreloadNextScene());
    }

    IEnumerator PreloadNextScene()
    {
        asyncOperation = SceneManager.LoadSceneAsync(nextSceneName);
        asyncOperation.allowSceneActivation = false;
        yield return null;
    }

    void Update()
    {
        elapsedTime += Time.deltaTime;

        if (elapsedTime >= maxPlayTime || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began))
        {
            DisableVideoPlayer();
            ActivateNextScene();
        }
    }

    void OnVideoFinished(VideoPlayer vp)
    {
        DisableVideoPlayer();
        ActivateNextScene();
    }

    void ActivateNextScene()
    {
        if (asyncOperation != null && !asyncOperation.allowSceneActivation)
        {
            asyncOperation.allowSceneActivation = true;
        }
    }

    void DisableVideoPlayer()
    {
        videoPlayer.enabled = false;
    }

    //IEnumerator LoadSceneAsync(string sceneName)
    //{
    //    AsyncOperation asyncOperation = SceneManager.LoadSceneAsync(sceneName);

    //    while (!asyncOperation.isDone)
    //    {
    //        // You can show a loading screen or progress bar here if needed
    //        yield return null;
    //    }
    //}
}
