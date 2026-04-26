using System.Collections;
using UnityEngine;

/// <summary>
/// Single-target skill dealing ATK * multiplier damage to current target.
/// Cast time based on current attack speed.
/// </summary>
[CreateAssetMenu(fileName = "PowerShot", menuName = "Scriptable Objects/Skill/PowerShotSkill")]
public class PowerShotSkill : BaseSkill
{
    [Header("Skill Settings")]
    [Tooltip("Damage multiplier relative to ATK (2.0 = 200%)")]
    public float damageMultiplier = 4f;
    public TrailRenderer trail;
    public float reachTime = 0.3f;

    public override IEnumerator Execute(UnitController caster)
    {
        // Use Animation event to stop Animation
        yield return new WaitForSeconds(castTime);
        caster.Animator.ResumeAnimation(); // resumeAnimation
        // Deal multiplied damage to current target
        UnitController target = caster.AI.CurrentTarget;
        if (target == null || target.Stats.CurrentHp <= 0) yield break;

        TrailRenderer pooledTrail = caster.Visuals.GetTrail(trail, caster.Visuals.skillTrailPool);
        float damage = caster.Stats.CurrentAtt * damageMultiplier * caster.Stats.SkillDamageMultiplier;
        caster.Visuals.PlaySkillSound();
        caster.Visuals.SpawnProjectile(pooledTrail, target.Visuals.HitBox, reachTime, () => target.TakeDamage(damage), (t) => caster.Visuals.ReturnTrail(t, caster.Visuals.skillTrailPool));
        Debug.Log($"[PowerShot] {caster.Stats.UnitData.unitName} → {target.Stats.UnitData.unitName} ({damage} damage)");
        yield return new WaitForSeconds(reachTime);
    }
}
