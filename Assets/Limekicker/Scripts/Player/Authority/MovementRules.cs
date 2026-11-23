using UnityEngine;

/// <summary>
/// Pure server-side check of a movement sample. Vertical speed is not limited because falling and
/// collision launches are legitimate; the play volume bounds the vertical range instead.
/// </summary>
public static class MovementRules
{
    public static MovementVerdict Evaluate(Vector3 previous, Vector3 current, float deltaTime, MovementLimits limits)
    {
        if (limits.PlayVolume.HasValue && !limits.PlayVolume.Value.Contains(current))
            return MovementVerdict.OutOfBounds;

        if (deltaTime <= 0f)
            return MovementVerdict.Accepted;

        Vector3 horizontalDelta = current - previous;
        horizontalDelta.y = 0f;

        return horizontalDelta.magnitude / deltaTime > limits.MaxHorizontalSpeed
            ? MovementVerdict.TooFast
            : MovementVerdict.Accepted;
    }
}
