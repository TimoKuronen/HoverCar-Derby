using System.Reflection;
using UnityEditor;
using UnityEngine;

[CreateAssetMenu(fileName = "AudioCue", menuName = "Limekicker/AudioCue", order = 1)]
public class AudioCue : ScriptableObject
{
    public bool loop;
    public bool randomize = true;
    public bool dontAdjustVolume;
    public bool stopPrevious;
    public SoundType soundType;
    public AudioClip[] clips;
    public AudioClip[] overlayClips;
    [Range(0f, 1f)] public float minVolume = 1f;
    [Range(0f, 1f)] public float maxVolume = 1f;
    [Range(0f, 2f)] public float minPitch = 0.95f;
    [Range(0f, 2f)] public float maxPitch = 1.05f;
    [Range(0f, 1f)] public float forcedPitch = 1f;
    [Range(0f, 1f)] public float ovrMinVolume = 1f;
    [Range(0f, 1f)] public float ovrMaxVolume = 1f;
    [Range(0f, 1f)] public float volumeMultiplier = 1f;
    [Range(0f, 1f)] public float fadeInDuration = 0f;
    [Range(0f, 1f)] public float fadeOutDuration = 0f;
    [Range(0f, 1f)] public float delay = 0f;
    public float playDuration = 1f;

#if UNITY_EDITOR
    [Button]
    void PlayRandomClipInEditor()
    {
        Assembly unityEditorAssembly = typeof(AudioImporter).Assembly;
        System.Type audioUtilClass = unityEditorAssembly.GetType("UnityEditor.AudioUtil");

        if (audioUtilClass == null)
        {
            Debug.LogError("Could not find UnityEditor.AudioUtil!");
            return;
        }

        MethodInfo playClip = audioUtilClass.GetMethod("PlayPreviewClip", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

        if (playClip == null)
        {
            Debug.LogError("Could not find PlayPreviewClip method!");
            return;
        }

        playClip.Invoke(null, new object[] { clips[Random.Range(0, clips.Length)], 0, false });
    }
#endif
}