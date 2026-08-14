using System;
using UnityEngine;

/// <summary>
/// Scriptable integer value that notifies listeners when it changes.
/// </summary>
[CreateAssetMenu(menuName = "Variables/int Variable")]
public class IntVariable : RuntimeScriptableObject
{
    [SerializeField] private int initialValue;
    [SerializeField] private int value;

    public event Action<int> OnValueChanged = delegate { };

    public int Value
    {
        get => value;
        set
        {
            if (this.value != value)
            {
                this.value = value;
                OnValueChanged.Invoke(this.value);
            }
        }
    }

    protected override void OnReset()
    {
        value = initialValue;
        OnValueChanged.Invoke(value);
    }
}
