using System;
using System.Collections;
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
public class UnitController : MonoBehaviour
{
    // Placement //

    [Header("Placement")]
    [SerializeField] private TileScript currentTile; // Currently occupied hex tile
    [SerializeField] private BenchTileScript currentBenchTile; // Bench tile; null = on field
    [SerializeField] private Vector2Int currentCoord; // Current tile coordinate
    [SerializeField] private Team currentTeam;
    private UnitVisuals unitVisuals;
    // Component Cache //

    public UnitAI AI { get; private set; }
    public UnitStats Stats { get; private set; }
    public UnitAnimator Animator { get; private set; }
    public UnitMovement Movement { get; private set; }
    public UnitVisuals Visuals { get; private set; }
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
        if(TryGetComponent<UnitVisuals>(out unitVisuals))
        {
            Visuals = unitVisuals;
        }
    }

    /// <summary>
    /// Called by UnitSpawner.SpawnUnit().
    /// Initialize stats → occupy tile → fire event.
    /// </summary>
    public void Initialize(UnitData data, BaseTile spawnTile, Team team)
    {
        Stats.Initialize(data);
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
        Debug.Log($"{data.unitName} @ {currentCoord} ({currentTeam})");
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
        StartCoroutine(MoveToTileSmoothly(newTile.transform.position, clearCurrent ? 0.1f : 0.2f));
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
        StartCoroutine(MoveToTileSmoothly(slot.transform.position, 0.2f));
    }

    /// <summary>Smooth movement during placement.</summary>
    private IEnumerator MoveToTileSmoothly(Vector3 targetPosition, float duration)
    {
        float elapsed = 0f;
        Vector3 startPosition = transform.position;

        while (elapsed < duration)
        {
            transform.position = Vector3.Lerp(startPosition, targetPosition, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
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
        float att = Stats.CurrentAtt;
        target.TakeDamage(att);
        OnAttackHit?.Invoke(this, target, att);
        Stats.GainMp(Stats.MpGainOnAttack);
    }

    /// <summary>Skill cast coroutine. Called via yield return from UnitAI.</summary>
    public IEnumerator CastSkillCoroutine()
    {
        OnBeforeSkillCast?.Invoke();
        Debug.Log($"[Skill] {Stats.UnitData.unitName} → casting {Stats.Skill.skillName}!");

        yield return StartCoroutine(Stats.Skill.Execute(this));

        Stats.SetMp(0f);
        Debug.Log($"[Skill] {Stats.UnitData.unitName} → {Stats.Skill.skillName} complete, MP reset");
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
        StartCoroutine(DestroyAfterDelay(2f));
    }

    /// <summary>Full state reset on round transition.</summary>
    public void ResetForNewRound()
    {
        AI.ResetState();
        Stats.ResetStats();
        transform.rotation = Quaternion.identity; // reset facing direction
    }

    /// <summary>After delay: player→deactivate, enemy→destroy.</summary>
    private IEnumerator DestroyAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (currentTeam == Team.Player)
            gameObject.SetActive(false);
        else
            Destroy(gameObject);
    }
}
