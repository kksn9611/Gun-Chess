using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// Heals the N allies with the lowest HP percentage.
/// Heal amount = caster ATK * healMultiplier * SkillDamageMultiplier.
/// </summary>
[CreateAssetMenu(fileName = "HealSkill", menuName = "Scriptable Objects/Skill/HealSkill")]
public class HealSkill : BaseSkill
{
    [Header("Skill Settings")]
    [Tooltip("Heal multiplier relative to ATK")]
    public float healMultiplier = 2f;

    [Tooltip("Number of allies to heal (sorted by lowest HP%)")]
    [Min(1)] public int targetCount = 1;

    public override async UniTask Execute(UnitController caster, CancellationToken ct = default)
    {
        await UniTask.WaitForSeconds(castTime, cancellationToken: ct);
        caster.Animator.ResumeAnimation();

        List<UnitController> healTargets = FindLowestHpAllies(caster, targetCount);
        if (healTargets.Count == 0) return;

        float healAmount = caster.Stats.CurrentAtt * healMultiplier * caster.Stats.SkillDamageMultiplier;
        caster.Visuals.PlaySkillSound();

        foreach (UnitController target in healTargets)
        {
            target.Stats.SetHp(target.Stats.CurrentHp + healAmount);
            Debug.Log($"[Heal] {caster.Stats.UnitData.unitName} → {target.Stats.UnitData.unitName} (+{healAmount} HP)");
        }
    }

    // Target Search //

    private List<UnitController> FindLowestHpAllies(UnitController caster, int count)
    {
        IReadOnlyList<UnitController> allies = UnitManager.Instance.GetAlliesOf(caster.CurrentTeam);
        List<UnitController> damaged = new List<UnitController>();

        foreach (UnitController ally in allies)
        {
            if (ally == null || ally.AI.CurrentState == UnitState.Dead) continue;
            if (ally.Stats.CurrentHp >= ally.Stats.CurrentMaxHp) continue;
            damaged.Add(ally);
        }

        // Sort by HP% ascending
        damaged.Sort((a, b) =>
        {
            float pctA = a.Stats.CurrentHp / a.Stats.CurrentMaxHp;
            float pctB = b.Stats.CurrentHp / b.Stats.CurrentMaxHp;
            return pctA.CompareTo(pctB);
        });

        // Take up to count targets; fallback to self if no one is damaged
        if (damaged.Count == 0)
        {
            damaged.Add(caster);
            return damaged;
        }

        if (damaged.Count > count)
            damaged.RemoveRange(count, damaged.Count - count);

        return damaged;
    }
}
