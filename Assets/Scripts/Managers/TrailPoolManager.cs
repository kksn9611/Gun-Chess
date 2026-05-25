using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Centralized trail object pool. Keyed by prefab reference.
/// Pure C# singleton. Supports maxSize per prefab — Trim() removes excess.
/// </summary>
public class TrailPoolManager
{
    private static TrailPoolManager instance;
    public static TrailPoolManager Instance => instance ??= new TrailPoolManager();

    private TrailPoolManager() { }

    private const int ExpandSize = 2;

    private readonly Dictionary<TrailRenderer, Queue<TrailRenderer>> pools = new();
    private readonly Dictionary<TrailRenderer, int> maxSizes = new();
    private Transform container;

    // Container //

    private Transform GetContainer()
    {
        if (container != null) return container;

        GameObject containerObj = GameObject.Find("PoolContainer");
        if (containerObj == null)
            containerObj = new GameObject("PoolContainer");

        container = containerObj.transform;
        return container;
    }

    /// <summary>
    /// Pre-warm the pool for a given prefab and set its max size.
    /// </summary>
    public void Prewarm(TrailRenderer prefab, int count)
    {
        if (prefab == null) return;

        Queue<TrailRenderer> pool = GetPool(prefab);

        // maxSize = 2x single prewarm count (never accumulates)
        if (!maxSizes.ContainsKey(prefab))
            maxSizes[prefab] = count * 2;

        for (int i = 0; i < count; i++)
        {
            TrailRenderer trail = Object.Instantiate(prefab, GetContainer());
            trail.gameObject.SetActive(false);
            pool.Enqueue(trail);
        }
    }

    /// <summary>
    /// Get a trail from the pool. Auto-expands if empty.
    /// </summary>
    public TrailRenderer Get(TrailRenderer prefab, Vector3 spawnPosition)
    {
        if (prefab == null) return null;

        Queue<TrailRenderer> pool = GetPool(prefab);

        if (pool.Count == 0)
            Prewarm(prefab, ExpandSize);

        TrailRenderer trail = pool.Dequeue();
        if (trail == null) return Get(prefab, spawnPosition); // retry if destroyed externally
        trail.transform.position = spawnPosition;
        trail.gameObject.SetActive(true);
        trail.Clear();
        return trail;
    }

    /// <summary>
    /// Return a trail to the pool. Prefab key required for correct pool routing.
    /// </summary>
    public void Return(TrailRenderer prefab, TrailRenderer trail)
    {
        if (trail == null) return;

        trail.gameObject.SetActive(false);
        Queue<TrailRenderer> pool = GetPool(prefab);
        pool.Enqueue(trail);
    }

    /// <summary>
    /// Trim all pools back to their maxSize. Call on round reset.
    /// </summary>
    public void Trim()
    {
        foreach (var kvp in pools)
        {
            int max = maxSizes.TryGetValue(kvp.Key, out int m) ? m : 0;
            Queue<TrailRenderer> pool = kvp.Value;

            while (pool.Count > max)
            {
                TrailRenderer trail = pool.Dequeue();
                if (trail != null)
                    Object.Destroy(trail.gameObject);
            }
        }
    }

    /// <summary>
    /// Clear all pools and destroy all pooled objects. Call on game reset.
    /// </summary>
    public void ClearAll()
    {
        foreach (var pool in pools.Values)
        {
            while (pool.Count > 0)
            {
                TrailRenderer trail = pool.Dequeue();
                if (trail != null)
                    Object.Destroy(trail.gameObject);
            }
        }
        pools.Clear();
        maxSizes.Clear();
    }

    // Internal //

    private Queue<TrailRenderer> GetPool(TrailRenderer prefab)
    {
        if (!pools.TryGetValue(prefab, out Queue<TrailRenderer> pool))
        {
            pool = new Queue<TrailRenderer>();
            pools[prefab] = pool;
        }
        return pool;
    }
}
