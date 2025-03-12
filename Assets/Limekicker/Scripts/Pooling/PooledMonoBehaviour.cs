using System;
using System.Collections;
using UnityEngine;

public class PooledMonoBehaviour : MonoBehaviour
{
    [SerializeField] int initialPoolSize = 50;

    public int InitialPoolSize { get { return initialPoolSize; } }

    public event Action<PooledMonoBehaviour> OnReturnToPool;

    public T Get<T>(bool enable = true) where T : PooledMonoBehaviour
    {
        var pool = Pool.GetPool(this);
        var pooledObject = pool.Get<T>();

        if (enable)
        {
            pooledObject.gameObject.SetActive(true);
        }

        return pooledObject;
    }

    public T Get<T>(Vector3 position, Quaternion rotation) where T : PooledMonoBehaviour
    {
        var pooledObject = Get<T>();

        pooledObject.transform.SetPositionAndRotation(position, rotation);

        return pooledObject;
    }

    public void ReturnToPool(float delay = 0)
    {
        if (!gameObject.activeInHierarchy)
            return;

        StartCoroutine(ReturnToPoolAfterSeconds(delay));
    }

    private IEnumerator ReturnToPoolAfterSeconds(float delay)
    {
        yield return new WaitForSeconds(delay);
        gameObject.SetActive(false);
    }

    protected virtual void OnDisable()
    {
        OnReturnToPool?.Invoke(this);
    }
}