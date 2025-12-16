using UnityEngine;

public class CarPartColor : MonoBehaviour
{
    [SerializeField] private ColorType partColorType;

    private void Awake()
    {
        transform.root.GetComponent<CarColorPainter>().AddCarPart(partColorType, GetComponent<MeshRenderer>());
    }
}
