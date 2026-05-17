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
    [Header("AoE Skill Must use animation event")]
    [Header("Skill Settings")]
    [Tooltip("Damage multiplier relative to ATK")]
    public float damageMultiplier = 3f;
    [Tooltip("Stun Enable")]
    public bool isStun = false;
    public float stunDuration;

    [Tooltip("Area shape definition")]
    public AreaShapeData areaShape;
    public Color color;

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

            // Refresh pivot each cast (tracks target movement)
            UnitController primaryTarget = caster.AI.FindClosestTarget();
            
            // target check
            if (primaryTarget == null) break;
            caster.Movement.LookAtTargetSkill(primaryTarget.transform, ct).Forget();


            Vector3 pivot = (primaryTarget != null && primaryTarget.Stats.CurrentHp > 0)
                ? primaryTarget.Visuals.HitBox.position
                : caster.Visuals.FirePoint.position;

            var indicator = SkillAreaRenderer.Create(areaShape, caster.Visuals.FirePoint.position, pivot, color);
            indicator.ShowForDuration(0.3f).Forget();

            // Collect and damage targets
            List<UnitController> targets = AreaTargetingUtility.GetTargetsInArea(areaShape, caster, pivot);
            foreach (UnitController target in targets)
            {
                float damage = caster.Stats.CurrentAtt * damageMultiplier * caster.Stats.SkillDamageMultiplier;
                if (canCrit) damage = caster.Stats.ApplyCrit(damage, out _);
                target.TakeDamage(damage);
                if (isStun) target.CCHandler.ApplyStun(stunDuration);
            }
            // Multi Cast Logic
            if (i + 1 == castCount) break;
            caster.Animator.PlaySkill();
        }
        return true;
    }
}
