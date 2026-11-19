using NUnit.Framework;

public class ScoreLedgerTests
{
    [Test]
    public void TryAddPlayer_RejectsDuplicates()
    {
        var ledger = new ScoreLedger();

        Assert.IsTrue(ledger.TryAddPlayer(1));
        Assert.IsFalse(ledger.TryAddPlayer(1));
    }

    [Test]
    public void TryAddPlayer_DoesNotResetExistingPoints()
    {
        var ledger = new ScoreLedger();
        ledger.TryAddPlayer(1);
        ledger.TryAward(1, 25, out _);

        ledger.TryAddPlayer(1);

        ledger.TryGetPoints(1, out int points);
        Assert.AreEqual(25, points);
    }

    [Test]
    public void TryAward_AccumulatesAndReturnsTotal()
    {
        var ledger = new ScoreLedger();
        ledger.TryAddPlayer(1);

        ledger.TryAward(1, 10, out _);
        bool awarded = ledger.TryAward(1, 15, out int total);

        Assert.IsTrue(awarded);
        Assert.AreEqual(25, total);
    }

    [Test]
    public void TryAward_UnknownId_IsRejected()
    {
        var ledger = new ScoreLedger();

        Assert.IsFalse(ledger.TryAward(9, 10, out _));
        Assert.IsFalse(ledger.TryGetPoints(9, out _));
    }

    [Test]
    public void GetRanked_OrdersHighestFirst()
    {
        var ledger = new ScoreLedger();
        ledger.TryAddPlayer(1);
        ledger.TryAddPlayer(2);
        ledger.TryAddPlayer(3);
        ledger.TryAward(1, 10, out _);
        ledger.TryAward(2, 30, out _);
        ledger.TryAward(3, 20, out _);

        var ranked = ledger.GetRanked();

        Assert.AreEqual(2UL, ranked[0].Key);
        Assert.AreEqual(3UL, ranked[1].Key);
        Assert.AreEqual(1UL, ranked[2].Key);
    }

    [Test]
    public void TryGetDamagePoints_RoundsDamage()
    {
        Assert.IsTrue(ScoreLedger.TryGetDamagePoints(4.6f, 1, out int up));
        Assert.IsTrue(ScoreLedger.TryGetDamagePoints(4.4f, 1, out int down));

        Assert.AreEqual(5, up);
        Assert.AreEqual(4, down);
    }

    [TestCase(0f)]
    [TestCase(-3f)]
    public void TryGetDamagePoints_NonPositiveDamage_IsNotScorable(float damage)
    {
        Assert.IsFalse(ScoreLedger.TryGetDamagePoints(damage, 1, out int points));
        Assert.AreEqual(0, points);
    }

    [Test]
    public void TryGetDamagePoints_WithoutAttacker_IsNotScorable()
    {
        Assert.IsFalse(ScoreLedger.TryGetDamagePoints(30f, ulong.MaxValue, out int points));
        Assert.AreEqual(0, points);
    }
}
