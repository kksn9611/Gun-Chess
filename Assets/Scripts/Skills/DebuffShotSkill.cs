using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// Burst-fire skill that applies stat debuffs to enemies on hit.
/// Each shot targets either the current target or a random enemy.
/// </summary>
[CreateAssetMenu(fileName = "DebuffShot", menuName = "Scriptable Objects/Skill/DebuffShotSkill")]
public class DebuffShotSkill : BaseSkill
{
    public enum ShotTargetMode { CurrentTarget, RandomEnemy } // who each shot aims at

    [Header("Skill Settings")]
    [Tooltip("Stat debuffs to apply on hit (percent = multiplicative reduction, e.g. 30 = x0.7)")]
    public StatBoostEntry[] debuffs;
    [Tooltip("Aim at the current target or pick a random enemy per shot")]
    public ShotTargetMode targetMode = ShotTargetMode.CurrentTarget;
    public float reachTime = 0.3f;

    [Header("Burst Settings")]
    [Tooltip("Number of shots to fire")]
    [Min(1)] public int burstCount = 1;
    [Tooltip("Time between each shot")]
    public float shotInterval = 0.3f;
    [Tooltip("animation delay")]
    public float shotDelay = 0.1f;

    public override async UniTask<bool> Execute(UnitController caster, CancellationToken ct = default)
    {
        if (debuffs == null || debuffs.Length == 0) return false;

        // Initial cast wind-up (skipped when animation events drive timing)
        if (!useAnimationEvent)
            await UniTask.WaitForSeconds(castTime, cancellationToken: ct);

        if (PickTarget(caster) == null) return false;

        // Fire burst
        for (int i = 0; i < burstCount; i++)
        {
            if (ct.IsCancellationRequested) return false;

            // Re-pick target each shot
            UnitController target = PickTarget(caster);
            if (target == null) break;

            // Play attack animation for each shot
            caster.Movement.LookAtTargetSkill(target.transform, ct).Forget();
            caster.Animator.PlaySkill();

            // Wait for the shoot moment: animation event or fixed delay
            if (useAnimationEvent)
                await caster.Visuals.WaitForSkillEvent(ct);
            else if (shotDelay > 0f)
                await UniTask.WaitForSeconds(shotDelay, cancellationToken: ct);

            // Spawn projectile; apply debuff on hit
            GameObject projectile = caster.Visuals.GetSkillProjectile();
            GameObject prefabKey = caster.Visuals.SkillProjectilePrefab;
            caster.Visuals.PlaySkillSound(0).Forget();
            caster.Visuals.SpawnProjectile(projectile, prefabKey, target.Visuals.HitBox, reachTime, () => ApplyDebuff(caster, target));
            Debug.Log($"[DebuffShot] {caster.Stats.UnitData.unitName} → {target.Stats.UnitData.unitName} shot {i + 1}/{burstCount}");

            // Wait interval before next shot (timer mode only; events pace themselves)
            if (!useAnimationEvent && i < burstCount - 1)
                await UniTask.WaitForSeconds(shotInterval, cancellationToken: ct);
        }

        // Wait for return animation (timer mode only)
        if (!useAnimationEvent)
            await UniTask.WaitForSeconds(shotDelay, cancellationToken: ct);
        // Wait for last projectile to reach target
        await UniTask.WaitForSeconds(reachTime, cancellationToken: ct);
        return true;
    }

    // Debuff //

    private void ApplyDebuff(UnitController caster, UnitController target)
    {
        if (target == null || target.Stats.CurrentHp <= 0) return;
        foreach (var entry in debuffs)
            target.Stats.ApplyStatDebuff(entry.statType, entry.percentBoost);
    }

    // Target Search //

    private UnitController PickTarget(UnitController caster)
    {
        if (targetMode == ShotTargetMode.CurrentTarget)
        {
            UnitController current = caster.AI.CurrentTarget;
            return (current != null && current.Stats.CurrentHp > 0) ? current : null;
        }

        // RandomEnemy: pick a random living enemy
        IReadOnlyList<UnitController> enemies = UnitManager.Instance.GetEnemiesOf(caster.CurrentTeam);
        List<UnitController> alive = new List<UnitController>();
        foreach (UnitController enemy in enemies)
        {
            if (enemy == null || enemy.AI.CurrentState == UnitState.Dead) continue;
            if (enemy.Stats.CurrentHp <= 0) continue;
            alive.Add(enemy);
        }

        if (alive.Count == 0) return null;
        return alive[Random.Range(0, alive.Count)];
    }
}
