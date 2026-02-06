using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Manages the playing of sounds with constraints on simultaneous sounds per type
/// </summary>
public class AudioService : IAudioService
{
    // Maximum simultaneous sounds allowed for each sound type.
    private int maxSimultaneousImpactSounds = 10;
    private int maxSimultaneousDeathSounds = 5;
    private int maxSimultaneousLaunchSounds = 20;
    private int maxSimultaneousOtherSounds = 10;
    private int maxSimultaneousUISounds = 100;

    // Tracks currently playing sounds by their type.
    private Dictionary<SoundType, List<PlayingSound>> activeSounds;

    public void Initialize()
    {
        activeSounds = new Dictionary<SoundType, List<PlayingSound>>();
    }

    public void Play(AudioCue data, AudioSource audioSource)
    {
        if (data == null)
        {
            Debug.LogWarning("AudioCue data is NULL! Cannot play sound.");
            return;
        }

        if (!Enum.IsDefined(typeof(SoundType), data.soundType))
        {
            Debug.LogWarning($"Invalid SoundType: {data.soundType}");
            return;
        }

        // UI sounds ignore simultaneous sound constraints.
        if (data.soundType == SoundType.UI)
        {
            CoroutineMonoBehavior.Instance.StartCoroutine(PlaySoundCoroutine(audioSource, data));
            return;
        }

        // Lazy initialization of activeSounds dictionary
        if (activeSounds == null)
        {
            Initialize();
        }

        // Initialize the list for the sound type if it doesn't exist.
        if (!activeSounds.ContainsKey(data.soundType))
        {
            activeSounds[data.soundType] = new List<PlayingSound>();
        }

        // Get the list of active sounds for the given type.
        List<PlayingSound> soundList = activeSounds[data.soundType];
        int maxSounds = GetMaxSimultaneousSounds(data.soundType);
        float now = Time.time;

        // Remove sounds that have finished playing.
        soundList.RemoveAll(s => now - s.StartTime > s.Cue.playDuration);

        // If the maximum number of sounds is reached, stop the oldest sound.
        if (soundList.Count >= maxSounds)
        {
            var oldest = soundList.OrderBy(s => s.StartTime).FirstOrDefault();
            if (oldest != null)
            {
                Debug.Log("Stopping oldest sound " + oldest.Cue.name + " because count is " + soundList.Count + " and max sound count is " + maxSounds);
                AudioCuePlayer.Stop(oldest.Cue, oldest.Source);
                soundList.Remove(oldest);
            }
        }

        if (CoroutineMonoBehavior.Instance == null)
        {
            Debug.LogError("CoroutineMonoBehavior instance is missing. Cannot play sound.");
            return;
        }

        // Start the coroutine to play the sound.
        CoroutineMonoBehavior.Instance.StartCoroutine(PlaySoundCoroutine(audioSource, data));

        // Add the new sound to the active sounds list.
        soundList.Add(new PlayingSound
        {
            Cue = data,
            Source = audioSource,
            StartTime = Time.time
        });
    }

    // Coroutine to play a sound and remove it from the active list once finished.
    private IEnumerator PlaySoundCoroutine(AudioSource audioSource, AudioCue audioCue)
    {
        if (audioSource == null)
        {
            Debug.LogWarning("AudioSource is NULL. Cannot play sound.");
            yield break;
        }

        AudioCuePlayer.Play(audioCue, audioSource);

        yield return new WaitForSeconds(audioCue.playDuration);

        // Remove the sound from the active list once it's done.
        if (activeSounds.TryGetValue(audioCue.soundType, out List<PlayingSound> soundList))
        {
            var entry = soundList.FirstOrDefault(s => s.Cue == audioCue && s.Source == audioSource);
            if (entry != null)
                soundList.Remove(entry);
        }
    }

    private int GetMaxSimultaneousSounds(SoundType soundType)
    {
        return soundType switch
        {
            SoundType.Impact => maxSimultaneousImpactSounds,
            SoundType.Death => maxSimultaneousDeathSounds,
            SoundType.Launch => maxSimultaneousLaunchSounds,
            SoundType.Other => maxSimultaneousOtherSounds,
            SoundType.UI => maxSimultaneousUISounds,
            _ => 100, // Default case if the type is not recognized.
        };
    }

    // Stops all active sounds and clears the lists.
    public void Dispose()
    {
        foreach (var list in activeSounds.Values)
        {
            foreach (var sound in list)
            {
                AudioCuePlayer.Stop(sound.Cue, sound.Source);
            }
            list.Clear();
        }
    }

    // Represents a currently playing sound with its cue, source, and start time.
    private class PlayingSound
    {
        public AudioCue Cue;
        public AudioSource Source;
        public float StartTime;
    }
}

// Defines the types of sounds.
public enum SoundType { Impact, Death, Launch, UI, Other }