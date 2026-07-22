using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// Plays the merge visual: projectiles fly from each source to the target tile, and after
/// reachTime an impact effect plays while the caller (MergeManager) spawns the upgraded unit.
/// Pooling and timing live here so MergeManager stays pure logic.
/// </summary>
public class MergeVfxManager : MonoBehaviour
{
    public static MergeVfxManager Instance { get; private set; }

    [Header("Launch")]
    [SerializeField] private GameObject launchPrefab;       // pooled VFX at each projectile's launch position
    [SerializeField] private Vector3 launchScale = Vector3.one;
    [SerializeField] private float launchLifetime = 1f;

    [Header("Projectile")]
    [SerializeField] private GameObject projectilePrefab;   // pooled VFX that flies to the target
    [SerializeField] private Vector3 projectileScale = Vector3.one;
    [SerializeField] private float reachTime = 0.35f;       // travel time to the target
    [SerializeField] private float arcHeight = 1.2f;        // vertical arc peak

    [Header("Impact")]
    [SerializeField] private GameObject impactStar2Prefab;  // impact when the merge result is a 2-star unit
    [SerializeField] private GameObject impactStar3Prefab;  // impact when the merge result is a 3-star unit
    [SerializeField] private Vector3 impactScale = Vector3.one;
    [SerializeField] private float impactLifetime = 1f;

    [Header("Spawn")]
    [Tooltip("Delay before the upgraded unit appears, measured from the merge start")]
    [SerializeField] private float spawnDelay = 0.35f;

    private const float MinTravelDist = 0.05f; // skip near-zero-length projectiles

    public float ReachTime => reachTime;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }


    // Public API //

    /// <summary>Fly a projectile from each source to target; after reachTime play the star-specific impact and invoke onReach.</summary>
    public void PlayMerge(IReadOnlyList<Vector3> sources, Vector3 target, int resultStarLevel, Action onReach)
    {
        if (sources != null)
        {
            foreach (Vector3 src in sources)
            {
                PlayOneShot(launchPrefab, launchScale, launchLifetime, src, Quaternion.identity); // launch always fires

                // Skip the projectile when the source sits on the target (e.g. the anchor), but keep its launch.
                if (projectilePrefab != null && (target - src).sqrMagnitude >= MinTravelDist * MinTravelDist)
                {
                    GameObject proj = VfxPoolManager.Instance.Get(projectilePrefab, src, LookRot(src, target));
                    proj.transform.localScale = projectileScale;
                    FlyProjectile(proj, src, target).Forget();
                }
            }
        }
        PlayImpact(target, resultStarLevel).Forget(); // impact when the projectiles land (reachTime)
        SpawnAfterDelay(onReach).Forget();            // upgraded unit appears after spawnDelay
    }


    // Coroutines //

    /// <summary>Lerp a pooled projectile along an arc to the target, then return it to the pool.</summary>
    private async UniTaskVoid FlyProjectile(GameObject proj, Vector3 from, Vector3 to)
    {
        float elapsed = 0f;
        try
        {
            while (elapsed < reachTime)
            {
                float t = reachTime > 0f ? elapsed / reachTime : 1f;
                Vector3 pos = Vector3.Lerp(from, to, t);
                pos.y += arcHeight * Mathf.Sin(Mathf.PI * t); // arc peak at the midpoint
                if (proj != null) proj.transform.position = pos;
                elapsed += Time.deltaTime;
                await UniTask.Yield(PlayerLoopTiming.Update, this.GetCancellationTokenOnDestroy());
            }
        }
        catch (OperationCanceledException) { }
        finally { if (proj != null) VfxPoolManager.Instance.Return(projectilePrefab, proj); }
    }

    /// <summary>Wait reachTime (projectile flight), then play the star-specific impact at target.</summary>
    private async UniTaskVoid PlayImpact(Vector3 target, int resultStarLevel)
    {
        try { await UniTask.WaitForSeconds(reachTime, cancellationToken: this.GetCancellationTokenOnDestroy()); }
        catch (OperationCanceledException) { return; }

        PlayOneShot(GetImpactPrefab(resultStarLevel), impactScale, impactLifetime, target, Quaternion.identity);
    }

    /// <summary>Wait spawnDelay, then signal the caller to spawn the upgraded unit.</summary>
    private async UniTaskVoid SpawnAfterDelay(Action onReach)
    {
        try { await UniTask.WaitForSeconds(spawnDelay, cancellationToken: this.GetCancellationTokenOnDestroy()); }
        catch (OperationCanceledException) { }

        onReach?.Invoke(); // always spawn so consumed units are never lost
    }

    /// <summary>Impact VFX for the resulting star level. 3-star falls back to the 2-star VFX if unset.</summary>
    private GameObject GetImpactPrefab(int starLevel)
        => starLevel >= 3 ? (impactStar3Prefab != null ? impactStar3Prefab : impactStar2Prefab)
                          : impactStar2Prefab;

    /// <summary>Spawn a pooled one-shot VFX at pos and return it to the pool after lifetime.</summary>
    private void PlayOneShot(GameObject prefab, Vector3 scale, float lifetime, Vector3 pos, Quaternion rot)
    {
        if (prefab == null) return;
        GameObject fx = VfxPoolManager.Instance.Get(prefab, pos, rot);
        fx.transform.localScale = scale;
        ReturnDelayed(prefab, fx, lifetime).Forget();
    }

    private async UniTaskVoid ReturnDelayed(GameObject prefab, GameObject fx, float delay)
    {
        try { await UniTask.WaitForSeconds(delay, cancellationToken: this.GetCancellationTokenOnDestroy()); }
        catch (OperationCanceledException) { }
        finally { if (fx != null) VfxPoolManager.Instance.Return(prefab, fx); }
    }

    private static Quaternion LookRot(Vector3 from, Vector3 to)
    {
        Vector3 dir = to - from;
        return dir.sqrMagnitude > 0.0001f ? Quaternion.LookRotation(dir) : Quaternion.identity;
    }
}
