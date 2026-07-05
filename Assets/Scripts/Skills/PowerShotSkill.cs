using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// Burst-fire skill dealing ATK * multiplier damage per shot.
/// Each shot targets either the current target or a random enemy.
/// Supports multiple shots with interval and per-shot delay.
/// </summary>
[CreateAssetMenu(fileName = "PowerShot", menuName = "Scriptable Objects/Skill/PowerShotSkill")]
public class PowerShotSkill : BaseSkill
{
    public enum ShotTargetMode { CurrentTarget, RandomEnemy } // who each shot aims at

    [Header("Skill Settings")]
    [Tooltip("Damage multiplier per shot relative to ATK")]
    public float damageMultiplier = 4f;
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

            // Face the target and play attack animation for each shot
            caster.Movement.LookAtTargetSkill(target.transform, ct).Forget();
            caster.Animator.PlaySkill();

            // Wait for the shoot moment: animation event or fixed delay
            if (useAnimationEvent)
                await caster.Visuals.WaitForSkillEvent(ct);
            else if (shotDelay > 0f)
                await UniTask.WaitForSeconds(shotDelay, cancellationToken: ct);

            // Calculate per-shot damage
            float damage = caster.Stats.CurrentAtt * damageMultiplier * caster.Stats.SkillDamageMultiplier;
            if (canCrit) damage = caster.Stats.ApplyCrit(damage, out _);

            // Spawn projectile
            GameObject projectile = caster.Visuals.GetSkillProjectile();
            GameObject prefabKey = caster.Visuals.SkillProjectilePrefab;
            caster.Visuals.PlaySkillSound(0).Forget();
            caster.Visuals.SpawnProjectile(projectile, prefabKey, target.Visuals.HitBox, reachTime, () => target.TakeDamage(damage, caster));
            Debug.Log($"[PowerShot] {caster.Stats.UnitData.unitName} → {target.Stats.UnitData.unitName} shot {i + 1}/{burstCount} ({damage} damage)");

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
