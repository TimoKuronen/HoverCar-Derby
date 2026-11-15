using UnityEngine;

/// <summary>
/// Tuning values that shape the collision damage decision.
/// </summary>
public struct DamageSettings
{
    public float DamageMultiplier;
    public float MinDamageThreshold;
    public float IdleSpeedThreshold;
    public float DecentSpeedThreshold;
}

/// <summary>
/// Geometry and speeds of one car-to-car contact, from the point of view of "this" car.
/// Speeds are sampled before the physics step. HitNormal is the contact normal as seen by "this" car.
/// </summary>
public struct CollisionInput
{
    public Vector3 ThisForward;
    public Vector3 ThisRight;
    public Vector3 OtherForward;
    public Vector3 OtherRight;
    public Vector3 HitNormal;
    public float ThisSpeed;
    public float OtherSpeed;
    public float ImpactForce;
}

/// <summary>
/// Damage each car takes from one collision. ToOther is zero unless it exceeds the minimum
/// damage threshold; ToThis is applied whenever it is above zero.
/// </summary>
public readonly struct CollisionDamage
{
    public readonly float ToThis;
    public readonly float ToOther;

    public CollisionDamage(float toThis, float toOther)
    {
        ToThis = toThis;
        ToOther = toOther;
    }
}

/// <summary>
/// Pure damage rules: front bumper hits deal full damage, head-on collisions split damage by speed,
/// and side hits require majority velocity to deal damage.
/// </summary>
public static class CollisionDamageCalculator
{
    private const float FrontBumperDot = 0.7f;
    private const float SideDot = 0.5f;
    private const float MajorityVelocityRatio = 1.5f;
    private const float IdleHitFraction = 0.1f;
    private const float SideHitFraction = 0.7f;

    public static CollisionDamage Calculate(CollisionInput input, DamageSettings settings)
    {
        Vector3 hitNormal = input.HitNormal;
        bool thisIsFrontBumper = Vector3.Dot(input.ThisForward, -hitNormal) > FrontBumperDot;
        bool otherIsFrontBumper = Vector3.Dot(input.OtherForward, hitNormal) > FrontBumperDot;
        bool thisIsSide = !thisIsFrontBumper && Mathf.Abs(Vector3.Dot(input.ThisRight, -hitNormal)) > SideDot;
        bool otherIsSide = !otherIsFrontBumper && Mathf.Abs(Vector3.Dot(input.OtherRight, hitNormal)) > SideDot;

        bool isHeadOn = thisIsFrontBumper && otherIsFrontBumper;
        float baseDamage = input.ImpactForce * settings.DamageMultiplier;

        float thisSpeed = input.ThisSpeed;
        float otherSpeed = input.OtherSpeed;
        float idle = settings.IdleSpeedThreshold;
        float decent = settings.DecentSpeedThreshold;

        bool thisIsIdle = thisSpeed < idle;
        bool otherIsIdle = otherSpeed < idle;
        bool thisHasDecentSpeed = thisSpeed >= decent;
        bool otherHasDecentSpeed = otherSpeed >= decent;

        float damageToThis = 0f;
        float damageToOther = 0f;

        if (isHeadOn)
        {
            float totalDamage = baseDamage * 0.5f;
            float totalSpeed = thisSpeed + otherSpeed;

            if (totalSpeed < 0.01f)
            {
                damageToThis = totalDamage * 0.5f;
                damageToOther = totalDamage * 0.5f;
            }
            else
            {
                damageToThis = totalDamage * (otherSpeed / totalSpeed);
                damageToOther = totalDamage * (thisSpeed / totalSpeed);
            }
        }
        else if (thisIsFrontBumper)
        {
            if (thisIsIdle)
            {
                damageToOther = baseDamage * IdleHitFraction;
            }
            else if (otherIsIdle || !otherHasDecentSpeed)
            {
                damageToOther = baseDamage * Mathf.Clamp01(thisSpeed / decent);
            }
            else
            {
                damageToOther = baseDamage;
            }
        }
        else if (otherIsFrontBumper)
        {
            if (otherIsIdle)
            {
                damageToThis = baseDamage * IdleHitFraction;
            }
            else if (thisIsIdle || !thisHasDecentSpeed)
            {
                damageToThis = baseDamage * Mathf.Clamp01(otherSpeed / decent);
            }
            else
            {
                damageToThis = baseDamage;
            }
        }
        else
        {
            bool thisHasMajorityVelocity = thisSpeed > otherSpeed * MajorityVelocityRatio;
            bool otherHasMajorityVelocity = otherSpeed > thisSpeed * MajorityVelocityRatio;

            if (thisHasMajorityVelocity && thisIsSide && (otherIsIdle || !otherHasDecentSpeed))
            {
                damageToOther = baseDamage * Mathf.Clamp01((thisSpeed - otherSpeed) / decent) * SideHitFraction;
            }
            else if (otherHasMajorityVelocity && otherIsSide && (thisIsIdle || !thisHasDecentSpeed))
            {
                damageToThis = baseDamage * Mathf.Clamp01((otherSpeed - thisSpeed) / decent) * SideHitFraction;
            }
            else if (thisIsIdle && otherIsIdle)
            {
                damageToThis = baseDamage * IdleHitFraction;
                damageToOther = baseDamage * IdleHitFraction;
            }
            else
            {
                damageToThis = baseDamage * 0.5f;
                damageToOther = baseDamage * 0.5f;
            }
        }

        if (damageToOther <= settings.MinDamageThreshold)
            damageToOther = 0f;

        return new CollisionDamage(damageToThis, damageToOther);
    }
}
