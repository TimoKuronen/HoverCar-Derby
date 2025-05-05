using System;
using System.Collections;
using System.Collections.Generic;
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

        foreach (var item in PlayerController.Instance.DamageManager.CarParts)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.TextField(item.Key.ToString() + " - " + item.Value.CurrentHealth.ToString());
            EditorGUILayout.EndHorizontal();
        }
        
        Vector2 delta = Services.Get<IInputManager>().CurrentTouchPosition - Services.Get<IInputManager>().StartingTouchPosition;
        
        if (!Services.Get<IInputManager>().InputGiven)
            delta = Vector2.zero;

        float horizontalInput = Mathf.Clamp(delta.x / Screen.width, -1f, 1f);
        float verticalInput = Mathf.Clamp(delta.y / Screen.height, -1f, 1f);

        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("Horizontal input: ");
        EditorGUILayout.TextField(horizontalInput.ToString());
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("Vertical input: ");
        EditorGUILayout.TextField(verticalInput.ToString());
        EditorGUILayout.EndHorizontal();
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
