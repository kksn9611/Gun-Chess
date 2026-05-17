using System;
using System.Collections;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// Central hub component for a unit.
/// Handles placement, combat actions, and death.
/// Stats/synergy via UnitStats, AI via UnitAI.
/// </summary>
[RequireComponent(typeof(UnitAI))]
[RequireComponent(typeof(UnitStats))]
[RequireComponent(typeof(UnitAnimator))]
[RequireComponent(typeof(UnitMovement))]
[RequireComponent(typeof(UnitVisuals))]
[RequireComponent(typeof(UnitCCHandler))]
public class UnitController : MonoBehaviour
{
    // Placement //

    [Header("Placement")]
    [SerializeField] private TileScript currentTile; // Currently occupied hex tile
    [SerializeField] private BenchTileScript currentBenchTile; // Bench tile; null = on field
    [SerializeField] private Vector2Int currentCoord; // Current tile coordinate
    [SerializeField] private Team currentTeam;
    private CancellationTokenSource cts;

    // Component Cache //

    public UnitAI AI { get; private set; }
    public UnitStats Stats { get; private set; }
    public UnitAnimator Animator { get; private set; }
    public UnitMovement Movement { get; private set; }
    public UnitVisuals Visuals { get; private set; }
    public UnitCCHandler CCHandler { get; private set; }
    // Events //

    public Transform uiAnchor;
    public static event Action<UnitController> OnUnitSpawned;

    /// <summary>Bench ↔ field transition (true = bench)</summary>
    public event Action<bool> OnBenchState;
    /// <summary>Attack hit (attacker, target, damage)</summary>
    public event Action<UnitController, UnitController, float> OnAttackHit;
    /// <summary>Before taking damage</summary>
    public event Action<float> OnBeforeTakeDamage;
    /// <summary>Before skill cast</summary>
    public event Action OnBeforeSkillCast;

    // Properties (read only) //

    public BaseTile    CurrentTile    => currentBenchTile != null ? (BaseTile)currentBenchTile : (BaseTile)currentTile;
    public Team        CurrentTeam    => currentTeam;
    public bool        IsOnBench      => currentBenchTile != null;
    public Vector2Int  CurrentCoord   => currentCoord;
    public TileScript  CurrentHexTile => currentTile;

    // Initialization //

    private void Awake()
    {
        AI    = GetComponent<UnitAI>();
        Stats = GetComponent<UnitStats>();
        Animator = GetComponent<UnitAnimator>();
        Movement = GetComponent<UnitMovement>();
        Visuals = GetComponent<UnitVisuals>();
        CCHandler = GetComponent<UnitCCHandler>();
        cts = new CancellationTokenSource();
    }

    private void OnDestroy()
    {
        cts?.Cancel();
        cts?.Dispose();
    }

    /// <summary>
    /// Called by UnitSpawner.SpawnUnit().
    /// Initialize stats → occupy tile → fire event.
    /// </summary>
    public void Initialize(UnitData data, BaseTile spawnTile, Team team)
    {
        Stats.Initialize(data);
        Visuals.Initialize(data);
        currentTeam  = team;
        currentCoord = spawnTile.GetCoordinate();
        spawnTile.IsOccupied = true;

        // Branch field/bench reference based on tile type
        if (spawnTile is BenchTileScript benchTile)
        {
            currentBenchTile = benchTile;
            currentTile      = null;
        }
        else if (spawnTile is TileScript hexTile)
        {
            currentTile      = hexTile;
            currentBenchTile = null;
        }

        OnUnitSpawned?.Invoke(this);

        if (!IsOnBench && BattleManager.Instance.CurrentPhase == BattleManager.Phase.Battle)
        {
            AI.EnterIdleState();
        }
    }

    // Placement //

    /// <summary>
    /// Place unit on a hex tile. (Preparation phase)
    /// clearCurrent=false: keep original IsOccupied during swap.
    /// </summary>
    public void PlaceOnTile(TileScript newTile, bool clearCurrent = true)
    {
        AI.ResetState();

        if (clearCurrent)
        {
            if (currentTile != null)      currentTile.IsOccupied = false;
            if (currentBenchTile != null) currentBenchTile.IsOccupied = false;
        }
        currentTile      = newTile;
        currentBenchTile = null;
        currentCoord     = newTile.GetCoordinate();
        newTile.IsOccupied = true;
        OnBenchState?.Invoke(false);
        MoveToTileSmoothly(newTile.transform.position, clearCurrent ? 0.1f : 0.2f).Forget();
    }

    /// <summary>
    /// Place unit on a bench slot.
    /// clearCurrent=false: keep original IsOccupied during swap.
    /// </summary>
    public void PlaceOnBench(BenchTileScript slot, bool clearCurrent = true)
    {
        AI.ResetState();

        if (clearCurrent)
        {
            if (currentTile != null)      currentTile.IsOccupied = false;
            if (currentBenchTile != null) currentBenchTile.IsOccupied = false;
        }
        currentTile      = null;
        currentBenchTile = slot;
        currentCoord     = slot.GetCoordinate();
        slot.IsOccupied  = true;
        OnBenchState?.Invoke(true);
        MoveToTileSmoothly(slot.transform.position, 0.2f).Forget();
    }

    /// <summary>Smooth movement during placement.</summary>
    private async UniTask MoveToTileSmoothly(Vector3 targetPosition, float duration)
    {
        float elapsed = 0f;
        Vector3 startPosition = transform.position;

        while (elapsed < duration)
        {
            transform.position = Vector3.Lerp(startPosition, targetPosition, elapsed / duration);
            elapsed += Time.deltaTime;
            await UniTask.Yield(cts.Token);
        }
        transform.position = targetPosition;
    }

    /// <summary>Update logical tile/coordinate after UnitAI movement.</summary>
    public void SetCurrentTile(TileScript newTile)
    {
        currentTile  = newTile;
        currentCoord = newTile.GridCoordinate;
    }

    // Combat Actions //

    /// <summary>Attack target. Apply damage + gain MP + fire events.</summary>
    public void PerformAttack(UnitController target)
    {
        if (target == null || target.Stats.CurrentHp <= 0) return;
        Animator.PlayAttack();
        float att = Stats.ApplyCrit(Stats.CurrentAtt, out bool isCrit);

        // Passes a lambda expression to be executed when the last bullet hits.
        Visuals.FireWeaponEffect(target, () => {
            if (target != null && target.Stats.CurrentHp > 0)
            {
                target.TakeDamage(att);
                if (Stats.CurrentHp > 0)
                {
                    OnAttackHit?.Invoke(this, target, att);
                    Stats.GainMp(Stats.MpGainOnAttack);
                }
            }
        });
    }

    /// <summary>Skill cast. Called via yield return from UnitAI (bridged with ToCoroutine).</summary>
    public async UniTask CastSkillAsync()
    {
        OnBeforeSkillCast?.Invoke();
        Animator.SetSkillSpeed(Stats.Skill.animationSpd);
        Animator.PlaySkill();
        Debug.Log($"[Skill] {Stats.UnitData.unitName} → casting {Stats.Skill.skillName}!");

        bool fired = await Stats.Skill.Execute(this, cts.Token);

        if (fired)
        {
            Stats.SetMp(0f);
            Debug.Log($"[Skill] {Stats.UnitData.unitName} → {Stats.Skill.skillName} complete, MP reset");
        }
        else
        {
            Debug.Log($"[Skill] {Stats.UnitData.unitName} → {Stats.Skill.skillName} canceled, MP retained");
        }
    }

    // Stun, CC //
    public void OnStunApplied()
    {
        AI.EnterStunnedState();
    }
    public void OnStunEnded()
    {
        AI.EnterIdleState();
    }

    // Damage / Death //

    /// <summary>Calculate damage with defense → reduce HP → check death.</summary>
    public void TakeDamage(float damage)
    {
        if (AI.CurrentState == UnitState.Dead) return;

        OnBeforeTakeDamage?.Invoke(damage);

        float actualDamage = damage * (1f - Stats.CurrentDef / 100f);
        Stats.SetHp(Stats.CurrentHp - actualDamage);
        Stats.GainMp(Stats.MpGainOnHit);

        if (Stats.CurrentHp <= 0f)
            Die();
    }

    /// <summary>Handle unit death.</summary>
    public void Die()
    {
        cts?.Cancel();
        cts?.Dispose();
        cts = new CancellationTokenSource(); // reset for potential reuse (player units)
        StopAllCoroutines();
        AI.EnterDeadState();

        if (currentTile != null)
        {
            currentTile.IsOccupied = false;
            currentTile            = null;
        }
        if (currentBenchTile != null)
        {
            Debug.LogWarning("Die() called on bench tile! Needs investigation");
            currentBenchTile.IsOccupied = false;
            currentBenchTile            = null;
        }

        UnitManager.Instance.RemoveUnit(this, currentTeam);
        UnitManager.Instance.CheckBattleEnd();

        Debug.Log($"{gameObject} died");
        DestroyAfterDelay(2f).Forget();
    }

    /// <summary>Full state reset on round transition.</summary>
    public void ResetForNewRound()
    {
        AI.ResetState();
        Stats.ResetStats();
        transform.rotation = Quaternion.identity; // reset facing direction
    }

    /// <summary>After delay: player→deactivate, enemy→destroy.</summary>
    private async UniTaskVoid DestroyAfterDelay(float delay)
    {
        await UniTask.WaitForSeconds(delay, cancellationToken: cts.Token);
        if (currentTeam == Team.Player)
            gameObject.SetActive(false);
        else
            Destroy(gameObject);
    }
}
