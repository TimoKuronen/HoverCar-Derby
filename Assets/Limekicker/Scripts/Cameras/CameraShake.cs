using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Accumulates camera shake requests and evaluates their combined offset. Requests expire after their
/// duration and fall off with distance from an optional source position.
/// </summary>
public class CameraShake : MonoBehaviour
{
    public static CameraShake Instance { get; private set; }

    private readonly List<ActiveShake> shakes = new();

    private void Awake()
    {
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void Update()
    {
        for (int i = shakes.Count - 1; i >= 0; i--)
        {
            ActiveShake shake = shakes[i];
            shake.Elapsed += Time.deltaTime;

            if (shake.Elapsed > shake.Data.Duration)
                shakes.RemoveAt(i);
        }
    }

    public void RequestShake(CameraShakeData data)
    {
        shakes.Add(new ActiveShake { Data = data });
    }

    public Vector3 GetShakeOffset(Vector3 cameraPosition)
    {
        float totalIntensity = 0f;

        foreach (ActiveShake shake in shakes)
        {
            float falloff = shake.Data.HasSource
                ? 1f / (1f + Vector3.Distance(cameraPosition, shake.Data.SourcePosition))
                : 1f;

            totalIntensity += shake.Data.Intensity * falloff;
        }

        return Random.insideUnitSphere * totalIntensity;
    }

    private class ActiveShake
    {
        public CameraShakeData Data;
        public float Elapsed;
    }
}
