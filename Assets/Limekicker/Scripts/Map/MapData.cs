using UnityEngine;

/// <summary>
/// Playable area of a map: a rectangle on the XZ plane and the ground elevation band in which
/// gameplay objects may be placed.
/// </summary>
[CreateAssetMenu(fileName = "MapData", menuName = "HoverCar/Map Data")]
public class MapData : ScriptableObject
{
    [field: SerializeField] public Vector2 Center { get; private set; } = Vector2.zero;
    [field: SerializeField] public Vector2 Size { get; private set; } = new Vector2(120f, 120f);
    [field: SerializeField] public float MinGroundHeight { get; private set; } = 0f;
    [field: SerializeField] public float MaxGroundHeight { get; private set; } = 1f;

    /// <summary>
    /// Uniformly random point inside the map rectangle, as (x, z).
    /// </summary>
    public Vector2 GetRandomPoint()
    {
        Vector2 half = Size * 0.5f;
        return new Vector2(
            Center.x + Random.Range(-half.x, half.x),
            Center.y + Random.Range(-half.y, half.y));
    }

    public bool IsGroundHeight(float height)
    {
        return height >= MinGroundHeight && height <= MaxGroundHeight;
    }
}
