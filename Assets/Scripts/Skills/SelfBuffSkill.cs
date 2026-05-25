using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// Self-buff skill. Applies multiple stat boosts to the caster for the rest of the battle.
/// Buff types and percentages are configured in Inspector via StatBoostEntry array.
/// </summary>
[CreateAssetMenu(fileName = "SelfBuff", menuName = "Scriptable Objects/Skill/SelfBuffSkill")]
public class SelfBuffSkill : BaseSkill
{
    [Header("Skill Settings")]
    [Tooltip("Stat boosts to apply on cast")]
    public StatBoostEntry[] boosts;

    public float skillSoundDelay = 1f;

    public override async UniTask<bool> Execute(UnitController caster, CancellationToken ct = default)
    {
        // skill speed calculate
        float skillSpeed = caster.Animator.SkillAnimLength / castTime;
        // set Skill speed;
        caster.Animator.SetSkillSpeed(skillSpeed);
        caster.Visuals.PlaySkillSound(skillSoundDelay).Forget();
        // Wait for cast animation
        await UniTask.WaitForSeconds(castTime, cancellationToken: ct);

        if (caster == null || caster.Stats.CurrentHp <= 0) return false;
        if (boosts == null || boosts.Length == 0) return false;

        // Apply all stat boosts to self
        foreach (var entry in boosts)
        {
            caster.Stats.ApplyStatModifier(entry.statType, entry.percentBoost);
            Debug.Log($"[SelfBuff] {caster.Stats.UnitData.unitName} +{entry.percentBoost}% {entry.statType}");
        }
        return true;
    }
}
