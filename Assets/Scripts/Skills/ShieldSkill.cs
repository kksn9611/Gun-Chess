using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// Grants a damage-absorbing shield on cast. Shields the caster or the N lowest-HP% allies.
/// Shield amount = caster ATK * shieldMultiplier * SkillDamageMultiplier.
/// </summary>
[CreateAssetMenu(fileName = "ShieldSkill", menuName = "Scriptable Objects/Skill/ShieldSkill")]
public class ShieldSkill : BaseSkill
{
    public enum ShieldTargetMode { Self, LowestHpAllies } // who receives the shield

    [Header("Skill Settings")]
    public float skillSoundDelay = 0f;
    [Tooltip("Shield multiplier relative to ATK")]
    public float shieldMultiplier = 5f;
    [Tooltip("Who receives the shield")]
    public ShieldTargetMode targetMode = ShieldTargetMode.Self;
    [Tooltip("Number of allies to shield (LowestHpAllies mode, sorted by lowest HP%)")]
    [Min(1)] public int targetCount = 1;

    [Header("Cast VFX")]
    [Tooltip("Scale applied to castVfxPrefab")]
    public Vector3 vfxScale = Vector3.one;

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

        // Cast VFX on the caster //
        if (castVfxPrefab != null)
        {
            Vector3 pos = caster.transform.position;
            Vector3 vfxPos = new Vector3(pos.x, 0.1f, pos.z);
            GameObject vfx = VfxPoolManager.Instance.Get(castVfxPrefab, vfxPos, Quaternion.identity);
            vfx.transform.localScale = vfxScale;
            ReturnVfxDelayed(castVfxPrefab, vfx, 5f, ct).Forget();
        }

        float shieldAmount = caster.Stats.CurrentAtt * shieldMultiplier * caster.Stats.SkillDamageMultiplier;

        // Self shield
        if (targetMode == ShieldTargetMode.Self)
        {
            caster.Stats.ApplyShield(shieldAmount);
            Debug.Log($"[Shield] {caster.Stats.UnitData.unitName} +{shieldAmount} shield (self)");
            return true;
        }

        // Shield the N lowest-HP% allies (self-fallback built into the helper)
        List<UnitController> targets = AreaTargetingUtility.FindLowestHpAllies(caster, targetCount);
        foreach (UnitController target in targets)
        {
            target.Stats.ApplyShield(shieldAmount);
            Debug.Log($"[Shield] {caster.Stats.UnitData.unitName} → {target.Stats.UnitData.unitName} +{shieldAmount} shield");
        }
        return true;
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
