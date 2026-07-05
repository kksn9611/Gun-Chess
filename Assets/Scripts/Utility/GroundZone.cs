using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// Persistent ground effect (damage pool). Periodically damages enemies standing inside a circle.
/// Owns its own lifecycle independent of the casting skill and cleans up on round end.
/// </summary>
public class GroundZone : MonoBehaviour
{
    private UnitController source;   // caster; may be destroyed (null-checked before use)
    private Team targetTeam;         // team whose members take damage
    private Vector3 center;
    private float radius;
    private float damagePerTick;
    private bool applyCrit;

    private SkillAreaRenderer indicator; // fallback visual when no VFX prefab
    private GameObject vfxPrefab;        // pooled VFX prefab (kept for return)
    private GameObject vfxInstance;      // active pooled VFX
    private CancellationTokenSource cts;

    // Factory //

    /// <summary>
    /// Spawn a damage pool at center. Damages targetTeam's enemies every tickInterval for duration.
    /// </summary>
    public static GroundZone Create(UnitController source, Vector3 center, float radius,
        float damagePerTick, float duration, float tickInterval, bool applyCrit, Color color,
        GameObject vfxPrefab = null, Vector3 vfxScale = default)
    {
        GameObject go = new GameObject("GroundZone_DoT");
        GroundZone zone = go.AddComponent<GroundZone>();
        zone.Init(source, center, radius, damagePerTick, duration, tickInterval, applyCrit, color, vfxPrefab, vfxScale);
        return zone;
    }

    private void Init(UnitController source, Vector3 center, float radius,
        float damagePerTick, float duration, float tickInterval, bool applyCrit, Color color,
        GameObject vfxPrefab, Vector3 vfxScale)
    {
        this.source        = source;
        this.targetTeam    = source.CurrentTeam;
        this.center        = center;
        this.radius        = radius;
        this.damagePerTick = damagePerTick;
        this.applyCrit     = applyCrit;
        this.vfxPrefab     = vfxPrefab;

        transform.position = center;

        // Visual: pooled VFX if provided, otherwise the flat indicator quad
        if (vfxPrefab != null)
        {
            Vector3 vfxPos = new Vector3(center.x, 0.1f, center.z);
            vfxInstance = VfxPoolManager.Instance.Get(vfxPrefab, vfxPos, Quaternion.identity);
            vfxInstance.transform.localScale = vfxScale == default ? Vector3.one : vfxScale;
        }
        else
        {
            var shape = new AreaShapeData { shapeType = AreaShapeType.Circle, radius = radius };
            indicator = SkillAreaRenderer.Create(shape, center, center, color);
        }

        BattleManager.OnBattleEnd += OnBattleEnd;

        cts = new CancellationTokenSource();
        TickLoop(duration, tickInterval, cts.Token).Forget();
    }

    // Tick Loop //

    /// <summary>Apply one damage tick every tickInterval until duration elapses.</summary>
    private async UniTaskVoid TickLoop(float duration, float tickInterval, CancellationToken ct)
    {
        float elapsed = 0f;
        try
        {
            while (elapsed < duration)
            {
                await UniTask.WaitForSeconds(tickInterval, cancellationToken: ct);
                elapsed += tickInterval;
                ApplyTick();
            }
        }
        catch (System.OperationCanceledException) { }
        finally
        {
            if (this != null) Destroy(gameObject);
        }
    }

    /// <summary>Damage every enemy currently inside the pool.</summary>
    private void ApplyTick()
    {
        var targets = AreaTargetingUtility.GetTargetsInCircle(center, radius, targetTeam);
        foreach (UnitController target in targets)
        {
            float damage = damagePerTick;
            if (applyCrit && source != null) damage = source.Stats.ApplyCrit(damage, out _);
            target.TakeDamage(damage, source);
        }
    }

    // Cleanup //

    /// <summary>Cancel the pool when the round ends.</summary>
    private void OnBattleEnd(Team winner)
    {
        cts?.Cancel();
    }

    private void OnDestroy()
    {
        BattleManager.OnBattleEnd -= OnBattleEnd;

        cts?.Cancel();
        cts?.Dispose();
        cts = null;

        if (indicator != null) indicator.Hide();
        if (vfxInstance != null && vfxPrefab != null)
            VfxPoolManager.Instance.Return(vfxPrefab, vfxInstance);
    }
}
