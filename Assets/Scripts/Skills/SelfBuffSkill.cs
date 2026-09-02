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
        // Skip a cast that resolves after the fight is over (AI cancelled, or no longer in Battle phase):
        // applying a buff or spawning a lingering aura outside combat is meaningless and causes the
        // late-cast VFX-linger bug.
        if (ct.IsCancellationRequested) return false;
        if (BattleManager.Instance != null && BattleManager.Instance.CurrentPhase != BattleManager.Phase.Battle) return false;
        if (boosts == null || boosts.Length == 0) return false;

        // Cast VFX parented to the caster so it follows the unit //
        if (castVfxPrefab != null)
        {
            GameObject vfx = VfxPoolManager.Instance.Get(castVfxPrefab, caster.transform.position, Quaternion.identity);
            vfx.transform.SetParent(caster.transform, worldPositionStays: false);
            vfx.transform.localPosition = new Vector3(0f, 0.1f, 0f);
            vfx.transform.localRotation = Quaternion.identity;
            vfx.transform.localScale = vfxScale;
            ReturnVfxOnCancel(castVfxPrefab, vfx, ct).Forget();
        }

        // Apply stat boosts to self
        foreach (var entry in boosts)
        {
            caster.Stats.ApplyStatModifier(entry.statType, entry.percentBoost);
            Debug.Log($"[SelfBuff] {caster.Stats.UnitData.unitName} +{entry.percentBoost}% {entry.statType}");
        }
        return true;
    }

    // Keep the buff VFX alive until the cast token cancels (battle reset / death), then pool it.
    // Self-contained (no global event) and finally-guaranteed, so it can't miss cleanup no matter when
    // it spawned — unlike the old OnBattleEnd subscription, which was lost if it spawned after that event.
    private async UniTaskVoid ReturnVfxOnCancel(GameObject prefab, GameObject instance, CancellationToken ct)
    {
        try
        {
            await UniTask.WaitUntilCanceled(ct);
        }
        finally
        {
            if (instance != null)
                VfxPoolManager.Instance.Return(prefab, instance);
        }
    }
}
