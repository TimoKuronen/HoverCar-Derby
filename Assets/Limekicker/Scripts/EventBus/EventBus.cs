using System.Collections.Generic;

/// <summary>
/// Generic event bus for type-safe event handling. Supports both parameterized and parameterless callbacks.
/// </summary>
public static class EventBus<T> where T : IEvent
{
    static readonly HashSet<IEventBinding<T>> bindings = new();

    public static void Register(EventBinding<T> binding)
    {
        bindings.Add(binding);
    }
    public static void Unregister(EventBinding<T> binding)
    {
        bindings.Remove(binding);
    }

    /// <summary>
    /// Raises an event, invoking all registered callbacks. Handles null bindings gracefully.
    /// </summary>
    public static void Raise(T @event)
    {
        foreach (var binding in bindings)
        {
            if (binding == null)
                continue;

            binding.OnEvent?.Invoke(@event);
            binding.OnEventNoArgs?.Invoke();
        }
    }
}
 