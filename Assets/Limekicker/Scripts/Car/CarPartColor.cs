using UnityEngine;

/// <summary>
/// Registers a mesh renderer with the car color painter by part type.
/// </summary>
public class CarPartColor : MonoBehaviour
{
    [SerializeField] private ColorType partColorType;

    private void Awake()
    {
        transform.root.GetComponent<CarColorPainter>().AddCarPart(partColorType, GetComponent<MeshRenderer>());
    }
}
