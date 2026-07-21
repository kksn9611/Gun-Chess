using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// Fires a configurable projectile at the target.
/// Pre-calculates final damage and passes it to the Projectile component.
/// Supports Multi-Cast: fires castCount times, re-acquiring the target each shot.
/// </summary>
[CreateAssetMenu(fileName = "ProjectileSkill", menuName = "Scriptable Objects/Skill/ProjectileSkill")]
public class ProjectileSkill : BaseSkill
{
    [Header("Skill Settings")]
    public float damageMultiplier = 4f;
    [Tooltip("Explosion damage multiplier relative to ATK (ignored if useExplosion is off)")]
    public float explosionDamageMultiplier = 2f;

    [Header("Projectile")]
    public ProjectileData projectileData;

    [Header("Multi-Cast")]
    [Min(1)] public int castCount = 1;

    public override async UniTask<bool> Execute(UnitController caster, CancellationToken ct = default)
    {
        for (int i = 0; i < castCount; i++)
        {
            // Each cast waits for animation event or timer
            if (useAnimationEvent)
                await caster.Visuals.WaitForSkillEvent(ct);
            else
                await UniTask.WaitForSeconds(castTime, cancellationToken: ct);

            if (caster == null || caster.Stats.CurrentHp <= 0) return false;

            // Re-acquire the target every shot; retarget a new enemy if the current one is gone
            UnitController target = caster.AI.CurrentTarget;
            if (target == null || target.Stats.CurrentHp <= 0)
            {
                target = caster.AI.FindClosestTarget();
                caster.Movement.LookAtTargetSkill(target.transform, ct).Forget();
            }
            if (target == null || target.Stats.CurrentHp <= 0) continue; // no enemies left

            // Pre-calculate final damages separately (fresh crit roll per cast)
            float baseDmg = caster.Stats.CurrentAtt * caster.Stats.SkillDamageMultiplier;
            float hitDamage = baseDmg * damageMultiplier;
            float explodeDamage = baseDmg * explosionDamageMultiplier;
            if (canCrit)
            {
                hitDamage = caster.Stats.ApplyCrit(hitDamage, out _);
                explodeDamage = caster.Stats.ApplyCrit(explodeDamage, out _);
            }

            // Spawn from pool and fire
            Vector3 spawnPos = caster.Visuals.FirePoint.position;
            GameObject go = VfxPoolManager.Instance.Get(projectileData.prefab, spawnPos, Quaternion.identity);
            Projectile projectile = go.GetComponent<Projectile>();
            projectile.Fire(hitDamage, explodeDamage, caster.CurrentTeam, projectileData, target.Visuals.HitBox, caster);

            // Multi Cast Logic
            if (i + 1 == castCount) break;
            caster.Animator.PlaySkill();
        }
        return true;
    }
}
