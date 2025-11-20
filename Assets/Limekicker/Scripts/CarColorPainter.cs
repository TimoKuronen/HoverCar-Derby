using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class CarColorPainter : NetworkBehaviour
{
    [Header("Visual Variants")]
    [SerializeField] private GameObject[] carBody;

    [Header("Palettes")]
    [SerializeField] private CarColorPalette carColorPalette;

    [Header("Parts")]
    [SerializeField] private List<MeshRenderer> primaryPartRenderers = new List<MeshRenderer>();
    [SerializeField] private List<MeshRenderer> secondaryPartRenderers = new List<MeshRenderer>();
    [SerializeField] private List<MeshRenderer> decorationPartRenderers = new List<MeshRenderer>();
    [SerializeField] private List<MeshRenderer> bumperPartRenderers = new List<MeshRenderer>();

    private NetworkVariable<int> colorIndex = new NetworkVariable<int>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public override void OnNetworkSpawn()
    {
        colorIndex.OnValueChanged += OnColorChanged;

        // Apply immediately also for late joiners
        OnColorChanged(0, colorIndex.Value);
    }

    public void AssignColor(int index)
    {
        if (IsServer)
        {
            colorIndex.Value = index;
        }
        else
        {
            SetColorServerRpc(index);
        }
    }

    [ServerRpc]
    private void SetColorServerRpc(int index)
    {
        colorIndex.Value = index;
    }

    private void OnColorChanged(int oldIndex, int newIndex)
    {
        ApplyColor(newIndex);
    }

    private void ApplyColor(int index)
    {
        for (int i = 0; i < carBody.Length; i++)
            carBody[i].SetActive(i == index);

        if (carColorPalette == null)
            carColorPalette = Resources.Load<CarColorPalette>("CarColorPalette");

        CarColors selectedColors = carColorPalette.GetCarColors(index);

        // Apply all material colors
        foreach (var r in primaryPartRenderers)
            r.material.color = selectedColors.primaryColor;
        foreach (var r in secondaryPartRenderers)
            r.material.color = selectedColors.secondaryColor;
        foreach (var r in decorationPartRenderers)
            r.material.color = selectedColors.decorationColor;
        foreach (var r in bumperPartRenderers)
            r.material.color = selectedColors.bumperColor;
    }

    public void AddCarPart(ColorType colorType, MeshRenderer renderer)
    {
        switch (colorType)
        {
            case ColorType.Primary:
                primaryPartRenderers.Add(renderer);
                break;

            case ColorType.Secondary:
                secondaryPartRenderers.Add(renderer);
                break;

            case ColorType.Decoration:
                decorationPartRenderers.Add(renderer);
                break;

            case ColorType.Bumper:
                bumperPartRenderers.Add(renderer);
                break;
        }
    }
}
