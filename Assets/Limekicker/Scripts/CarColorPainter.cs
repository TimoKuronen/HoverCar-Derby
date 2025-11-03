using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarColorPainter : MonoBehaviour
{
    [SerializeField] private CarColorPalette carColorPalette;
    [SerializeField] private MeshRenderer[] primaryPartRenderers;
    [SerializeField] private MeshRenderer[] secondaryPartRenderers;
    [SerializeField] private MeshRenderer[] decorationPartRenderers;
    [SerializeField] private MeshRenderer[] bumperPartRenderers;

    IEnumerator Start()
    {
        // Wait one frame to ensure all components are initialized
        yield return null;
        AssignColor(PlayerPrefs.GetInt("SelectedCarColorIndex", Random.Range(0, 1)));
    }

    public void AssignColor(int index)
    {
        carColorPalette = carColorPalette == null ? Resources.Load<CarColorPalette>("CarColorPalette") : carColorPalette;
        CarColors selectedColors = carColorPalette.GetCarColors(index);

        foreach (var renderer in primaryPartRenderers)
        {
            renderer.material.color = selectedColors.primaryColor;
        }
        foreach (var renderer in secondaryPartRenderers)
        {
            renderer.material.color = selectedColors.secondaryColor;
        }
        foreach (var renderer in decorationPartRenderers)
        {
            renderer.material.color = selectedColors.decorationColor;
        }
        foreach (var renderer in bumperPartRenderers)
        {
            renderer.material.color = selectedColors.bumperColor;
        }
    }
}
