using System.Collections.Generic;
using UnityEngine;

public class Pool : MonoBehaviour
{
    private static Dictionary<PooledMonoBehaviour, Pool> pools = new Dictionary<PooledMonoBehaviour, Pool>();

    private Queue<PooledMonoBehaviour> objects = new Queue<PooledMonoBehaviour>();
    private PooledMonoBehaviour prefab;

    public static Pool GetPool(PooledMonoBehaviour prefab)
    {
        if (pools.ContainsKey(prefab))
            return pools[prefab];

        var poolGameObject = new GameObject("Pool-" + prefab.name);
        var pool = poolGameObject.AddComponent<Pool>();

        pool.prefab = prefab;

        pools.Add(prefab, pool);
        return pool;
    }

    public T Get<T>() where T : PooledMonoBehaviour
    {
        if (objects.Count == 0)
        {
            GrowPool();
        }

        var pooledObject = objects.Dequeue();
        return pooledObject as T;
    }

    private void GrowPool()
    {
        for (int i = 0; i < prefab.InitialPoolSize; i++)
        {
            var pooledObject = Instantiate(prefab) as PooledMonoBehaviour;
            pooledObject.gameObject.name += " " + i;

            pooledObject.OnReturnToPool += AddObjectToAvailableQueue;

            pooledObject.transform.SetParent(this.transform);
            pooledObject.gameObject.SetActive(false);
        }
    }

    private void AddObjectToAvailableQueue(PooledMonoBehaviour pooledObject)
    {
        if (pooledObject == null || pooledObject.gameObject == null)
        {
            Debug.LogWarning("Attempted to add a destroyed pooled object to the queue.");
            return;
        }

        // Check if the pool instance is destroyed or inactive
        if (this == null || !gameObject.activeSelf)
        {
            return;
        }

        if (pooledObject.gameObject.activeInHierarchy)
        {
            pooledObject.transform.SetParent(this.transform);
        }
        objects.Enqueue(pooledObject);
    }
}
