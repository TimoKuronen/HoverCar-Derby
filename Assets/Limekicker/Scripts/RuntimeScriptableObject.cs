using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class RuntimeScriptableObject : ScriptableObject
{
    static readonly List<RuntimeScriptableObject> instances = new List<RuntimeScriptableObject>();

    private void OnEnable() => instances.Add(this);
    private void OnDisable() => instances.Remove(this);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void ResetAllInstances()
    {
        foreach (var instance in instances)
        {
            instance.OnReset();
        }
    }

    protected abstract void OnReset();
}
