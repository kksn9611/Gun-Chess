using UnityEngine;
using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;

[RequireComponent(typeof(UnitController))]
public class UnitVisuals : MonoBehaviour
{
    private UnitController unit;
    [Header("Fire Point")]
    [SerializeField] private Transform firePoint;
    [SerializeField] private Transform hitBox;
    public Transform HitBox => hitBox;
    public Transform FirePoint => firePoint;

    [Header("Fire Effect")]
    [SerializeField] private float bulletReachTime = 0.15f;
    [SerializeField] private int burstCount = 3;
    [SerializeField] private float shotInterval = 0.05f;
    [Tooltip("Fire all pellets simultaneously (shotgun)")]
    [SerializeField] private bool burstAtOnce = false;
    [Tooltip("Random spread radius around hitbox (burstAtOnce only)")]
    [SerializeField] private float spreadRadius = 0.3f;

    [Header("Heal Effect")]
    [SerializeField] private GameObject healEffect;

    [Header("Sound Setting")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioSource skillAudioSource;
    [SerializeField] private AudioClip skillSound;
    [Range(0f, 1f)]
    [SerializeField] private float skillSoundVolume = 1f;

    // Trail / VFX prefabs from UnitData
    private TrailRenderer bulletTrailPrefab;
    private GameObject skillProjectilePrefab;

    private CancellationTokenSource cts;
    private UniTaskCompletionSource fireSignal;
    private UniTaskCompletionSource skillSignal;

    private void Awake()
    {
        if (unit == null)
        { 
            unit = GetComponent<UnitController>();
        }
        if (healEffect != null)
        {
            healEffect = Instantiate(healEffect, this.transform);
            healEffect.SetActive(false);
        }
        cts = new CancellationTokenSource();
    }
    private void OnEnable()
    {
        unit.Stats.OnHealed += PlayHealEffect;
    }

    private void OnDisable()
    {
        unit.Stats.OnHealed -= PlayHealEffect;
    }

    /// <summary>
    /// Initialize trail prefabs from UnitData and prewarm pools.
    /// Called by UnitController.Initialize().
    /// </summary>
    public void Initialize(UnitData data)
    {
        bulletTrailPrefab    = data.bulletTrailPrefab;
        skillProjectilePrefab = data.skillProjectilePrefab;

        if (bulletTrailPrefab != null && data.poolSize > 0)
            TrailPoolManager.Instance.Prewarm(bulletTrailPrefab, data.poolSize);
        if (skillProjectilePrefab != null && data.skillPoolSize > 0)
            VfxPoolManager.Instance.Prewarm(skillProjectilePrefab, data.skillPoolSize);
    }
    private void OnDestroy()
    {
        cts?.Cancel();
        cts?.Dispose();
    }

    public void PlayHealEffect()
    {
        if(healEffect == null) return;

        healEffect.SetActive(true);
        HealEffectDisable().Forget();

    }
    public async UniTaskVoid HealEffectDisable()
    {
        await UniTask.Delay(1500);
        healEffect.SetActive(false);
    }

    public void PlaySkillSoundVolume(float volume)
    {
        skillAudioSource.volume = volume;
        skillAudioSource.PlayOneShot(skillSound);
    }

    public async UniTaskVoid PlaySkillSound(float delay)
    {
        if (delay > 0f)
        {
            await UniTask.WaitForSeconds(delay, cancellationToken: cts.Token);
        }
        audioSource.PlayOneShot(skillSound, skillSoundVolume);
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

    public UniTask WaitForSkillEvent(CancellationToken ct)
    {
        skillSignal = new UniTaskCompletionSource();
        return skillSignal.Task.AttachExternalCancellation(ct);
    }


    /// <summary>Skill projectile prefab assigned from UnitData.</summary>
    public GameObject SkillProjectilePrefab => skillProjectilePrefab;

    /// <summary>
    /// Get a trail from the centralized pool, positioned at fire point.
    /// </summary>
    public TrailRenderer GetTrail(TrailRenderer prefab)
    {
        return TrailPoolManager.Instance.Get(prefab, firePoint.position);
    }

    /// <summary>
    /// Get a skill projectile from the pool, positioned at fire point.
    /// </summary>
    public GameObject GetSkillProjectile()
    {
        if (skillProjectilePrefab == null) return null;
        return VfxPoolManager.Instance.Get(skillProjectilePrefab, firePoint.position, Quaternion.identity);
    }

    // Fire Effect //
    public void FireWeaponEffect(UnitController target, Action onLastHit)
    {
        if (target == null || bulletTrailPrefab == null)
        {
            onLastHit?.Invoke();
            return;
        }

        BurstAsync(target, onLastHit).Forget();
    }
    private async UniTaskVoid BurstAsync(UnitController target, Action onLastHit)
    {
        // Wait for Animation Event (OnFireEvent)
        fireSignal = new UniTaskCompletionSource();
        await fireSignal.Task.AttachExternalCancellation(cts.Token);

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

    /// <summary>Spawn a pooled GameObject projectile that homes toward target.</summary>
    public void SpawnProjectile(GameObject projectile, GameObject prefabKey, Transform target, float reachTime, Action onHit)
    {
        if (projectile == null) return;
        SpawnProjectileHomingAsync(projectile, prefabKey, target, reachTime, onHit).Forget();
    }

    private async UniTaskVoid SpawnProjectileHomingAsync(GameObject projectile, GameObject prefabKey, Transform target, float reachTime, Action onHit)
    {
        try
        {
            if (reachTime <= 0f) reachTime = 0.01f;
            Vector3 initialTarget = (target != null) ? target.position : projectile.transform.position;
            float speed = Vector3.Distance(projectile.transform.position, initialTarget) / reachTime;
            float elapsed = 0f;

            while (elapsed < reachTime)
            {
                if (projectile == null) return;
                Vector3 targetPos = (target != null) ? target.position : projectile.transform.position;
                if (projectile.transform.position != targetPos)
                {
                    projectile.transform.LookAt(targetPos);
                }
                projectile.transform.position = Vector3.MoveTowards(projectile.transform.position, targetPos, speed * Time.deltaTime);
                elapsed += Time.deltaTime;
                await UniTask.Yield(cts.Token);
            }

            if (target != null && projectile != null)
            {
                projectile.transform.position = target.position;
                onHit?.Invoke();
            }
        }
        finally
        {
            if (projectile != null)
                VfxPoolManager.Instance.Return(prefabKey, projectile);
        }
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
