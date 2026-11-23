using UnityEngine;

/// <summary>
/// Limits an owner-driven car must stay within: a horizontal speed ceiling and an optional play volume.
/// </summary>
public readonly struct MovementLimits
{
    public float MaxHorizontalSpeed { get; }
    public Bounds? PlayVolume { get; }

    public MovementLimits(float maxHorizontalSpeed, Bounds? playVolume)
    {
        MaxHorizontalSpeed = maxHorizontalSpeed;
        PlayVolume = playVolume;
    }
}
