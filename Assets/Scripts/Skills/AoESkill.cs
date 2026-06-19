using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;
using Unity.VisualScripting;
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

    [Header("VFX")]
    [Tooltip("Scale applied to castVfxPrefab to match indicator size")]
    public Vector3 vfxScale = Vector3.one;
    public float vfxDelay = 0;

    [Header("Multi-Cast")]
    [Min(1)] public int castCount = 1;
    [Tooltip("Lock the area from the first cast and reuse it for all subsequent casts")]
    public bool lockArea = false;

    public override async UniTask<bool> Execute(UnitController caster, CancellationToken ct = default)
    {
        Vector3 lockedPivot = Vector3.zero;
        Vector3 lockedOrigin = Vector3.zero;
        bool pivotLocked = false;

        for (int i = 0; i < castCount; i++)
        {
            // Each cast waits for animation event or timer
            if (useAnimationEvent)
                await caster.Visuals.WaitForSkillEvent(ct);
            else
                await UniTask.WaitForSeconds(castTime, cancellationToken: ct);

            if (caster == null || caster.Stats.CurrentHp <= 0) return false;

            Vector3 origin;
            Vector3 pivot;

            if (lockArea && pivotLocked)
            {
                // Reuse locked area
                origin = lockedOrigin;
                pivot = lockedPivot;
            }
            else
            {
                // Calculate fresh pivot
                UnitController primaryTarget = caster.AI.FindClosestTarget();
                if (primaryTarget == null) break;
                caster.Movement.LookAtTargetSkill(primaryTarget.transform, ct).Forget();

                origin = caster.Visuals.FirePoint.position;
                pivot = (primaryTarget.Stats.CurrentHp > 0)
                    ? primaryTarget.Visuals.HitBox.position
                    : origin;

                if (lockArea)
                {
                    lockedOrigin = origin;
                    lockedPivot = pivot;
                    pivotLocked = true;
                }
            }

            // skill area indicator code //
            //var indicator = SkillAreaRenderer.Create(areaShape, origin, pivot, color);
            //indicator.ShowForDuration(0.3f).Forget();

            // Spawn VFX or indicator
            if (castVfxPrefab != null)
            {
                Vector3 dir = new Vector3(pivot.x - origin.x, 0f, pivot.z - origin.z);
                Quaternion vfxRot = dir.sqrMagnitude > 0.001f
                    ? Quaternion.LookRotation(dir)
                    : Quaternion.identity;

                // Circle: spawn at target base, others: spawn at fire point
                Vector3 vfxPos = (areaShape.shapeType == AreaShapeType.Circle)
                    ? new Vector3(pivot.x, 0.1f, pivot.z)
                    : caster.Visuals.FirePoint.transform.position;

                GameObject vfx = VfxPoolManager.Instance.Get(castVfxPrefab, vfxPos, vfxRot);
                vfx.transform.localScale = vfxScale;
                ReturnVfxDelayed(castVfxPrefab, vfx, 5f, ct).Forget();
            }
            else
            {   
                // skill area indicator code //
                var indicator = SkillAreaRenderer.Create(areaShape, origin, pivot, color);
                indicator.ShowForDuration(0.3f).Forget();
            }
            await UniTask.WaitForSeconds(vfxDelay, cancellationToken: ct);
            // Collect and damage targets
            List<UnitController> targets = AreaTargetingUtility.GetTargetsInArea(areaShape, caster, pivot);
            foreach (UnitController target in targets)
            {
                float damage = caster.Stats.CurrentAtt * damageMultiplier * caster.Stats.SkillDamageMultiplier;
                if (canCrit) damage = caster.Stats.ApplyCrit(damage, out _);
                target.TakeDamage(damage, caster);
                if (isStun) target.CCHandler.ApplyStun(stunDuration);
            }
            // Multi Cast Logic
            if (i + 1 == castCount) break;
            caster.Animator.PlaySkill();
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
