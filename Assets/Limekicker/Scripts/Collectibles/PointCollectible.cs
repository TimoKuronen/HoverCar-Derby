using UnityEngine;

public class PointCollectible : CollisionCollectible
{
    [SerializeField] private int pointsAwared;

    /// <summary>
    /// TODO: Implement point scoring when collectibles are picked up.
    ///
    /// This class is currently a stub from the course and is not used by the
    /// live gameplay yet. Leaving the method unimplemented will throw at
    /// runtime if it ever gets called, which makes it obvious that wiring is
    /// incomplete instead of silently failing.
    ///
    /// When wiring scoring, inject or resolve an <see cref="IScoreManager"/>
    /// and call <c>IncreaseScore</c> for the owning <see cref="PlayerData"/>.
    /// </summary>
    protected override void CollectItem(CollisionCollectible collectible, CarManager carManager)
    {
        throw new System.NotImplementedException("PointCollectible is not wired yet. Implement scoring via IScoreManager before enabling this collectible.");
    }
}
