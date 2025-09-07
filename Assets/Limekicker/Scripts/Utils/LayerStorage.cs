using UnityEngine;

public static class LayerStorage
{
#if UNITY_EDITOR
    public static int PlayerLayer = LayerMask.NameToLayer("Player");
    public static int GroundLayer = LayerMask.NameToLayer("Default");
    public static int ObstacleLayer = LayerMask.NameToLayer("Obstacle");
    public static int CollectibleLayer = LayerMask.NameToLayer("Collectible");
#else
    public static int PlayerLayer { get; private set; }
    public static int GroundLayer { get; private set; }
    public static int ObstacleLayer { get; private set; }
    public static int CollectibleLayer { get; private set; }
#endif
    public static void SetLayerValues()
    {
        PlayerLayer = LayerMask.NameToLayer("Player");
        GroundLayer = LayerMask.NameToLayer("Default");
        ObstacleLayer = LayerMask.NameToLayer("Obstacle");
        CollectibleLayer = LayerMask.NameToLayer("Collectible");
    }
}
