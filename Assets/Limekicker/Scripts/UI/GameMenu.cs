using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameMenu : MonoBehaviour
{
    [SerializeField] private Toggle sfxToggle;
    [SerializeField] private Toggle musicToggle;
    [SerializeField] private Button quitButton;
    [SerializeField] private TMP_Text sfxLabel;
    [SerializeField] private TMP_Text musicLabel;
    [SerializeField] private AudioSource[] musicSources;

    private void Awake()
    {
        if (sfxToggle != null)
        {
            sfxToggle.SetIsOnWithoutNotify(AudioPreferences.IsSfxEnabled());
            sfxToggle.onValueChanged.AddListener(SetSfxEnabled);
        }

        if (musicToggle != null)
        {
            musicToggle.SetIsOnWithoutNotify(AudioPreferences.IsMusicEnabled());
            musicToggle.onValueChanged.AddListener(SetMusicEnabled);
        }

        if (quitButton != null)
            quitButton.onClick.AddListener(QuitGame);

        ApplyAudioStateToUI();
    }

    private void SetSfxEnabled(bool enabled)
    {
        AudioPreferences.SetSfxEnabled(enabled);
        ApplyAudioStateToUI();
    }

    private void SetMusicEnabled(bool enabled)
    {
        AudioPreferences.SetMusicEnabled(enabled);
        ApplyAudioStateToUI();
    }

    private void QuitGame()
    {
        NetworkSession.LeaveGame();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void ApplyAudioStateToUI()
    {
        bool sfxEnabled = AudioPreferences.IsSfxEnabled();
        bool musicEnabled = AudioPreferences.IsMusicEnabled();

        if (sfxToggle != null && sfxToggle.isOn != sfxEnabled)
            sfxToggle.SetIsOnWithoutNotify(sfxEnabled);
        if (musicToggle != null && musicToggle.isOn != musicEnabled)
            musicToggle.SetIsOnWithoutNotify(musicEnabled);

        if (sfxLabel != null)
            sfxLabel.text = sfxEnabled ? "SFX On" : "SFX Off";
        if (musicLabel != null)
            musicLabel.text = musicEnabled ? "Music On" : "Music Off";

        if (musicSources != null)
        {
            for (int i = 0; i < musicSources.Length; i++)
            {
                if (musicSources[i] != null)
                    musicSources[i].mute = !musicEnabled;
            }
        }
    }

    private void OnDestroy()
    {
        if (sfxToggle != null)
            sfxToggle.onValueChanged.RemoveListener(SetSfxEnabled);
        if (musicToggle != null)
            musicToggle.onValueChanged.RemoveListener(SetMusicEnabled);
    }
}
