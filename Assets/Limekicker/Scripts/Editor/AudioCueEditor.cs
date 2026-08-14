#if UNITY_EDITOR
using System.Reflection;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(AudioCue))]
public class AudioCueEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        var audioCue = (AudioCue)target;
        if (GUILayout.Button("Play Random Clip Preview"))
        {
            PlayRandomClipPreview(audioCue);
        }
    }

    static void PlayRandomClipPreview(AudioCue audioCue)
    {
        if (audioCue.clips == null || audioCue.clips.Length == 0)
        {
            Debug.LogWarning("No clips available to preview.");
            return;
        }

        AudioClip clipToPlay = audioCue.clips[Random.Range(0, audioCue.clips.Length)];
        if (clipToPlay == null)
        {
            Debug.LogWarning("Selected clip is null.");
            return;
        }

        PlayPreviewClip(clipToPlay);
    }

    static void PlayPreviewClip(AudioClip clip)
    {
        Assembly unityEditorAssembly = typeof(AudioImporter).Assembly;
        System.Type audioUtilClass = unityEditorAssembly.GetType("UnityEditor.AudioUtil");
        if (audioUtilClass == null)
        {
            Debug.LogError("Could not find UnityEditor.AudioUtil.");
            return;
        }

        MethodInfo playClip = audioUtilClass.GetMethod(
            "PlayPreviewClip",
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        if (playClip == null)
        {
            Debug.LogError("Could not find PlayPreviewClip method.");
            return;
        }

        playClip.Invoke(null, new object[] { clip, 0, false });
    }
}
#endif
