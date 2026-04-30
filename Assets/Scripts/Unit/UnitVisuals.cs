using UnityEngine;
using System.Collections.Generic;
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

    [Header("Fire Effect")]
    [SerializeField] private TrailRenderer bulletTrailPrefab;
    [SerializeField] private float bulletReachTime = 0.15f;
    [SerializeField] private int burstCount = 3;
    [SerializeField] private float shotInterval = 0.05f;
    [SerializeField] private float shotDelay = 0.05f;

    [Header("Sound Setting")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip fireSound;
    [SerializeField] private AudioClip skillSound;

    [Header("Trail Pool")]
    [SerializeField] private int poolSize = 5;
    private Queue<TrailRenderer> trailPool = new Queue<TrailRenderer>();
    public Queue<TrailRenderer> skillTrailPool = new Queue<TrailRenderer>();
    private static Transform globalPoolContainer;

    private CancellationTokenSource cts;

    private void Awake()
    {
        cts = new CancellationTokenSource();

        if (bulletTrailPrefab == null) return;

        if (globalPoolContainer == null)
        {
            GameObject containerObj = GameObject.Find("PoolContainer");

            if (containerObj != null)
            {
                globalPoolContainer = containerObj.transform;
            }
        }

            for (int i = 0; i < poolSize; i++)
        {
            TrailRenderer trail = Instantiate(bulletTrailPrefab, globalPoolContainer);
            trail.gameObject.SetActive(false);
            trailPool.Enqueue(trail);
        }
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
            // 유닛이 죽으면 안전하게 취소되도록 cts.Token 추가
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
    public TrailRenderer GetTrail(TrailRenderer trail, Queue<TrailRenderer> trailPool)
    {
        if (trailPool.Count == 0)
        {
            int expandSize = 2; // expend by 2

            for (int i = 0; i < expandSize; i++)
            {
                TrailRenderer newTrail = Instantiate(trail, globalPoolContainer);
                newTrail.gameObject.SetActive(false);
                trailPool.Enqueue(newTrail);
            }
        }
        trail = trailPool.Dequeue();


        trail.transform.position = firePoint.position;
        trail.gameObject.SetActive(true);
        trail.Clear();
        return trail;
    }

    public void ReturnTrail(TrailRenderer trail, Queue<TrailRenderer> returnPool)
    {
        trail.gameObject.SetActive(false);
        returnPool.Enqueue(trail);
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

        if (shotDelay > 0f)
        {
            await UniTask.WaitForSeconds(shotDelay, cancellationToken: cts.Token);
        }
        for (int i = 0; i < burstCount; i++)
        {
            if (target == null || target.Stats.CurrentHp <= 0) return;
            if (audioSource != null && fireSound != null)
            {
                audioSource.Play();
            }

            Vector3 finalHitPoint = target.Visuals.HitBox.position;

            TrailRenderer trail = GetTrail(bulletTrailPrefab, trailPool);

            // check last bullet
            bool isLastShot = (i == burstCount - 1);
            SpawnTrailAsync(trail, finalHitPoint, bulletReachTime, isLastShot ? onLastHit : null, (t) => ReturnTrail(t, trailPool)).Forget();

            // wait until next shot
            if (!isLastShot)
                await UniTask.WaitForSeconds(shotInterval, cancellationToken: cts.Token);
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
