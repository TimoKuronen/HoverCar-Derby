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
    }

    private void SubToEvents()
    {
        PlayerController.Instance.OnPlayerCarDamaged += () => Repaint();
    }
}
