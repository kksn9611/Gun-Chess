using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// Fires a configurable projectile at the target.
/// Pre-calculates final damage and passes it to the Projectile component.
/// </summary>
[CreateAssetMenu(fileName = "ProjectileSkill", menuName = "Scriptable Objects/Skill/ProjectileSkill")]
public class ProjectileSkill : BaseSkill
{
    [Header("Skill Settings")]
    public float damageMultiplier = 4f;

    [Header("Projectile")]
    public ProjectileData projectileData;

    public override async UniTask<bool> Execute(UnitController caster, CancellationToken ct = default)
    {
        if (useAnimationEvent)
            await caster.Visuals.WaitForSkillEvent(ct);
        else
            await UniTask.WaitForSeconds(castTime, cancellationToken: ct);

        UnitController target = caster.AI.CurrentTarget;
        if (target == null || target.Stats.CurrentHp <= 0) return false;

        // Pre-calculate final damage
        float damage = caster.Stats.CurrentAtt * damageMultiplier * caster.Stats.SkillDamageMultiplier;
        if (canCrit) damage = caster.Stats.ApplyCrit(damage, out _);

        // Spawn from pool and fire
        Vector3 spawnPos = caster.Visuals.FirePoint.position;
        GameObject go = VfxPoolManager.Instance.Get(projectileData.prefab, spawnPos, Quaternion.identity);
        Projectile projectile = go.GetComponent<Projectile>();
        projectile.Fire(damage, caster.CurrentTeam, projectileData, target.Visuals.HitBox);

        return true;
    }
}
