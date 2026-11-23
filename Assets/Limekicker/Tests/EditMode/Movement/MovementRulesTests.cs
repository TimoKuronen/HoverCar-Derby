using NUnit.Framework;
using UnityEngine;

public class MovementRulesTests
{
    private static readonly Bounds Arena = new Bounds(Vector3.zero, new Vector3(100f, 20f, 80f));
    private static readonly MovementLimits Limits = new MovementLimits(60f, Arena);

    [Test]
    public void Evaluate_SpeedUnderLimit_IsAccepted()
    {
        MovementVerdict verdict = MovementRules.Evaluate(Vector3.zero, new Vector3(10f, 0f, 0f), 0.25f, Limits);

        Assert.AreEqual(MovementVerdict.Accepted, verdict);
    }

    [Test]
    public void Evaluate_SpeedAtLimit_IsAccepted()
    {
        MovementVerdict verdict = MovementRules.Evaluate(Vector3.zero, new Vector3(15f, 0f, 0f), 0.25f, Limits);

        Assert.AreEqual(MovementVerdict.Accepted, verdict);
    }

    [Test]
    public void Evaluate_SpeedOverLimit_IsTooFast()
    {
        MovementVerdict verdict = MovementRules.Evaluate(Vector3.zero, new Vector3(30f, 0f, 0f), 0.25f, Limits);

        Assert.AreEqual(MovementVerdict.TooFast, verdict);
    }

    [Test]
    public void Evaluate_TeleportInsideArena_IsTooFast()
    {
        MovementVerdict verdict = MovementRules.Evaluate(new Vector3(-40f, 0f, 0f), new Vector3(40f, 0f, 0f), 0.25f, Limits);

        Assert.AreEqual(MovementVerdict.TooFast, verdict);
    }

    [Test]
    public void Evaluate_VerticalMovementIsNotSpeedLimited()
    {
        MovementVerdict verdict = MovementRules.Evaluate(Vector3.zero, new Vector3(0f, 9f, 0f), 0.1f, Limits);

        Assert.AreEqual(MovementVerdict.Accepted, verdict);
    }

    [Test]
    public void Evaluate_OutsidePlayVolume_IsOutOfBounds()
    {
        MovementVerdict verdict = MovementRules.Evaluate(new Vector3(49f, 0f, 0f), new Vector3(51f, 0f, 0f), 1f, Limits);

        Assert.AreEqual(MovementVerdict.OutOfBounds, verdict);
    }

    [Test]
    public void Evaluate_BelowPlayVolume_IsOutOfBounds()
    {
        MovementVerdict verdict = MovementRules.Evaluate(Vector3.zero, new Vector3(0f, -11f, 0f), 1f, Limits);

        Assert.AreEqual(MovementVerdict.OutOfBounds, verdict);
    }

    [Test]
    public void Evaluate_OutOfBoundsWinsOverSpeed()
    {
        MovementVerdict verdict = MovementRules.Evaluate(Vector3.zero, new Vector3(500f, 0f, 0f), 0.25f, Limits);

        Assert.AreEqual(MovementVerdict.OutOfBounds, verdict);
    }

    [Test]
    public void Evaluate_WithoutPlayVolume_OnlySpeedIsChecked()
    {
        var unbounded = new MovementLimits(60f, null);

        Assert.AreEqual(MovementVerdict.Accepted, MovementRules.Evaluate(Vector3.zero, new Vector3(0f, 0f, 9999f), 1000f, unbounded));
        Assert.AreEqual(MovementVerdict.TooFast, MovementRules.Evaluate(Vector3.zero, new Vector3(0f, 0f, 9999f), 1f, unbounded));
    }

    [Test]
    public void Evaluate_NonPositiveDeltaTime_IsAccepted()
    {
        MovementVerdict verdict = MovementRules.Evaluate(Vector3.zero, new Vector3(30f, 0f, 0f), 0f, Limits);

        Assert.AreEqual(MovementVerdict.Accepted, verdict);
    }
}
