using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Object pool for car collision impact particle effects.
/// Pre-warms particle systems to eliminate first-hit lag spikes on mobile.
/// </summary>
public class VFXImpactPool : MonoBehaviour
{
    #region Fields
    [Header("Pool Settings")]
    [SerializeField] private ParticleSystem impactEffectPrefab;
    [SerializeField] private int poolSize = 10;
    [SerializeField] private bool preWarmOnStart = true;

    private Queue<ParticleSystem> pool = new Queue<ParticleSystem>();
    private static VFXImpactPool instance;
    #endregion

    #region Properties
    public static VFXImpactPool Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<VFXImpactPool>();
            }
            return instance;
        }
    }
    #endregion

    #region Unity Lifecycle
    void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            Destroy(gameObject);
            return;
        }

        InitializePool();
    }

    void Start()
    {
        if (preWarmOnStart)
        {
            PreWarmPool();
        }
    }

    void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }
    #endregion

    #region Public Methods
    /// <summary>
    /// Gets an impact effect from the pool. Creates a new one if pool is empty.
    /// </summary>
    public ParticleSystem GetImpactEffect()
    {
        ParticleSystem ps = pool.Count > 0 ? pool.Dequeue() : CreateNewInstance();
        ps.gameObject.SetActive(true);
        return ps;
    }

    /// <summary>
    /// Returns an impact effect to the pool after it finishes playing.
    /// </summary>
    public void ReturnToPool(ParticleSystem ps)
    {
        if (ps == null) 
            return;

        ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        ps.Clear();
        ps.gameObject.SetActive(false);
        pool.Enqueue(ps);
    }

    /// <summary>
    /// Returns an impact effect to the pool after a specified delay.
    /// </summary>
    public void ReturnToPoolAfterDelay(ParticleSystem ps, float delay)
    {
        if (ps == null) 
            return;

        StartCoroutine(ReturnToPoolCoroutine(ps, delay));
    }
    #endregion

    #region Private Methods
    private void InitializePool()
    {
        if (impactEffectPrefab == null)
        {
            Debug.LogError("[VFXImpactPool] Impact effect prefab is not assigned!");
            enabled = false;
            return;
        }

        // Create initial pool instances
        for (int i = 0; i < poolSize; i++)
        {
            CreateNewInstance();
        }
    }

    private ParticleSystem CreateNewInstance()
    {
        ParticleSystem ps = Instantiate(impactEffectPrefab, transform);
        ps.gameObject.SetActive(false);
        pool.Enqueue(ps);

        return ps;
    }

    private void PreWarmPool()
    {
        if (pool.Count == 0)
        {
            Debug.LogWarning("[VFXImpactPool] Pool is empty, cannot pre-warm.");
            return;
        }

        ParticleSystem warmup = pool.Dequeue();
        warmup.gameObject.SetActive(true);
        warmup.Play();
        warmup.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        warmup.Clear();
        warmup.gameObject.SetActive(false);
        pool.Enqueue(warmup);

        Debug.Log($"[VFXImpactPool] Pre-warmed impact effect pool. Pool size: {pool.Count}");
    }

    private IEnumerator ReturnToPoolCoroutine(ParticleSystem ps, float delay)
    {
        yield return new WaitForSeconds(delay);
        ReturnToPool(ps);
    }
    #endregion
}
