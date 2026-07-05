using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// Buffs N random allies (excluding the caster) with stat boosts for the rest of the battle.
/// Buff types and percentages are configured in Inspector via StatBoostEntry array.
/// </summary>
[CreateAssetMenu(fileName = "AllyBuff", menuName = "Scriptable Objects/Skill/AllyBuffSkill")]
public class AllyBuffSkill : BaseSkill
{
    [Header("Skill Settings")]
    [Tooltip("Stat boosts to apply on cast")]
    public StatBoostEntry[] boosts;

    [Tooltip("Number of random allies to buff (caster excluded)")]
    [Min(1)] public int targetCount = 1;

    public float skillSoundDelay = 1f;

    public override async UniTask<bool> Execute(UnitController caster, CancellationToken ct = default)
    {
        if (useAnimationEvent)
            await caster.Visuals.WaitForSkillEvent(ct);
        else
        {
            caster.Visuals.PlaySkillSound(skillSoundDelay).Forget();
            await UniTask.WaitForSeconds(castTime, cancellationToken: ct);
        }

        if (caster == null || caster.Stats.CurrentHp <= 0) return false;
        if (boosts == null || boosts.Length == 0) return false;

        List<UnitController> buffTargets = FindRandomAllies(caster, targetCount);
        if (buffTargets.Count == 0) return false;

        // Apply stat boosts to selected allies
        foreach (UnitController target in buffTargets)
        {
            foreach (var entry in boosts)
            {
                target.Stats.ApplyStatModifier(entry.statType, entry.percentBoost);
                Debug.Log($"[AllyBuff] {caster.Stats.UnitData.unitName} → {target.Stats.UnitData.unitName} +{entry.percentBoost}% {entry.statType}");
            }
        }
        return true;
    }

    // Target Search //

    private List<UnitController> FindRandomAllies(UnitController caster, int count)
    {
        IReadOnlyList<UnitController> allies = UnitManager.Instance.GetAlliesOf(caster.CurrentTeam);
        List<UnitController> candidates = new List<UnitController>();

        foreach (UnitController ally in allies)
        {
            if (ally == null || ally == caster) continue; // exclude caster
            if (ally.AI.CurrentState == UnitState.Dead) continue;
            candidates.Add(ally);
        }

        // Fisher-Yates shuffle, then take up to count
        for (int i = candidates.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (candidates[i], candidates[j]) = (candidates[j], candidates[i]);
        }

        if (candidates.Count > count)
            candidates.RemoveRange(count, candidates.Count - count);

        return candidates;
    }
}
