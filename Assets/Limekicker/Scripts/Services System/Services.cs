using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public abstract class Services : MonoBehaviour
{
    private static Services instance;

    protected Dictionary<Type, IService> serviceMap = new Dictionary<Type, IService>();
    private List<IUpdateableService> updateableServices = new List<IUpdateableService>();

    private void Awake()
    {
        instance = this;

        Initialize();

        var monobehaviorServices = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None).OfType<IService>();

        foreach (var service in monobehaviorServices)
        {
            if (serviceMap.ContainsKey(service.GetType()))
            {
                continue;
            }

            AddService(service);
        }

        //Debug.Log($"Services initialized by {this.GetType().Name}!");
    }

    /// <summary>
    /// Initialize is called before all other awake methods in the game.
    /// This is where you should set up all services.
    /// </summary>
    protected abstract void Initialize();

    /// <summary>
    /// Registers a service, making it available through the Get method.
    /// </summary>
    public void AddService<T>(T service) where T : IService
    {
        serviceMap.Add(typeof(T), service);
        //Debug.Log($"Added service: {service.GetType().Name}");

        if (service is IUpdateableService updateableService)
        {
            updateableServices.Add(updateableService);
        }
    }

    public static T Get<T>() where T : IService
    {
        Type type = typeof(T);

        if (instance.serviceMap.TryGetValue(type, out IService service))
        {
            return (T)service;
        }

        Debug.LogError($"Service not found: {type.Name}");

        return default;
    }

    private void Update()
    {
        for (int i = 0; i < updateableServices.Count; i++)
        {
            updateableServices[i].Update();
        }
    }

    private void OnDisable()
    {
        // Dispose services in serviceMap
        foreach (var service in serviceMap)
        {
            if (service.Value is IDisposable disposableService)
            {
                disposableService.Dispose();
            }
        }

        // Dispose services in updateableServices
        foreach (var updateableService in updateableServices)
        {
            if (updateableService is IDisposable disposableUpdateableService)
            {
                disposableUpdateableService.Dispose();
            }
        }
    }
}
