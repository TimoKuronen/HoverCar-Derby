#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class SelectPrefabTool : EditorWindow
{
    private const string PrefabKey = "SelectedPrefabGUID";
    private const string AutoSelectKey = "AutoSelectPlayer";

    // Dictionary to store prefabs and their keys
    private Dictionary<string, Object> assets = new Dictionary<string, Object>();
    private bool autoSelectPlayer;

    [MenuItem("Limekicker/Select Prefab Tool")]
    public static void ShowWindow()
    {
        GetWindow<SelectPrefabTool>("Select Prefab Tool");
    }

    private void OnEnable()
    {
        assets[PrefabKey] = null;

        // Load saved assets and settings
        LoadAsset(PrefabKey, typeof(GameObject));
        autoSelectPlayer = EditorPrefs.GetBool(AutoSelectKey, false);

        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    private void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            OnPlayModeChanged();
        }
    }

    private void OnDisable()
    {
        // Unsubscribe to avoid memory leaks
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
    }

    void OnGUI()
    {
        GUILayout.Label("Select Prefabs and Weapon Data", EditorStyles.boldLabel);

        // Draw and update each asset slot
        DrawAssetField("Prefab", PrefabKey, typeof(GameObject));

        // Toggle for auto-select with saving state
        bool newAutoSelectPlayer = EditorGUILayout.Toggle("Autoselect Player", autoSelectPlayer);
        if (newAutoSelectPlayer != autoSelectPlayer)
        {
            autoSelectPlayer = newAutoSelectPlayer;
            EditorPrefs.SetBool(AutoSelectKey, autoSelectPlayer);
        }
    }

    private void DrawAssetField(string label, string key, System.Type assetType)
    {
        // Draw ObjectField for the given key
        Object newAsset = EditorGUILayout.ObjectField(label, assets[key], assetType, false);
        if (newAsset != assets[key])
        {
            assets[key] = newAsset;
            SaveAsset(key);
        }
    }

    private void SaveAsset(string key)
    {
        // Save the asset's GUID to EditorPrefs
        if (assets[key] != null)
        {
            string guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(assets[key]));
            EditorPrefs.SetString(key, guid);
        }
        else
        {
            EditorPrefs.DeleteKey(key); // Clear the saved asset if null
        }
    }

    private void LoadAsset(string key, System.Type assetType)
    {
        // Load the asset's GUID from EditorPrefs and assign to dictionary
        string savedGuid = EditorPrefs.GetString(key, string.Empty);
        if (!string.IsNullOrEmpty(savedGuid))
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(savedGuid);
            assets[key] = AssetDatabase.LoadAssetAtPath(assetPath, assetType);
        }
    }

    private void OnPlayModeChanged()
    {
        if (autoSelectPlayer)
        {
            Selection.activeGameObject = GameObject.FindGameObjectWithTag("Player");
        }
    }
}
#endif