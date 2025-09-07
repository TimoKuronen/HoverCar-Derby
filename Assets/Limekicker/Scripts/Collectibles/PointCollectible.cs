using UnityEngine;

public class PointCollectible : CollisionCollectible
{
    [SerializeField] private int pointsAwared;
    protected override void CollectItem(CollisionCollectible collectible, CarManager carManager)
    {
        throw new System.NotImplementedException();
    }
}
