using NUnit.Framework;
using UnityEngine;

public class CollisionDamageCalculatorTests
{
    private const float Impact = 40f;
    private const float Tolerance = 0.001f;

    private static readonly DamageSettings Settings = new DamageSettings
    {
        DamageMultiplier = 1f,
        MinDamageThreshold = 5f,
        IdleSpeedThreshold = 2f,
        DecentSpeedThreshold = 8f
    };

    private static readonly Vector3 Forward = Vector3.forward;
    private static readonly Vector3 Right = Vector3.right;

    // This car faces +Z and the other car sits ahead of it, facing back at it.
    private static CollisionInput HeadOn(float thisSpeed, float otherSpeed, float impact = Impact)
    {
        return new CollisionInput
        {
            ThisForward = Forward,
            ThisRight = Right,
            OtherForward = -Forward,
            OtherRight = -Right,
            HitNormal = -Forward,
            ThisSpeed = thisSpeed,
            OtherSpeed = otherSpeed,
            ImpactForce = impact
        };
    }

    // This car's front bumper meets the other car's flank.
    private static CollisionInput ThisRamsOtherSide(float thisSpeed, float otherSpeed)
    {
        return new CollisionInput
        {
            ThisForward = Forward,
            ThisRight = Right,
            OtherForward = Right,
            OtherRight = -Forward,
            HitNormal = -Forward,
            ThisSpeed = thisSpeed,
            OtherSpeed = otherSpeed,
            ImpactForce = Impact
        };
    }

    // Both cars face +Z and touch along their flanks.
    private static CollisionInput FlankContact(float thisSpeed, float otherSpeed, float impact = Impact)
    {
        return new CollisionInput
        {
            ThisForward = Forward,
            ThisRight = Right,
            OtherForward = Forward,
            OtherRight = Right,
            HitNormal = -Right,
            ThisSpeed = thisSpeed,
            OtherSpeed = otherSpeed,
            ImpactForce = impact
        };
    }

    // The same contact described from the other car's point of view.
    private static CollisionInput Mirror(CollisionInput input)
    {
        return new CollisionInput
        {
            ThisForward = input.OtherForward,
            ThisRight = input.OtherRight,
            OtherForward = input.ThisForward,
            OtherRight = input.ThisRight,
            HitNormal = -input.HitNormal,
            ThisSpeed = input.OtherSpeed,
            OtherSpeed = input.ThisSpeed,
            ImpactForce = input.ImpactForce
        };
    }

    [Test]
    public void HeadOn_SplitsDamageByOpposingSpeed()
    {
        CollisionDamage damage = CollisionDamageCalculator.Calculate(HeadOn(9f, 3f), Settings);

        Assert.AreEqual(5f, damage.ToThis, Tolerance);
        Assert.AreEqual(15f, damage.ToOther, Tolerance);
    }

    [Test]
    public void HeadOn_BothStationary_SplitsEvenly()
    {
        CollisionDamage damage = CollisionDamageCalculator.Calculate(HeadOn(0f, 0f), Settings);

        Assert.AreEqual(10f, damage.ToThis, Tolerance);
        Assert.AreEqual(10f, damage.ToOther, Tolerance);
    }

    [Test]
    public void FrontBumperAgainstFlank_DamagesOnlyTheVictim()
    {
        CollisionDamage damage = CollisionDamageCalculator.Calculate(ThisRamsOtherSide(10f, 10f), Settings);

        Assert.AreEqual(0f, damage.ToThis, Tolerance);
        Assert.AreEqual(Impact, damage.ToOther, Tolerance);
    }

    [Test]
    public void FrontBumperAgainstIdleCar_ScalesWithAttackerSpeed()
    {
        CollisionDamage damage = CollisionDamageCalculator.Calculate(ThisRamsOtherSide(4f, 0f), Settings);

        Assert.AreEqual(0f, damage.ToThis, Tolerance);
        Assert.AreEqual(Impact * 0.5f, damage.ToOther, Tolerance);
    }

    [Test]
    public void IdleAttackerFrontBumper_DealsNoDamage()
    {
        CollisionDamage damage = CollisionDamageCalculator.Calculate(ThisRamsOtherSide(1f, 0f), Settings);

        Assert.AreEqual(0f, damage.ToThis, Tolerance);
        Assert.AreEqual(0f, damage.ToOther, Tolerance);
    }

    [Test]
    public void VictimOfFrontBumper_TakesTheDamageNotTheAttacker()
    {
        CollisionDamage damage = CollisionDamageCalculator.Calculate(Mirror(ThisRamsOtherSide(10f, 10f)), Settings);

        Assert.AreEqual(Impact, damage.ToThis, Tolerance);
        Assert.AreEqual(0f, damage.ToOther, Tolerance);
    }

    [Test]
    public void FlankHit_WithMajorityVelocity_DamagesTheSlowerCar()
    {
        CollisionDamage damage = CollisionDamageCalculator.Calculate(FlankContact(10f, 0f), Settings);

        Assert.AreEqual(0f, damage.ToThis, Tolerance);
        Assert.AreEqual(Impact * 0.7f, damage.ToOther, Tolerance);
    }

    [Test]
    public void FlankHit_WithoutMajorityVelocity_SplitsEvenly()
    {
        CollisionDamage damage = CollisionDamageCalculator.Calculate(FlankContact(5f, 4f), Settings);

        Assert.AreEqual(Impact * 0.5f, damage.ToThis, Tolerance);
        Assert.AreEqual(Impact * 0.5f, damage.ToOther, Tolerance);
    }

    [Test]
    public void FlankHit_BothIdle_DealsReducedDamageToBoth()
    {
        CollisionDamage damage = CollisionDamageCalculator.Calculate(FlankContact(0f, 0f, 100f), Settings);

        Assert.AreEqual(10f, damage.ToThis, Tolerance);
        Assert.AreEqual(10f, damage.ToOther, Tolerance);
    }

    [Test]
    public void DamageToOther_AtOrBelowMinThreshold_IsZeroedWhileDamageToThisIsKept()
    {
        CollisionDamage damage = CollisionDamageCalculator.Calculate(HeadOn(0f, 0f, 10f), Settings);

        Assert.AreEqual(2.5f, damage.ToThis, Tolerance);
        Assert.AreEqual(0f, damage.ToOther, Tolerance);
    }

    [Test]
    public void DamageMultiplier_ScalesDamage()
    {
        DamageSettings doubled = Settings;
        doubled.DamageMultiplier = 2f;

        CollisionDamage damage = CollisionDamageCalculator.Calculate(ThisRamsOtherSide(10f, 10f), doubled);

        Assert.AreEqual(Impact * 2f, damage.ToOther, Tolerance);
    }

    [TestCase(7f, 5f)]
    [TestCase(10f, 0f)]
    [TestCase(10f, 10f)]
    [TestCase(5f, 4f)]
    public void ViewpointSwap_SwapsWhoTakesDamage(float thisSpeed, float otherSpeed)
    {
        CollisionInput[] contacts =
        {
            HeadOn(thisSpeed, otherSpeed),
            ThisRamsOtherSide(thisSpeed, otherSpeed),
            FlankContact(thisSpeed, otherSpeed)
        };

        foreach (CollisionInput contact in contacts)
        {
            CollisionDamage direct = CollisionDamageCalculator.Calculate(contact, Settings);
            CollisionDamage mirrored = CollisionDamageCalculator.Calculate(Mirror(contact), Settings);

            Assert.AreEqual(direct.ToThis, mirrored.ToOther, Tolerance);
            Assert.AreEqual(direct.ToOther, mirrored.ToThis, Tolerance);
        }
    }
}
