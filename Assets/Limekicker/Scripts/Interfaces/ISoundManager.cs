using UnityEngine;

public interface ISoundManager : IService
{
    void Play(AudioCue data, AudioSource audioSource);
}