using UnityEngine;
using UnityEngine.UI;

public class AudioToggle : MonoBehaviour
{
    public Toggle SoundEffectsVolumeToggle;
    public Toggle MusicVolumeToggle;

    [SerializeField] private ToggleTextAnimator soundEffectsTextAnimator;
    [SerializeField] private ToggleTextAnimator musicTextAnimator;

    private AudioManager audioManager;

    void Start()
    {
        audioManager = AudioManager.Instance;

        SoundEffectsVolumeToggle.isOn = PlayerPrefs.GetInt("SoundEffectsVolume", 1) == 1 ? true : false;
        SoundEffectsVolumeToggle.onValueChanged.AddListener(OnSoundEffectsVolumeChanged);
        soundEffectsTextAnimator.AnimateToggleText(SoundEffectsVolumeToggle.isOn);

        MusicVolumeToggle.isOn = PlayerPrefs.GetInt("MusicVolume", 1) == 1 ? true : false;
        MusicVolumeToggle.onValueChanged.AddListener(OnMusicVolumeChanged);
        musicTextAnimator.AnimateToggleText(MusicVolumeToggle.isOn);
    }

    void OnSoundEffectsVolumeChanged(bool on)
    {
        audioManager.SetSoundEffectsVolume(on);
        soundEffectsTextAnimator.AnimateToggleText(on);
    }

    void OnMusicVolumeChanged(bool on)
    {
        audioManager.SetMusicVolume(on);
        musicTextAnimator.AnimateToggleText(on);
    }
}
