using System.Collections.Generic;

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
 