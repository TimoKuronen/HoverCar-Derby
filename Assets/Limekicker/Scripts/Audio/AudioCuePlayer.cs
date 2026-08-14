using UnityEngine;

/// <summary>
/// Static and component helpers for playing and stopping audio cue clips.
/// </summary>
public class AudioCuePlayer : MonoBehaviour
{
    public AudioSource audioSource;
    public bool playOnAwake = false;
    public AudioCue audioCue;

    private void Awake()
    {
        if (playOnAwake && audioCue != null && audioSource != null)
        {
            Play(audioCue, audioSource);
        }
    }

    public static int Play(AudioCue cue, AudioSource audioSource, int previousClipIndex = -1)
    {
        if (!ValidatePlayback(cue, audioSource))
            return -1;

        if (cue.stopPrevious)
            audioSource.Stop();

        float volume = CalculateVolume(cue, audioSource);
        if (!cue.dontAdjustVolume)
            audioSource.volume = volume;

        audioSource.pitch = Random.Range(cue.minPitch, cue.maxPitch);

        if (cue.playAllClips)
        {
            PlayAllClips(cue, audioSource, volume);
            return 0;
        }
        else
        {
            int clipIndex = SelectClipIndex(cue, previousClipIndex);
            if (clipIndex < 0)
                return -1;

            PlaySingleClip(cue, audioSource, clipIndex, volume);
            return clipIndex;
        }
    }

    private static bool ValidatePlayback(AudioCue cue, AudioSource audioSource)
    {
        if (cue == null)
        {
            Debug.LogWarning("AudioCuePlayer: Cannot play - AudioCue is null.");
            return false;
        }

        if (audioSource == null)
        {
            Debug.LogWarning("AudioCuePlayer: Cannot play - AudioSource is null.");
            return false;
        }

        if (!audioSource.isActiveAndEnabled)
        {
            Debug.LogWarning("AudioCuePlayer: Cannot play - AudioSource is not active or enabled.");
            return false;
        }

        if (cue.clips == null || cue.clips.Length == 0)
        {
            Debug.LogWarning($"AudioCuePlayer: Cannot play '{cue.name}' - no clips assigned.");
            return false;
        }

        return true;
    }

    private static float CalculateVolume(AudioCue cue, AudioSource audioSource)
    {
        if (cue.dontAdjustVolume)
            return audioSource.volume;

        return Random.Range(cue.minVolume, cue.maxVolume) * cue.volumeMultiplier;
    }

    private static int SelectClipIndex(AudioCue cue, int previousClipIndex)
    {
        if (cue.randomize)
        {
            int clipIndex = Random.Range(0, cue.clips.Length);

            if (clipIndex == previousClipIndex && cue.clips.Length > 1)
            {
                clipIndex = (clipIndex + 1) % cue.clips.Length;
            }

            return clipIndex;
        }
        else
        {
            return 0;
        }
    }

    private static void PlayAllClips(AudioCue cue, AudioSource audioSource, float volume)
    {
        if (cue.loop)
        {
            audioSource.loop = true;
            audioSource.clip = cue.clips[0];
            audioSource.volume = volume;
            audioSource.Play();

            for (int i = 1; i < cue.clips.Length; i++)
            {
                if (cue.clips[i] != null)
                    audioSource.PlayOneShot(cue.clips[i], volume);
            }
        }
        else
        {
            foreach (AudioClip clip in cue.clips)
            {
                if (clip != null)
                    audioSource.PlayOneShot(clip, volume);
            }
        }

        PlayOverlayClips(cue, audioSource);
    }

    private static void PlaySingleClip(AudioCue cue, AudioSource audioSource, int clipIndex, float volume)
    {
        AudioClip clip = cue.clips[clipIndex];
        if (clip == null)
        {
            Debug.LogWarning($"AudioCuePlayer: Clip at index {clipIndex} is null in '{cue.name}'.");
            return;
        }

        if (cue.loop)
        {
            audioSource.loop = true;
            audioSource.clip = clip;
            audioSource.volume = volume;
            audioSource.Play();
        }
        else
        {
            audioSource.PlayOneShot(clip, volume);
            PlayOverlayClips(cue, audioSource);
        }
    }

    private static void PlayOverlayClips(AudioCue cue, AudioSource audioSource)
    {
        if (cue.loop || cue.overlayClips == null || cue.overlayClips.Length == 0)
            return;

        float overlayVolume = Random.Range(cue.ovrMinVolume, cue.ovrMaxVolume) * cue.volumeMultiplier;

        if (cue.playAllOverlayClips)
        {
            foreach (AudioClip overlayClip in cue.overlayClips)
            {
                if (overlayClip != null)
                    audioSource.PlayOneShot(overlayClip, overlayVolume);
            }
        }
        else
        {
            AudioClip selectedOverlay = cue.overlayClips[Random.Range(0, cue.overlayClips.Length)];
            if (selectedOverlay != null)
                audioSource.PlayOneShot(selectedOverlay, overlayVolume);
        }
    }

    public static void Stop(AudioCue cue, AudioSource audioSource)
    {
        if (audioSource == null)
            return;

        audioSource.loop = false;
        audioSource.Stop();
    }

    public static bool IsPlaying(AudioSource audioSource)
    {
        return audioSource != null && audioSource.isPlaying;
    }
}
