using UnityEngine;
using VContainer;

public class UIAudioPlayer : MonoBehaviour
{
    [Header("Countdown Audio Cues")]
    [SerializeField] private AudioCue countdownCue;
    [SerializeField] private AudioCue countdownGoCue;

    [Header("Audio Source")]
    [SerializeField] private AudioSource audioSource;

    private IAudioService audioService;
    private EventBinding<CountdownEvent> countdownEventBinding;

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

        countdownEventBinding = new EventBinding<CountdownEvent>(OnCountdownChanged);
        EventBus<CountdownEvent>.Register(countdownEventBinding);
    }

    private void OnDestroy()
    {
        if (countdownEventBinding != null)
        {
            EventBus<CountdownEvent>.Unregister(countdownEventBinding);
        }
    }

    private void OnCountdownChanged(CountdownEvent countdownEvent)
    {
        AudioCue cueToPlay = GetAudioCueForCountdown(countdownEvent.CountdownNumber);
        
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
