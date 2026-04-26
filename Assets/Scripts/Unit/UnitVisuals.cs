using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

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

    [Header("Sound Setting")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip fireSound;
    [SerializeField] private AudioClip skillSound;

    [Header("Trail Pool")]
    [SerializeField] private int poolSize = 5;
    private Queue<TrailRenderer> trailPool = new Queue<TrailRenderer>();
    public Queue<TrailRenderer> skillTrailPool = new Queue<TrailRenderer>();
    private static Transform globalPoolContainer;

    private void Awake()
    {
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
    public void PlaySkillSound()
    {
        audioSource.PlayOneShot(skillSound);
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

        StartCoroutine(BurstCoroutine(target, onLastHit));
    }


    private IEnumerator BurstCoroutine(UnitController target, Action onLastHit)
    {
        for (int i = 0; i < burstCount; i++)
        {
            if (target == null || target.Stats.CurrentHp <= 0) yield break;
            if (audioSource != null && fireSound != null)
            {
                audioSource.Play();
            }
            
            Vector3 finalHitPoint = target.Visuals.HitBox.position;

            TrailRenderer trail = GetTrail(bulletTrailPrefab, trailPool);

            // check last bullet
            bool isLastShot = (i == burstCount - 1);
            StartCoroutine(SpawnTrail(trail, finalHitPoint, bulletReachTime, isLastShot ? onLastHit : null, (trail) => ReturnTrail(trail,trailPool)));

            // wait until next shot
            if (!isLastShot)
                yield return new WaitForSeconds(shotInterval);
        }
    }

    public void SpawnProjectile(TrailRenderer trail, Vector3 hitPoint, float reachTime, Action onHit, Action<TrailRenderer> returnToPool)
    {
        if (trail == null) return;
        StartCoroutine(SpawnTrail(trail, hitPoint, reachTime, onHit, returnToPool));
    }

    public void SpawnProjectile(TrailRenderer trail, Transform target, float reachTime, Action onHit, Action<TrailRenderer> returnToPool)
    {
        if (trail == null) return;
        StartCoroutine(SpawnTrailHoming(trail, target, reachTime, onHit, returnToPool));
    }

    public IEnumerator SpawnTrail(TrailRenderer trail, Vector3 hitPoint, float reachTime, Action onHit, Action<TrailRenderer> returnToPool)
    {
        float time = 0;
        Vector3 startPosition = trail.transform.position;

        // reachtime can't be zero
        if (reachTime <= 0f) reachTime = 0.01f;

        while (time < 1)
        {
            trail.transform.position = Vector3.Lerp(startPosition, hitPoint, time);
            time += Time.deltaTime / reachTime;
            yield return null;
        }
        trail.transform.position = hitPoint;

        onHit?.Invoke();

        // wait for trail to fade before returning to pool
        yield return new WaitForSeconds(trail.time);

        returnToPool?.Invoke(trail);
    }

    private IEnumerator SpawnTrailHoming(TrailRenderer trail, Transform target, float reachTime, Action onHit, Action<TrailRenderer> returnToPool)
    {
        Vector3 startPos = trail.transform.position;

        // reachtime can't be zero
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
            yield return null;
        }

        if (target != null)
        {
            trail.transform.position = target.position;
            onHit?.Invoke();
        }

        yield return new WaitForSeconds(trail.time);
        returnToPool?.Invoke(trail);
    }
}
