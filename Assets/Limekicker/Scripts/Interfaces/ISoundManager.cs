using UnityEngine;

public interface ISoundManager : IService
{
    void PlaySound(AudioSource audioSource, AudioCue data, float volume = 1.0f);
}