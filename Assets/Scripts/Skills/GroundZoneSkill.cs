using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// Fires a projectile at the current target; on landing it plays an impact VFX and drops a
/// damage pool (DoT ground effect) at the projectile's final position.
/// The pool persists and damages enemies standing inside it on a fixed interval.
/// Lifecycle is owned by GroundZone, so the pool survives after the cast completes.
/// If no projectile prefab is set, the pool drops instantly on the target.
/// </summary>
[CreateAssetMenu(fileName = "GroundZoneSkill", menuName = "Scriptable Objects/Skill/GroundZoneSkill")]
public class GroundZoneSkill : BaseSkill
{
    [Header("Skill Settings")]
    [Tooltip("Damage multiplier relative to ATK, applied every tick")]
    public float damageMultiplier = 1f;
    public float skillSoundDelay = 0f;

    [Header("Projectile")]
    [Tooltip("Projectile fired at the target; if empty the pool drops instantly on the target")]
    public GameObject projectilePrefab;
    [Tooltip("Seconds for the projectile to reach the target")]
    public float reachTime = 0.4f;
    public Vector3 projectileScale = Vector3.one;

    [Header("Impact")]
    [Tooltip("One-shot VFX played where the projectile lands")]
    public GameObject impactVfxPrefab;
    public Vector3 impactVfxScale = Vector3.one;
    [Tooltip("Seconds before the impact VFX is destroyed")]
    public float impactVfxLifetime = 3f;

    [Header("Pool Settings")]
    [Tooltip("Circle radius of the pool")]
    public float radius = 2f;
    [Tooltip("Total lifetime of the pool")]
    public float duration = 4f;
    [Tooltip("Seconds between damage ticks")]
    [Min(0.05f)] public float tickInterval = 1f;
    [Tooltip("Indicator color (used when no pool VFX prefab is assigned)")]
    public Color color = new Color(0.6f, 0f, 0.8f, 0.4f);

    [Header("Pool VFX")]
    [Tooltip("Optional looping pool VFX; falls back to the indicator quad if empty")]
    public GameObject poolVfxPrefab;
    public Vector3 vfxScale = Vector3.one;

    public override async UniTask<bool> Execute(UnitController caster, CancellationToken ct = default)
    {
        // Require a live target; capture its position as the fallback drop location
        UnitController target = caster.AI.CurrentTarget;
        if (target == null || target.Stats.CurrentHp <= 0) return false;
        Vector3 fallbackCenter = target.transform.position;

        // Cast wind-up: animation event or fixed castTime
        if (useAnimationEvent)
            await caster.Visuals.WaitForSkillEvent(ct);
        else
        {
            caster.Visuals.PlaySkillSound(skillSoundDelay).Forget();
            await UniTask.WaitForSeconds(castTime, cancellationToken: ct);
        }

        if (caster == null || caster.Stats.CurrentHp <= 0) return false;

        // Damage per tick snapshot (the pool is detached from the caster afterward)
        float damagePerTick = caster.Stats.CurrentAtt * damageMultiplier * caster.Stats.SkillDamageMultiplier;

        // Resolve the pool center: fly a projectile, or drop instantly if none is set
        Vector3 center;
        if (projectilePrefab != null)
            center = await FlyProjectileAsync(caster, target, ct);
        else
            center = (target != null && target.Stats.CurrentHp > 0) ? target.transform.position : fallbackCenter;

        // Impact VFX at the landing point (one-shot, self-destructs)
        if (impactVfxPrefab != null)
        {
            GameObject vfx = Instantiate(impactVfxPrefab, center, Quaternion.identity);
            if (impactVfxScale != Vector3.one) vfx.transform.localScale = impactVfxScale;
            Destroy(vfx, impactVfxLifetime);
        }

        GroundZone.Create(caster, center, radius, damagePerTick, duration, tickInterval,
            canCrit, color, poolVfxPrefab, vfxScale);
        return true;
    }

    // Projectile Flight //

    /// <summary>
    /// Launch the projectile from the caster and return its landing position.
    /// Tracks the target while it lives; if the target dies mid-flight the projectile keeps its
    /// last aim point, so the pool always lands where the projectile stops.
    /// </summary>
    private async UniTask<Vector3> FlyProjectileAsync(UnitController caster, UnitController target, CancellationToken ct)
    {
        Vector3 spawnPos = caster.Visuals.FirePoint.position;
        GameObject proj  = VfxPoolManager.Instance.Get(projectilePrefab, spawnPos, Quaternion.identity);
        if (projectileScale != Vector3.one) proj.transform.localScale = projectileScale;

        // Aim point follows the target's HitBox until it is destroyed
        Transform hitBox = (target != null) ? target.Visuals.HitBox : null;
        Vector3 aimPos   = (hitBox != null) ? hitBox.position : spawnPos;

        float rt      = reachTime <= 0f ? 0.01f : reachTime;
        float speed   = Vector3.Distance(spawnPos, aimPos) / rt;
        float elapsed = 0f;

        try
        {
            while (elapsed < rt)
            {
                if (proj == null) break;

                // Track the target while it exists; keep the last aim point once it is gone
                if (hitBox != null) aimPos = hitBox.position;
                if (proj.transform.position != aimPos) proj.transform.LookAt(aimPos);
                proj.transform.position = Vector3.MoveTowards(proj.transform.position, aimPos, speed * Time.deltaTime);

                elapsed += Time.deltaTime;
                await UniTask.Yield(ct);
            }
            return (proj != null) ? proj.transform.position : aimPos;
        }
        finally
        {
            if (proj != null) VfxPoolManager.Instance.Return(projectilePrefab, proj);
        }
    }
}
