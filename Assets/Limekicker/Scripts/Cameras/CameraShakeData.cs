using System;
using UnityEngine;

/// <summary>
/// Intensity and duration of one camera shake, optionally anchored to a world position.
/// </summary>
[Serializable]
public struct CameraShakeData
{
    public float Intensity;
    public float Duration;
    public bool HasSource;
    public Vector3 SourcePosition;
}
