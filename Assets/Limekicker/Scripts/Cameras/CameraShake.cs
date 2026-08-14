using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Singleton that accumulates and evaluates camera shake requests.
/// </summary>
public class CameraShake : MonoBehaviour
{
    public static CameraShake Instance { get; private set; }
    private readonly List<ShakeRequest> shakes = new();

    private void Awake()
    {
        Instance = this;
    }
    public void RequestShake(CameraShakeData data)
    {
        shakes.Add(new ShakeRequest(data.Intensity, data.Duration, data.SourcePos));
    }

    public Vector3 GetShakeOffset(Vector3 cameraPos)
    {
        float totalIntensity = 0f;
        for (int i = shakes.Count - 1; i >= 0; i--)
        {
            var s = shakes[i];
            if (s.Elapsed > s.Duration) shakes.RemoveAt(i);
            else
            {
                float falloff = 1f;
                if (s.SourcePos.HasValue)
                    falloff = 1f / (1f + Vector3.Distance(cameraPos, s.SourcePos.Value));
                totalIntensity += s.Intensity * falloff;
                s.Elapsed += Time.deltaTime;
            }
        }

        return UnityEngine.Random.insideUnitSphere * totalIntensity;
    }
    class ShakeRequest
    {
        public float Intensity;
        public float Duration;
        public float Elapsed;
        public Vector3? SourcePos;

        public ShakeRequest(float i, float d, Vector3? pos)
        {
            Intensity = i;
            Duration = d;
            Elapsed = 0f;
            SourcePos = pos;
        }
    }
}

/// <summary>
/// Serializable intensity, duration, and optional source for camera shake.
/// </summary>
[Serializable]
public class CameraShakeData
{
    public float Intensity;
    public float Duration;
    public Vector3? SourcePos;
}