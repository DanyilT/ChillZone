using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    public AudioSource soundEffectsSource;
    public AudioSource musicSource;

    public AudioClip[] soundEffectsClip;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

        SetSoundEffectsVolume(PlayerPrefs.GetInt("SoundEffectsVolume", 1) == 1 ? true : false);
        SetMusicVolume(PlayerPrefs.GetInt("MusicVolume", 1) == 1 ? true : false);
    }

    public void PlayButtonClick(int num)
    {
        soundEffectsSource.PlayOneShot(soundEffectsClip[num]);
    }

    public void SetSoundEffectsVolume(bool on)
    {
        soundEffectsSource.volume = on ? 1 : 0;
        PlayerPrefs.SetInt("SoundEffectsVolume", on ? 1 : 0);
    }

    public void SetMusicVolume(bool on)
    {
        musicSource.volume = on ? 1 : 0;
        PlayerPrefs.SetInt("MusicVolume", on ? 1 : 0);
    }
}
