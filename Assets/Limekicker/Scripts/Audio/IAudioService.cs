using UnityEngine;

/// <summary>
/// Contract for playing audio cues through managed audio sources.
/// </summary>
public interface IAudioService
{
    void Play(AudioCue data, AudioSource audioSource);
}