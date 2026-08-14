using UnityEngine;

/// <summary>
/// Scriptable definition of clips, volume, pitch, and playback rules for a sound.
/// </summary>
[CreateAssetMenu(fileName = "AudioCue", menuName = "Limekicker/AudioCue", order = 1)]
public class AudioCue : ScriptableObject
{
    [Header("Playback Settings")]
    public bool loop;
    public bool randomize = true;
    [Tooltip("If true, play all clips simultaneously instead of just one")]
    public bool playAllClips = false;
    public bool dontAdjustVolume;
    public bool stopPrevious;

    [Header("Sound Classification")]
    public SoundType soundType;

    [Header("Audio Clips")]
    [Tooltip("Main audio clips. If playAllClips is false, one random clip is selected.")]
    public AudioClip[] clips;

    [Tooltip("Additional clips that play simultaneously with main clip(s). Only plays when not looping.")]
    public AudioClip[] overlayClips;
    public bool playAllOverlayClips = false;

    [Header("Volume Settings")]
    [Range(0f, 1f)] public float minVolume = 1f;
    [Range(0f, 1f)] public float maxVolume = 1f;
    [Range(0f, 1f)] public float volumeMultiplier = 1f;
    [Range(0f, 1f)] public float ovrMinVolume = 1f;
    [Range(0f, 1f)] public float ovrMaxVolume = 1f;

    [Header("Pitch Settings")]
    [Range(0f, 2f)] public float minPitch = 0.95f;
    [Range(0f, 2f)] public float maxPitch = 1.05f;
    [Range(0f, 1f)] public float forcedPitch = 1f;

    [Header("Timing Settings")]
    [Tooltip("Duration in seconds that this sound is considered 'playing' for audio management")]
    public float playDuration = 1f;
}
