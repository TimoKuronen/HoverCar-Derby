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
        // Validate index and carBody array
        if (carBody != null && carBody.Length > 0)
        {
            // Clamp index to valid range
            int clampedIndex = Mathf.Clamp(index, 0, carBody.Length - 1);
            for (int i = 0; i < carBody.Length; i++)
            {
                if (carBody[i] != null)
                    carBody[i].SetActive(i == clampedIndex);
            }
        }

        if (carColorPalette == null)
            carColorPalette = Resources.Load<CarColorPalette>("CarColorPalette");

        if (carColorPalette == null)
        {
            Debug.LogWarning("[CarColorPainter] CarColorPalette not found in Resources!");
            return;
        }

        // Clamp index to valid palette range (assuming max 8 colors, adjust if needed)
        int paletteIndex = Mathf.Clamp(index, 0, 7);
        CarColors selectedColors = carColorPalette.GetCarColors(paletteIndex);

        if (selectedColors == null)
        {
            Debug.LogWarning($"[CarColorPainter] Selected colors is null for index {paletteIndex}");
            return;
        }

        // Apply all material colors (with null checks)
        foreach (var r in primaryPartRenderers)
        {
            if (r != null && r.material != null)
                r.material.color = selectedColors.primaryColor;
        }
        foreach (var r in secondaryPartRenderers)
        {
            if (r != null && r.material != null)
                r.material.color = selectedColors.secondaryColor;
        }
        foreach (var r in decorationPartRenderers)
        {
            if (r != null && r.material != null)
                r.material.color = selectedColors.decorationColor;
        }
        foreach (var r in bumperPartRenderers)
        {
            if (r != null && r.material != null)
                r.material.color = selectedColors.bumperColor;
        }
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
