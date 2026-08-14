using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Identifies which car mesh part category receives a palette color.
/// </summary>
public enum ColorType
{
    Primary,
    Secondary,
    Decoration,
    Bumper
}

/// <summary>
/// ScriptableObject palette of car color schemes by index.
/// </summary>
[CreateAssetMenu(fileName = "CarColorPalette", menuName = "Limekicker/CarColorPalette", order = 1)]
public class CarColorPalette : ScriptableObject
{
    [SerializeField] private List<CarColors> carColorsList = new List<CarColors>();

    public CarColors GetCarColors(int index)
    {
        if (index < 0 || index >= carColorsList.Count)
        {
            Debug.LogWarning("Index out of range in CarColorPalette.GetCarColors");
            return null;
        }
        return carColorsList[index];
    }
}

/// <summary>
/// Serializable primary, secondary, decoration, and bumper colors for one scheme.
/// </summary>
[System.Serializable]
public class CarColors
{
    public Color primaryColor;
    public Color secondaryColor;
    public Color decorationColor;
    public Color bumperColor;
}