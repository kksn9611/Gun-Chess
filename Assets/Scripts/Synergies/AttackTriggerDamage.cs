using UnityEngine;
using Cysharp.Threading.Tasks;

[CreateAssetMenu(fileName = "AttackTriggerDamage", menuName = "Scriptable Objects/Synergy/EventTriggerSynergy/AttackTriggerDamage")]
public class AttackTriggerDamage : EventTriggerBehavior
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

    protected override void ExecuteAttackEffect(UnitController attacker, UnitController target)
    {
        if (target == null || target.Stats.CurrentHp <= 0) return;

        if (Random.value < damageChance)
        {
            // Capture the hit point before damage — the target may die/deactivate.
            Vector3 hitPos = target.Visuals.HitBox.position;

            target.TakeDamage(damage, attacker);
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
