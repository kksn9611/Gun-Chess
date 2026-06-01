using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// Burst-fire skill dealing ATK * multiplier damage per shot.
/// Supports multiple shots with interval and per-shot delay.
/// </summary>
[CreateAssetMenu(fileName = "PowerShot", menuName = "Scriptable Objects/Skill/PowerShotSkill")]
public class PowerShotSkill : BaseSkill
{
    [Header("Skill Settings")]
    [Tooltip("Damage multiplier per shot relative to ATK")]
    public float damageMultiplier = 4f;
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
        // Initial cast wind-up
        await UniTask.WaitForSeconds(castTime, cancellationToken: ct);

        UnitController target = caster.AI.CurrentTarget;
        if (target == null || target.Stats.CurrentHp <= 0) return false;

        // Fire burst
        for (int i = 0; i < burstCount; i++)
        {
            if (ct.IsCancellationRequested) return false;

            // Re-validate target each shot
            target = caster.AI.CurrentTarget;
            if (target == null || target.Stats.CurrentHp <= 0) break;

            // Play attack animation for each shot
            caster.Animator.PlaySkill();

            // Delay before projectile spawns
            if (shotDelay > 0f)
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

            // Wait interval before next shot (skip on last shot)
            if (i < burstCount - 1)
                await UniTask.WaitForSeconds(shotInterval, cancellationToken: ct);
            
        }
        // wait for return another animation
        await UniTask.WaitForSeconds(shotDelay, cancellationToken: ct);
        // Wait for last projectile to reach target
        await UniTask.WaitForSeconds(reachTime, cancellationToken: ct);
        return true;
    }
}
