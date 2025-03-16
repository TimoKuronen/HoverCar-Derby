using UnityEngine;

public class RepairCollectible : CollisionCollectible
{
    [SerializeField] private int repairAmount;
    public int RepairAmount => repairAmount;

    protected override void CollectItem(CollisionCollectible collectible, CarManager carManager)
    {
        carManager.CollectItem(this);
    }
}
