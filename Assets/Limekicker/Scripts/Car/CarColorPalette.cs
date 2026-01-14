using System.Collections.Generic;
using UnityEngine;

public enum ColorType
{
    Primary,
    Secondary,
    Decoration,
    Bumper
}

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

[System.Serializable]
public class CarColors
{
    public Color primaryColor;
    public Color secondaryColor;
    public Color decorationColor;
    public Color bumperColor;
}