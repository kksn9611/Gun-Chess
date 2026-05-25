using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Centralized GameObject object pool. Keyed by prefab reference.
/// Pure C# singleton. Tracks usage per round — unused pools are cleared on Trim().
/// </summary>
public class VfxPoolManager
{
    private static VfxPoolManager instance;
    public static VfxPoolManager Instance => instance ??= new VfxPoolManager();

    private VfxPoolManager() { }

    private const int ExpandSize = 2;

    private readonly Dictionary<GameObject, Queue<GameObject>> pools = new();
    private readonly HashSet<GameObject> usedThisRound = new();
    private Transform container;

    // Container //

    private Transform GetContainer()
    {
        if (container != null) return container;

        GameObject containerObj = GameObject.Find("VfxPoolContainer");
        if (containerObj == null)
            containerObj = new GameObject("VfxPoolContainer");

        container = containerObj.transform;
        return container;
    }

    /// <summary>Pre-warm the pool for a given prefab.</summary>
    public void Prewarm(GameObject prefab, int count)
    {
        if (prefab == null) return;

        Queue<GameObject> pool = GetPool(prefab);
        for (int i = 0; i < count; i++)
        {
            GameObject go = Object.Instantiate(prefab, GetContainer());
            go.SetActive(false);
            pool.Enqueue(go);
        }
    }

    /// <summary>Get an instance from the pool. Auto-expands if empty.</summary>
    public GameObject Get(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (prefab == null) return null;

        usedThisRound.Add(prefab);

        Queue<GameObject> pool = GetPool(prefab);

        if (pool.Count == 0)
            Prewarm(prefab, ExpandSize);

        GameObject go = pool.Dequeue();
        if (go == null) return Get(prefab, position, rotation); // retry if destroyed externally
        go.transform.SetPositionAndRotation(position, rotation);
        go.SetActive(true);
        return go;
    }

    /// <summary>Return an instance to the pool.</summary>
    public void Return(GameObject prefab, GameObject instance)
    {
        if (instance == null) return;

        instance.SetActive(false);
        instance.transform.SetParent(GetContainer());
        Queue<GameObject> pool = GetPool(prefab);
        pool.Enqueue(instance);
    }

    /// <summary>
    /// Clear pools that were not used this round. Call at round reset.
    /// </summary>
    public void Trim()
    {
        var toRemove = new List<GameObject>();

        foreach (var kvp in pools)
        {
            if (usedThisRound.Contains(kvp.Key)) continue;

            Queue<GameObject> pool = kvp.Value;
            while (pool.Count > 0)
            {
                GameObject go = pool.Dequeue();
                if (go != null)
                    Object.Destroy(go);
            }
            toRemove.Add(kvp.Key);
        }

        foreach (var key in toRemove)
            pools.Remove(key);

        usedThisRound.Clear();
    }

    /// <summary>Clear all pools and destroy all pooled objects.</summary>
    public void ClearAll()
    {
        foreach (var pool in pools.Values)
        {
            while (pool.Count > 0)
            {
                GameObject go = pool.Dequeue();
                if (go != null)
                    Object.Destroy(go);
            }
        }
        pools.Clear();
        usedThisRound.Clear();
    }

    // Internal //

    private Queue<GameObject> GetPool(GameObject prefab)
    {
        if (!pools.TryGetValue(prefab, out Queue<GameObject> pool))
        {
            pool = new Queue<GameObject>();
            pools[prefab] = pool;
        }
        return pool;
    }
}
