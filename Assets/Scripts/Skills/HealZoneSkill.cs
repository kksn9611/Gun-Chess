using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// Drops a persistent, stationary healing zone (HoT ground effect) that heals allies inside a circle.
/// The zone is fixed at its cast-time position. Center is either the caster or the lowest-HP% ally.
/// Lifecycle is owned by GroundZone, so the zone survives after the cast completes.
/// </summary>
[CreateAssetMenu(fileName = "HealZoneSkill", menuName = "Scriptable Objects/Skill/HealZoneSkill")]
public class HealZoneSkill : BaseSkill
{
    public enum ZoneCenterMode { Caster, LowestHpAlly } // where the zone is centered

    [Header("Skill Settings")]
    [Tooltip("Heal multiplier relative to ATK, applied every tick")]
    public float healMultiplier = 1f;
    [Tooltip("Where the healing zone is centered (fixed at cast time)")]
    public ZoneCenterMode centerMode = ZoneCenterMode.Caster;
    public float skillSoundDelay = 0f;

    [Header("Pool Settings")]
    [Tooltip("Circle radius of the zone")]
    public float radius = 2f;
    [Tooltip("Total lifetime of the zone")]
    public float duration = 4f;
    [Tooltip("Seconds between heal ticks")]
    [Min(0.05f)] public float tickInterval = 1f;
    [Tooltip("Indicator color (used when no pool VFX prefab is assigned)")]
    public Color color = new Color(0.3f, 1f, 0.4f, 0.4f);

    [Header("Pool VFX")]
    [Tooltip("Optional looping zone VFX; falls back to the indicator quad if empty")]
    public GameObject poolVfxPrefab;
    public Vector3 vfxScale = Vector3.one;

    [Header("Cast VFX")]
    [Tooltip("Scale applied to castVfxPrefab (cast effect on the caster)")]
    public Vector3 castVfxScale = Vector3.one;

    public override async UniTask<bool> Execute(UnitController caster, CancellationToken ct = default)
    {
        // Cast wind-up: animation event or fixed castTime
        if (useAnimationEvent)
            await caster.Visuals.WaitForSkillEvent(ct);
        else
        {
            caster.Visuals.PlaySkillSound(skillSoundDelay).Forget();
            await UniTask.WaitForSeconds(castTime, cancellationToken: ct);
        }

        if (caster == null || caster.Stats.CurrentHp <= 0) return false;

        // Cast VFX on the caster //
        if (castVfxPrefab != null)
        {
            Vector3 casterPos = caster.transform.position;
            Vector3 vfxPos = new Vector3(casterPos.x, 0.1f, casterPos.z);
            GameObject vfx = VfxPoolManager.Instance.Get(castVfxPrefab, vfxPos, Quaternion.identity);
            vfx.transform.localScale = castVfxScale;
            ReturnVfxDelayed(castVfxPrefab, vfx, 5f, ct).Forget();
        }

        // Resolve the fixed center point
        Vector3 center;
        if (centerMode == ZoneCenterMode.LowestHpAlly)
        {
            List<UnitController> target = AreaTargetingUtility.FindLowestHpAllies(caster, 1);
            center = target[0].transform.position; // never empty (self-fallback)
        }
        else
        {
            center = caster.transform.position;
        }

        // Heal per tick snapshot (the zone is detached from the caster afterward)
        float healPerTick = caster.Stats.CurrentAtt * healMultiplier * caster.Stats.SkillDamageMultiplier;

        GroundZone.Create(caster, center, radius, healPerTick, duration, tickInterval,
            applyCrit: false, color, poolVfxPrefab, vfxScale, effect: ZoneEffect.Heal);
        return true;
    }

    private async UniTaskVoid ReturnVfxDelayed(GameObject prefab, GameObject instance, float delay, CancellationToken ct)
    {
        try
        {
            await UniTask.WaitForSeconds(delay, cancellationToken: ct);
        }
        catch (System.OperationCanceledException) { }
        finally
        {
            if (instance != null)
                VfxPoolManager.Instance.Return(prefab, instance);
        }
    }
}
