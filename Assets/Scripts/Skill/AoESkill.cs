using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

/// <summary>
/// AoE skill. Deals damage to all enemies within the area shape.
/// Aims at the primary target before casting.
/// </summary>
[CreateAssetMenu(fileName = "AoESkill", menuName = "Scriptable Objects/Skill/AoESkill")]
public class AoESkill : BaseSkill
{
    [Header("Skill Settings")]
    [Tooltip("Damage multiplier relative to ATK")]
    public float damageMultiplier = 3f;
    public float skillSoundDelay = 0f;

    [Tooltip("Area shape definition")]
    public AreaShapeData areaShape;
    public Color color;


    public override async UniTask<bool> Execute(UnitController caster, CancellationToken ct = default)
    {
        float skillSpeed = caster.Animator.SkillAnimLength / castTime;
        caster.Animator.SetSkillSpeed(skillSpeed);
        // Pivot = primary target HitBox (Circle center), fallback to caster FirePoint
        UnitController primaryTarget = caster.AI.CurrentTarget;

        Vector3 pivot = primaryTarget != null ? primaryTarget.Visuals.HitBox.position
            : caster.Visuals.FirePoint.position;
        var indicator = SkillAreaRenderer.Create(areaShape, caster.Visuals.FirePoint.position, pivot, color);

        // Cast wind-up
        await UniTask.WaitForSeconds(castTime, cancellationToken: ct);
        
        if (caster == null || caster.Stats.CurrentHp <= 0) return false;
        indicator.ShowForDuration(0.2f).Forget();

        // Collect targets in area
        List<UnitController> targets = AreaTargetingUtility.GetTargetsInArea(areaShape, caster, pivot);
        if (targets.Count == 0) return true;

        // Apply damage to all targets
        foreach (UnitController target in targets)
        {
            float damage = caster.Stats.CurrentAtt * damageMultiplier * caster.Stats.SkillDamageMultiplier;
            if (canCrit) damage = caster.Stats.ApplyCrit(damage, out _);

            target.TakeDamage(damage);
            Debug.Log($"[AoE] {caster.Stats.UnitData.unitName} → {target.Stats.UnitData.unitName} ({damage} damage)");
        }
        return true;
    }
}
