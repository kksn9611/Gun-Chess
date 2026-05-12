using UnityEngine;
using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;

[RequireComponent(typeof(UnitController))]
public class UnitVisuals : MonoBehaviour
{
    [Header("Fire Point")]
    [SerializeField] private Transform firePoint;
    [SerializeField] private Transform hitBox;
    public Transform HitBox => hitBox;
    public Transform FirePoint => firePoint;

    [Header("Fire Effect")]
    [SerializeField] private float bulletReachTime = 0.15f;
    [SerializeField] private int burstCount = 3;
    [SerializeField] private float shotInterval = 0.05f;
    [Tooltip("if use the Animation Event, this is ignored")]
    [SerializeField] private float shotDelay = 0.05f;
    [Tooltip("Fire all pellets simultaneously (shotgun)")]
    [SerializeField] private bool burstAtOnce = false;
    [Tooltip("Random spread radius around hitbox (burstAtOnce only)")]
    [SerializeField] private float spreadRadius = 0.3f;
    [Tooltip("Wait for Animation Event (OnFireEvent) instead of shotDelay")]
    [SerializeField] private bool useAnimationEvent = false;

    [Header("Sound Setting")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip skillSound;

    // Trail prefabs from UnitData
    private TrailRenderer bulletTrailPrefab;
    private TrailRenderer skillTrailPrefab;

    private CancellationTokenSource cts;
    private UniTaskCompletionSource fireSignal;
    private UniTaskCompletionSource skillSignal;

    private void Awake()
    {
        cts = new CancellationTokenSource();
    }

    /// <summary>
    /// Initialize trail prefabs from UnitData and prewarm pools.
    /// Called by UnitController.Initialize().
    /// </summary>
    public void Initialize(UnitData data)
    {
        bulletTrailPrefab = data.bulletTrailPrefab;
        skillTrailPrefab  = data.skillTrailPrefab;

        if (bulletTrailPrefab != null && data.poolSize > 0)
            TrailPoolManager.Instance.Prewarm(bulletTrailPrefab, data.poolSize);
        if (skillTrailPrefab != null && data.skillPoolSize > 0)
            TrailPoolManager.Instance.Prewarm(skillTrailPrefab, data.skillPoolSize);
    }

    private void OnDestroy()
    {
        cts?.Cancel();
        cts?.Dispose();
    }

    public async UniTaskVoid PlaySkillSound(float delay)
    {
        if (delay > 0f)
        {
            await UniTask.WaitForSeconds(delay, cancellationToken: cts.Token);
        }
        audioSource.PlayOneShot(skillSound);
    }
    public void PlaySkillSound()
    {
        if (audioSource != null && skillSound != null)
        {
            audioSource.PlayOneShot(skillSound);
        }
    }
    public void PlaySound()
    {
        if (audioSource != null && audioSource.generator != null)
        {
            audioSource.Play();
        }
    }

    /// <summary>Called by Animation Event to trigger trail firing.</summary>
    public void OnFireEvent()
    {
        fireSignal?.TrySetResult();
    }
    public void OnSkillEvent()
    {
        skillSignal?.TrySetResult();
    }


    /// <summary>Skill trail prefab assigned from UnitData.</summary>
    public TrailRenderer SkillTrailPrefab => skillTrailPrefab;

    /// <summary>
    /// Get a trail from the centralized pool, positioned at fire point.
    /// </summary>
    public TrailRenderer GetTrail(TrailRenderer prefab)
    {
        return TrailPoolManager.Instance.Get(prefab, firePoint.position);
    }

    /// <summary>
    /// Get a skill trail from the centralized pool, positioned at fire point.
    /// </summary>
    public TrailRenderer GetSkillTrail()
    {
        if (skillTrailPrefab == null) return null;
        return TrailPoolManager.Instance.Get(skillTrailPrefab, firePoint.position);
    }

    /// <summary>
    /// Return a skill trail to the centralized pool.
    /// </summary>
    public void ReturnSkillTrail(TrailRenderer trail)
    {
        if (skillTrailPrefab == null) return;
        TrailPoolManager.Instance.Return(skillTrailPrefab, trail);
    }

    // Fire Effect //
    public void FireWeaponEffect(UnitController target, Action onLastHit)
    {
        if (target == null || bulletTrailPrefab == null)
        {
            onLastHit?.Invoke();
            return;
        }

        BurstAsync(target,shotDelay,onLastHit).Forget();
    }

    private async UniTaskVoid BurstAsync(UnitController target, float shotDelay, Action onLastHit)
    {
        // Wait for animation event or shotDelay
        if (useAnimationEvent)
        {
            fireSignal = new UniTaskCompletionSource();
            await fireSignal.Task.AttachExternalCancellation(cts.Token);
        }
        else if (shotDelay > 0f)
        {
            await UniTask.WaitForSeconds(shotDelay, cancellationToken: cts.Token);
        }

        if (target == null || target.Stats.CurrentHp <= 0) return;

        if (burstAtOnce)
        {
            // Shotgun: fire all pellets simultaneously with spread
            Vector3 center = target.Visuals.HitBox.position;

            for (int i = 0; i < burstCount; i++)
            {
                Vector3 offset = UnityEngine.Random.insideUnitSphere * spreadRadius;
                offset.y = 0f;
                Vector3 hitPoint = center + offset;

                TrailRenderer trail = GetTrail(bulletTrailPrefab);
                bool isLastShot = (i == burstCount - 1);
                SpawnTrailAsync(trail, hitPoint, bulletReachTime, isLastShot ? onLastHit : null, (t) => TrailPoolManager.Instance.Return(bulletTrailPrefab, t)).Forget();
            }
        }
        else
        {
            // Sequential burst fire
            for (int i = 0; i < burstCount; i++)
            {
                if (target == null || target.Stats.CurrentHp <= 0) return;

                Vector3 finalHitPoint = target.Visuals.HitBox.position;
                TrailRenderer trail = GetTrail(bulletTrailPrefab);

                bool isLastShot = (i == burstCount - 1);
                SpawnTrailAsync(trail, finalHitPoint, bulletReachTime, isLastShot ? onLastHit : null, (t) => TrailPoolManager.Instance.Return(bulletTrailPrefab, t)).Forget();

                if (!isLastShot)
                    await UniTask.WaitForSeconds(shotInterval, cancellationToken: cts.Token);
            }
        }
    }

    public void SpawnProjectile(TrailRenderer trail, Vector3 hitPoint, float reachTime, Action onHit, Action<TrailRenderer> returnToPool)
    {
        if (trail == null) return;
        SpawnTrailAsync(trail, hitPoint, reachTime, onHit, returnToPool).Forget();
    }

    public void SpawnProjectile(TrailRenderer trail, Transform target, float reachTime, Action onHit, Action<TrailRenderer> returnToPool)
    {
        if (trail == null) return;
        SpawnTrailHomingAsync(trail, target, reachTime, onHit, returnToPool).Forget();
    }

    private async UniTaskVoid SpawnTrailAsync(TrailRenderer trail, Vector3 hitPoint, float reachTime, Action onHit, Action<TrailRenderer> returnToPool)
    {
        Vector3 startPosition = trail.transform.position;

        if (reachTime <= 0f) reachTime = 0.01f;
        try
        {
            await trail.transform.DOMove(hitPoint, reachTime).SetEase(Ease.Linear).ToUniTask(cancellationToken: cts.Token);
            onHit?.Invoke();

            // wait for trail to fade before returning to pool
            await UniTask.Delay(System.TimeSpan.FromSeconds(trail.time), cancellationToken: cts.Token);
        }
        finally
        {
            returnToPool?.Invoke(trail);
        }
    }

    // Homing Trail //
    private async UniTaskVoid SpawnTrailHomingAsync(TrailRenderer trail, Transform target, float reachTime, Action onHit, Action<TrailRenderer> returnToPool)
    {
        Vector3 startPos = trail.transform.position;
        try
        {
            if (reachTime <= 0f) reachTime = 0.01f;

            // Calculate constant speed from initial distance
            Vector3 initialTarget = (target != null) ? target.position : startPos;
            float speed = Vector3.Distance(startPos, initialTarget) / reachTime;
            float elapsed = 0f;

            while (elapsed < reachTime)
            {
                Vector3 targetPos = (target != null) ? target.position : trail.transform.position;
                trail.transform.position = Vector3.MoveTowards(trail.transform.position, targetPos, speed * Time.deltaTime);
                elapsed += Time.deltaTime;
                await UniTask.Yield(cts.Token);
            }

            if (target != null)
            {
                trail.transform.position = target.position;
                onHit?.Invoke();
            }

            await UniTask.Delay(System.TimeSpan.FromSeconds(trail.time), cancellationToken: cts.Token);
        }
        finally
        {
            returnToPool?.Invoke(trail);
        }
    }
}
