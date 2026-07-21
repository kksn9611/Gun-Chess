using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>Per-tick effect a GroundZone applies to units inside it.</summary>
public enum ZoneEffect { Damage, Heal }

/// <summary>
/// Persistent ground effect (pool). Periodically damages enemies (or heals allies) standing
/// inside a circle. Owns its own lifecycle independent of the casting skill; cleans up on round end.
/// </summary>
public class GroundZone : MonoBehaviour
{
    private UnitController source;   // caster; may be destroyed (null-checked before use)
    private Team targetTeam;         // caster's team (enemies-of for damage, allies-of for heal)
    private Vector3 center;
    private float radius;
    private float amountPerTick;     // damage or heal per tick
    private bool applyCrit;
    private ZoneEffect effect;

    private SkillAreaRenderer indicator; // fallback visual when no VFX prefab
    private GameObject vfxPrefab;        // pooled VFX prefab (kept for return)
    private GameObject vfxInstance;      // active pooled VFX
    private CancellationTokenSource cts;

    // Factory //

    /// <summary>
    /// Spawn a pool at center. Applies effect to units inside every tickInterval for duration.
    /// Damage hits enemies of the caster's team; Heal affects allies of the caster's team.
    /// </summary>
    public static GroundZone Create(UnitController source, Vector3 center, float radius,
        float amountPerTick, float duration, float tickInterval, bool applyCrit, Color color,
        GameObject vfxPrefab = null, Vector3 vfxScale = default, ZoneEffect effect = ZoneEffect.Damage)
    {
        GameObject go = new GameObject(effect == ZoneEffect.Heal ? "GroundZone_HoT" : "GroundZone_DoT");
        GroundZone zone = go.AddComponent<GroundZone>();
        zone.Init(source, center, radius, amountPerTick, duration, tickInterval, applyCrit, color, vfxPrefab, vfxScale, effect);
        return zone;
    }

    private void Init(UnitController source, Vector3 center, float radius,
        float amountPerTick, float duration, float tickInterval, bool applyCrit, Color color,
        GameObject vfxPrefab, Vector3 vfxScale, ZoneEffect effect)
    {
        this.source        = source;
        this.targetTeam    = source.CurrentTeam;
        this.center        = center;
        this.radius        = radius;
        this.amountPerTick = amountPerTick;
        this.applyCrit     = applyCrit;
        this.vfxPrefab     = vfxPrefab;
        this.effect        = effect;

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

    /// <summary>Apply one effect tick every tickInterval until duration elapses.</summary>
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

    /// <summary>Apply the effect to every valid unit currently inside the pool.</summary>
    private void ApplyTick()
    {
        if (effect == ZoneEffect.Heal)
        {
            var allies = AreaTargetingUtility.GetAlliesInCircle(center, radius, targetTeam);
            foreach (UnitController ally in allies)
                ally.Stats.ApplyHeal(amountPerTick);
        }
        else
        {
            var targets = AreaTargetingUtility.GetTargetsInCircle(center, radius, targetTeam);
            foreach (UnitController target in targets)
            {
                float damage = amountPerTick;
                if (applyCrit && source != null) damage = source.Stats.ApplyCrit(damage, out _);
                target.TakeDamage(damage, source);
                if (source != null) source.RaiseSkillHit(target, damage);
            }
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
