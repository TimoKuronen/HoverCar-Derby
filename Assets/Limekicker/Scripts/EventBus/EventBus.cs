using System.Collections.Generic;

/// <summary>
/// Generic event bus for type-safe event handling. Supports both parameterized and parameterless callbacks.
/// </summary>
public static class EventBus<T> where T : IEvent
{
    static readonly HashSet<IEventBinding<T>> bindings = new();
    static IEventBinding<T>[] snapshot = System.Array.Empty<IEventBinding<T>>();
    static bool snapshotDirty;

    public static void Register(EventBinding<T> binding)
    {
        if (bindings.Add(binding))
            snapshotDirty = true;
    }
    public static void Unregister(EventBinding<T> binding)
    {
        if (bindings.Remove(binding))
            snapshotDirty = true;
    }

    /// <summary>
    /// Raises an event, invoking all registered callbacks. Iterates a snapshot so handlers may register or
    /// unregister during a raise; bindings removed mid-raise are skipped.
    /// </summary>
    public static void Raise(T @event)
    {
        if (snapshotDirty)
        {
            var rebuilt = new IEventBinding<T>[bindings.Count];
            bindings.CopyTo(rebuilt);
            snapshot = rebuilt;
            snapshotDirty = false;
        }

        foreach (var binding in snapshot)
        {
            if (binding == null || !bindings.Contains(binding))
                continue;

            binding.OnEvent?.Invoke(@event);
            binding.OnEventNoArgs?.Invoke();
        }
    }
}
