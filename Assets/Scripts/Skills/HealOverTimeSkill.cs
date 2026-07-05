using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// Applies a Heal over Time to the caster.
/// Total heal = caster ATK * healMultiplier * SkillDamageMultiplier, split across tickCount ticks.
/// The HoT runs in the background (UnitStats), so the caster keeps acting after the cast.
/// </summary>
[CreateAssetMenu(fileName = "HealOverTimeSkill", menuName = "Scriptable Objects/Skill/HealOverTimeSkill")]
public class HealOverTimeSkill : BaseSkill
{
    [Header("Skill Settings")]

    public float skillSoundDelay = 0f;
    [Tooltip("Total heal multiplier relative to ATK (spread across all ticks)")]
    public float healMultiplier = 5f;

    [Header("HoT Settings")]
    [Tooltip("Total duration of the heal over time")]
    public float duration = 3f;
    [Tooltip("Number of heal ticks across the duration")]
    [Min(1)] public int tickCount = 3;

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

        // Fire the HoT and return; UnitStats owns the tick loop
        float totalHeal = caster.Stats.CurrentAtt * healMultiplier * caster.Stats.SkillDamageMultiplier;
        caster.Stats.ApplyHealOverTime(totalHeal, duration, tickCount);
        Debug.Log($"[HoT] {caster.Stats.UnitData.unitName} starts healing {totalHeal} over {duration}s ({tickCount} ticks)");
        return true;
    }
}
