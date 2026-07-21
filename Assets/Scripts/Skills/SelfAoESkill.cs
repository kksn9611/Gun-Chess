using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

/// <summary>
/// Self-centered AoE skill. Damages all enemies within a circle around the caster.
/// Uses the Circle area shape; the area always follows the caster's position.
/// </summary>
[CreateAssetMenu(fileName = "SelfAoESkill", menuName = "Scriptable Objects/Skill/SelfAoESkill")]
public class SelfAoESkill : BaseSkill
{
    [Header("Skill Settings")]
    [Tooltip("Damage multiplier relative to ATK")]
    public float damageMultiplier = 3f;
    [Tooltip("Stun Enable")]
    public bool isStun = false;
    public float stunDuration;

    [Tooltip("Circle radius around the caster")]
    public float radius = 2f;
    public Color color;

    [Header("VFX")]
    [Tooltip("Scale applied to castVfxPrefab to match the radius")]
    public Vector3 vfxScale = Vector3.one;
    public float vfxDelay = 0;

    [Header("Multi-Cast")]
    [Min(1)] public int castCount = 1;

    public override async UniTask<bool> Execute(UnitController caster, CancellationToken ct = default)
    {
        for (int i = 0; i < castCount; i++)
        {
            // Each cast waits for animation event or timer
            if (useAnimationEvent)
                await caster.Visuals.WaitForSkillEvent(ct);
            else
                await UniTask.WaitForSeconds(castTime, cancellationToken: ct);

            if (caster == null || caster.Stats.CurrentHp <= 0) return false;

            // Center the area on the caster
            Vector3 center = caster.transform.position;

            // Spawn VFX or indicator
            if (castVfxPrefab != null)
            {
                Vector3 vfxPos = new Vector3(center.x, 0.1f, center.z);
                GameObject vfx = VfxPoolManager.Instance.Get(castVfxPrefab, vfxPos, Quaternion.identity);
                vfx.transform.localScale = vfxScale;
                ReturnVfxDelayed(castVfxPrefab, vfx, 5f, ct).Forget();
            }
            else
            {
                // Circle indicator centered on the caster (origin == pivot)
                var shape = new AreaShapeData { shapeType = AreaShapeType.Circle, radius = radius };
                // Fall back to a visible default if no color was authored (alpha 0)
                Color indicatorColor = color.a > 0.01f ? color : new Color(1f, 0f, 0f, 0.4f);
                var indicator = SkillAreaRenderer.Create(shape, center, center, indicatorColor);
                indicator.ShowForDuration(0.3f).Forget();
            }

            await UniTask.WaitForSeconds(vfxDelay, cancellationToken: ct);

            // Collect and damage all enemies surrounding the caster
            List<UnitController> targets = AreaTargetingUtility.GetTargetsInCircle(center, radius, caster.CurrentTeam);
            foreach (UnitController target in targets)
            {
                float damage = caster.Stats.CurrentAtt * damageMultiplier * caster.Stats.SkillDamageMultiplier;
                if (canCrit) damage = caster.Stats.ApplyCrit(damage, out _);
                target.TakeDamage(damage, caster);
                caster.RaiseSkillHit(target, damage);
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
