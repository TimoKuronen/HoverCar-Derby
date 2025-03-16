using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DamagingCollectible : CollisionCollectible
{
    [SerializeField] private int damageAmount;
    public int DamageAmount => damageAmount;

    protected override void CollectItem(CollisionCollectible collectible, CarManager carManager)
    {
        carManager.CollectItem(this);
    }
}
