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

    public float skillSoundDelay = 0f;
    [Tooltip("Heal multiplier relative to ATK")]
    public float healMultiplier = 5f;

    [Tooltip("Number of allies to heal (sorted by lowest HP%)")]
    [Min(1)] public int targetCount = 1;

    [Header("Cast VFX")]
    [Tooltip("Scale applied to castVfxPrefab")]
    public Vector3 vfxScale = Vector3.one;

    public override async UniTask<bool> Execute(UnitController caster, CancellationToken ct = default)
    {
        if (useRootMotion) caster.Animator.SetApplyRootMotion();
        try
        {
            if (useAnimationEvent)
                await caster.Visuals.WaitForSkillEvent(ct);
            else
            {
                caster.Visuals.PlaySkillSound(skillSoundDelay).Forget();
                await UniTask.WaitForSeconds(castTime, cancellationToken: ct);
            }
            if (caster == null || caster.Stats.CurrentHp <= 0) return false;

            // Cast VFX on the caster //
            if (castVfxPrefab != null)
            {
                Vector3 pos = caster.transform.position;
                Vector3 vfxPos = new Vector3(pos.x, 0.1f, pos.z);
                GameObject vfx = VfxPoolManager.Instance.Get(castVfxPrefab, vfxPos, Quaternion.identity);
                vfx.transform.localScale = vfxScale;
                ReturnVfxDelayed(castVfxPrefab, vfx, 5f, ct).Forget();
            }

            List<UnitController> healTargets = AreaTargetingUtility.FindLowestHpAllies(caster, targetCount);
            if (healTargets.Count == 0) return false;

            float healAmount = caster.Stats.CurrentAtt * healMultiplier * caster.Stats.SkillDamageMultiplier;

            foreach (UnitController target in healTargets)
            {
                target.Stats.ApplyHeal(healAmount);
                Debug.Log($"[Heal] {caster.Stats.UnitData.unitName} → {target.Stats.UnitData.unitName} (+{healAmount} HP)");
            }
            return true;
        }
        finally
        {
            if (useRootMotion) caster.Animator.ResetApplyRootMotion();
        }
    }

    private async UniTaskVoid ReturnVfxDelayed(GameObject prefab, GameObject instance, float delay, CancellationToken ct)
    {
        try
        {
            await UniTask.WaitForSeconds(delay, cancellationToken: ct);
        }
        catch (System.OperationCanceledException) { }
        finally
        {
            if (instance != null)
                VfxPoolManager.Instance.Return(prefab, instance);
        }
    }
}
