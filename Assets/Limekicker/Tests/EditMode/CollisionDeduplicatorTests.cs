using NUnit.Framework;

public class CollisionDeduplicatorTests
{
    private const float Cooldown = 0.5f;

    [Test]
    public void KeyFor_IsOrderIndependent()
    {
        Assert.AreEqual(CollisionDeduplicator.KeyFor(3, 7), CollisionDeduplicator.KeyFor(7, 3));
    }

    [Test]
    public void KeyFor_DistinguishesPairs()
    {
        Assert.AreNotEqual(CollisionDeduplicator.KeyFor(1, 2), CollisionDeduplicator.KeyFor(1, 3));
        Assert.AreNotEqual(CollisionDeduplicator.KeyFor(1, 2), CollisionDeduplicator.KeyFor(2, 3));
    }

    [Test]
    public void IsRecent_BeforeRegister_IsFalse()
    {
        var dedup = new CollisionDeduplicator();

        Assert.IsFalse(dedup.IsRecent(CollisionDeduplicator.KeyFor(1, 2), 10f, Cooldown));
    }

    [Test]
    public void IsRecent_WithinCooldown_IsTrue()
    {
        var dedup = new CollisionDeduplicator();
        ulong key = CollisionDeduplicator.KeyFor(1, 2);

        dedup.Register(key, 10f, Cooldown);

        Assert.IsTrue(dedup.IsRecent(key, 10.4f, Cooldown));
    }

    [Test]
    public void IsRecent_AtOrAfterCooldown_IsFalse()
    {
        var dedup = new CollisionDeduplicator();
        ulong key = CollisionDeduplicator.KeyFor(1, 2);

        dedup.Register(key, 10f, Cooldown);

        Assert.IsFalse(dedup.IsRecent(key, 10.5f, Cooldown));
        Assert.IsFalse(dedup.IsRecent(key, 12f, Cooldown));
    }

    [Test]
    public void IsRecent_WhenClockRestarted_IsFalse()
    {
        var dedup = new CollisionDeduplicator();
        ulong key = CollisionDeduplicator.KeyFor(1, 2);

        dedup.Register(key, 100f, Cooldown);

        Assert.IsFalse(dedup.IsRecent(key, 0.1f, Cooldown));
    }

    [Test]
    public void IsRecent_IsPerPair()
    {
        var dedup = new CollisionDeduplicator();

        dedup.Register(CollisionDeduplicator.KeyFor(1, 2), 10f, Cooldown);

        Assert.IsFalse(dedup.IsRecent(CollisionDeduplicator.KeyFor(1, 3), 10.1f, Cooldown));
    }

    [Test]
    public void Register_AfterCooldown_RestartsTheWindow()
    {
        var dedup = new CollisionDeduplicator();
        ulong key = CollisionDeduplicator.KeyFor(1, 2);

        dedup.Register(key, 10f, Cooldown);
        dedup.Register(key, 11f, Cooldown);

        Assert.IsTrue(dedup.IsRecent(key, 11.2f, Cooldown));
        Assert.IsFalse(dedup.IsRecent(key, 11.6f, Cooldown));
    }

    [Test]
    public void OneCollisionObservedByBothCarsAndTheOwnerReport_ScoresOnce()
    {
        var dedup = new CollisionDeduplicator();
        var ledger = new ScoreLedger();
        ledger.TryAddPlayer(1);
        ledger.TryAddPlayer(2);
        ulong key = CollisionDeduplicator.KeyFor(1, 2);

        // Car 1, car 2 and the owner's report all reach the server for the same physical hit.
        for (int observation = 0; observation < 3; observation++)
        {
            float now = 10f + observation * 0.01f;
            if (dedup.IsRecent(key, now, Cooldown))
                continue;

            dedup.Register(key, now, Cooldown);
            if (ScoreLedger.TryGetDamagePoints(30f, 1, out int points))
                ledger.TryAward(1, points, out _);
        }

        ledger.TryGetPoints(1, out int total);
        Assert.AreEqual(30, total);
    }

    [Test]
    public void SeparateHitsOutsideTheCooldown_ScoreEachTime()
    {
        var dedup = new CollisionDeduplicator();
        var ledger = new ScoreLedger();
        ledger.TryAddPlayer(1);
        ulong key = CollisionDeduplicator.KeyFor(1, 2);

        foreach (float now in new[] { 10f, 11f })
        {
            if (dedup.IsRecent(key, now, Cooldown))
                continue;

            dedup.Register(key, now, Cooldown);
            if (ScoreLedger.TryGetDamagePoints(30f, 1, out int points))
                ledger.TryAward(1, points, out _);
        }

        ledger.TryGetPoints(1, out int total);
        Assert.AreEqual(60, total);
    }
}
