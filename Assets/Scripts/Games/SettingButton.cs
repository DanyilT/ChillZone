using UnityEngine;
using UnityEngine.SceneManagement;

public class SettingButton : MonoBehaviour
{
    private string settingsSceneName = "Settings"; // Name of the settings scene

    public void OpenSettings()
    {
        SceneManager.LoadScene(settingsSceneName); // Load settings scene
    }
}
