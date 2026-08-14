using UnityEngine;
using VContainer;

/// <summary>
/// Plays countdown and go sounds driven by countdown variable changes.
/// </summary>
public class UIAudioPlayer : MonoBehaviour
{
    [Header("Countdown Audio Cues")]
    [SerializeField] private AudioCue countdownCue;
    [SerializeField] private AudioCue countdownGoCue;

    [Header("Audio Source")]
    [SerializeField] private AudioSource audioSource;

    [Header("Countdown Variable")]
    [SerializeField] private IntVariable countdownValue;

    private IAudioService audioService;

    [Inject]
    public void Construct(IAudioService audioService)
    {
        this.audioService = audioService;
    }

    private void Awake()
    {
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }
    }

    private void Start()
    {
        if (countdownValue != null)
        {
            countdownValue.OnValueChanged += OnCountdownValueChanged;
        }
    }

    private void OnDestroy()
    {
        if (countdownValue != null)
        {
            countdownValue.OnValueChanged -= OnCountdownValueChanged;
        }
    }

    private void OnCountdownValueChanged(int countdownNumber)
    {
        // Only play audio for valid countdown values (0-3)
        if (countdownNumber < 0 || countdownNumber > 3)
            return;

        AudioCue cueToPlay = GetAudioCueForCountdown(countdownNumber);
        
        if (cueToPlay != null && audioSource != null && audioService != null)
        {
            audioService.Play(cueToPlay, audioSource);
        }
    }

    private AudioCue GetAudioCueForCountdown(int countdownNumber)
    {
        return countdownNumber == 0 ? countdownGoCue : countdownCue;
    }

    public void PlayCountdownSound(int countdownNumber)
    {
        AudioCue cueToPlay = GetAudioCueForCountdown(countdownNumber);
        
        if (cueToPlay != null && audioSource != null && audioService != null)
        {
            audioService.Play(cueToPlay, audioSource);
        }
    }
}
