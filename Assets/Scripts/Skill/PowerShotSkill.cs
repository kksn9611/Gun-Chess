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
    public float damageMultiplier = 2f;

    public override IEnumerator Execute(UnitController caster)
    {
        // Cast duration = same as attack cooldown
        float castDuration = 1f / caster.Stats.CurrentAttSpd;
        yield return new WaitForSeconds(castDuration);

        // Deal multiplied damage to current target
        UnitController target = caster.AI.CurrentTarget;
        if (target != null && target.Stats.CurrentHp > 0)
        {
            float damage = caster.Stats.CurrentAtt * damageMultiplier * caster.Stats.SkillDamageMultiplier;
            target.TakeDamage(damage);
            Debug.Log($"[PowerShot] {caster.Stats.UnitData.unitName} → {target.Stats.UnitData.unitName} ({damage} damage)");
        }
    }
}
