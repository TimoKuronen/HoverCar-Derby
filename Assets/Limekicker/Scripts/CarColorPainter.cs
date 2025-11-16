using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarColorPainter : MonoBehaviour
{
    [SerializeField] private GameObject[] carBody;
    [SerializeField] private CarColorPalette carColorPalette;
    [SerializeField] private List<MeshRenderer> primaryPartRenderers = new List<MeshRenderer>();
    [SerializeField] private List<MeshRenderer> secondaryPartRenderers = new List<MeshRenderer>();
    [SerializeField] private List<MeshRenderer> decorationPartRenderers = new List<MeshRenderer>();
    [SerializeField] private List<MeshRenderer> bumperPartRenderers = new List<MeshRenderer>();

    public void AddCarPart(ColorType colorType, MeshRenderer partType)
    {
        switch (colorType)
        {
            case ColorType.Primary:
                primaryPartRenderers.Add(partType);
                break;
            case ColorType.Secondary:
                secondaryPartRenderers.Add(partType);
                break;
            case ColorType.Decoration:
                decorationPartRenderers.Add(partType);
                break;
            case ColorType.Bumper:
                bumperPartRenderers.Add(partType);
                break;
        }
    }

    public void AssignColor(int index)
    {
        Debug.Log("assigning color " + index);
        for (int i = 0; i < carBody.Length; i++)
        {
            carBody[i].SetActive(i == index);
        }

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
