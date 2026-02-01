using System.Collections;
using UnityEngine;

public class DamagingCollectible : CollisionCollectible
{
    [SerializeField] private int damageAmount;
    [SerializeField] private int lifetimeSeconds = 10;
    public int DamageAmount => damageAmount;

    protected override void CollectItem(CollisionCollectible collectible, CarManager carManager)
    {
        carManager.CollectItem(this);
    }

    private IEnumerator Start()
    {
        yield return new WaitForSeconds(lifetimeSeconds);
        Destroy(gameObject);
    }
}
