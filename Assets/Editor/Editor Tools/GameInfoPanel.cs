#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public class GameInfoPanel : EditorWindow
{
    bool subscribedToEvents = false;

    [MenuItem("Limekicker/GameInfoPanel")]
    public static void ShowWindow()
    {
        GetWindow<GameInfoPanel>("Game Info Panel");
    }

    private void OnGUI()
    {
        if (!EditorApplication.isPlaying)
        {
            subscribedToEvents = false;
            return;
        }

        if (!subscribedToEvents)
        {
            subscribedToEvents = true;
            SubToEvents();
        }

        GUILayout.Label("Game Info", EditorStyles.boldLabel);

        //foreach (var item in PlayerController.Instance.DamageManager.CarParts)
        //{
        //    EditorGUILayout.BeginHorizontal();
        //    EditorGUILayout.TextField(item.Key.ToString() + " - " + item.Value.CurrentHealth.ToString());
        //    EditorGUILayout.EndHorizontal();
        //}

        //float deltaX = DIBootstrapper.Container.Resolve<IInputManager>().GetSteer();
        //int gasValue = (int)DIBootstrapper.Container.Resolve<IInputManager>().GetGas();
        //float horizontalInput = Mathf.Clamp(deltaX / Screen.width, -1f, 1f);

        //EditorGUILayout.BeginHorizontal();
        //GUILayout.Label("Horizontal input: ");
        //EditorGUILayout.TextField(horizontalInput.ToString());
        //EditorGUILayout.EndHorizontal();
        //EditorGUILayout.BeginHorizontal();
        //GUILayout.Label("Gas input: ");
        //EditorGUILayout.TextField(gasValue.ToString());
        //EditorGUILayout.EndHorizontal();
    }

    void UpdateLoop()
    {
        Repaint();
    }

    private void SubToEvents()
    {
        EditorApplication.update += UpdateLoop;
    }

    void UnsubFromEvents()
    {
        EditorApplication.update -= UpdateLoop;
    }
}
#endif