using System;
using System.Collections.Generic;

public class DIContainer
{
    private Dictionary<Type, object> instances = new();

    public void Register<T>(T instance)
    {
        instances[typeof(T)] = instance;
    }

    public T Resolve<T>()
    {
        return (T)instances[typeof(T)];
    }

    public IEnumerable<T> ResolveAll<T>()
    {
        foreach (var instance in instances.Values)
        {
            if (instance is T tInstance)
                yield return tInstance;
        }
    }
}