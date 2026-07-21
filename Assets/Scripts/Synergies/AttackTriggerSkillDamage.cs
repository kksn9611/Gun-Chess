using UnityEngine;
using Cysharp.Threading.Tasks;

/// <summary>
/// Chance-based bonus damage that triggers on skill hits (not basic attacks).
/// Requires triggerOnSkill = true on the asset. Spawns a hit VFX at the target.
/// </summary>
[CreateAssetMenu(fileName = "AttackTriggerSkillDamage", menuName = "Scriptable Objects/Synergy/EventTriggerSynergy/AttackTriggerSkillDamage")]
public class AttackTriggerSkillDamage : EventTriggerBehavior
{
    [Header("Damage Setting")]
    [Range(0f, 1f)]
    [Tooltip("0 = 0%, 1 = 100%")]
    public float damageChance = 0.1f; // 10%
    public float damage;

    [Header("VFX")]
    public GameObject hitVfxPrefab;
    public Vector3 vfxScale = Vector3.one;
    public float vfxLifetime = 2f;

    protected override void ExecuteSkillEffect(UnitController caster, UnitController target)
    {
        if (target == null || target.Stats.CurrentHp <= 0) return;

        if (Random.value < damageChance)
        {
            // Capture the hit point before damage — the target may die/deactivate.
            Vector3 hitPos = target.Visuals.HitBox.position;

            target.TakeDamage(damage, caster);
            SpawnHitVfx(hitPos);
        }
    }

    // Pooled one-shot VFX at the impact point //
    private void SpawnHitVfx(Vector3 pos)
    {
        if (hitVfxPrefab == null) return;
        GameObject vfx = VfxPoolManager.Instance.Get(hitVfxPrefab, pos, Quaternion.identity);
        vfx.transform.localScale = vfxScale;
        ReturnVfxDelayed(hitVfxPrefab, vfx, vfxLifetime).Forget();
    }

    private async UniTaskVoid ReturnVfxDelayed(GameObject prefab, GameObject instance, float delay)
    {
        try { await UniTask.WaitForSeconds(delay); }
        catch (System.OperationCanceledException) { }
        finally { if (instance != null) VfxPoolManager.Instance.Return(prefab, instance); }
    }
}
