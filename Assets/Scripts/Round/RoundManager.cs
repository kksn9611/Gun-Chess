using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Manages round progression.
/// Preparation -> Battle -> Result -> Preparation loop.
/// Saves player unit positions before battle, restores after.
/// </summary>
public class RoundManager : MonoBehaviour
{
    [Header("Stage Data")]
    [SerializeField] private StageData[] stages;

    [Header("Player Initial Placement")]
    [SerializeField] private PlayerSpawnInfo[] playerSpawns;

    [Header("Settings")]
    [SerializeField] private int currentRound = 0;
    private float resultPhaseDuration = 5f;
    private int previousRound;

    [Header("Debug")]
    [Tooltip("Target round for the 'Force Set Round' context-menu action")]
    [SerializeField] private int debugRound = 1;
    private bool forcedRoundPending; // set by OnValidate (Play mode), applied safely in Update


    [Header("References")]
    [SerializeField] private UnitSpawner unitSpawner;
    [SerializeField] private SynergyManager synergyManager;
    [SerializeField] private ShopManager shopManager;

    private CancellationTokenSource cts;

    public int CurrentRound => currentRound;

    /// <summary>StageData for the current round, or null if out of range.</summary>
    public StageData CurrentStage =>
        (stages != null && currentRound >= 1 && currentRound <= stages.Length) ? stages[currentRound - 1] : null;

    /// <summary>Saved field unit positions before battle starts</summary>
    private readonly Dictionary<UnitController, TileScript> savedFieldPositions = new();

    /// <summary>Enemy units spawned for current round preview</summary>
    private readonly List<UnitController> previewEnemies = new();

    // Inspector: changing currentRound requests a forced transition.
    // Edit mode does nothing (tiles/managers don't exist yet); Play mode defers to Update so we never
    // Instantiate/Destroy from within OnValidate (which Unity may call mid-serialization).
    private void OnValidate()
    {
        if (currentRound == previousRound) return;
        previousRound = currentRound;
        if (Application.isPlaying) forcedRoundPending = true;
    }

    private void Update()
    {
        if (!forcedRoundPending) return;
        forcedRoundPending = false;
        ForceSetRound(currentRound);
    }
    private void OnEnable()
    {
        cts = new CancellationTokenSource();
        BattleManager.OnBattleEnd += OnBattleEnd;
    }

    private void OnDisable()
    {
        BattleManager.OnBattleEnd -= OnBattleEnd;
        cts?.Cancel();
        cts?.Dispose();
        cts = null;
    }

    private async UniTaskVoid Start()
    {
        // Wait for HexGridLayout / BenchLayout to create tiles
        await UniTask.WaitForSeconds(0.5f, cancellationToken: cts.Token);

        // Initial player unit placement
        SpawnPlayerUnitsForTest();

        // Start round 1 preparation -- preview enemy units
        currentRound = 1;
        previousRound = currentRound;
        SpawnEnemiesForPreview(stages[currentRound - 1]);
        Debug.Log($"[RoundManager] === Round {currentRound} Preparation Phase ===");
    }

    /// <summary>
    /// Bound to Space key via Input System -> Invoke Unity Events.
    /// Starts battle during Preparation phase.
    /// </summary>
    public void OnStartBattle(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        BeginBattle();
    }
    /// <summary>
    /// Start battle for the current round.
    /// </summary>
    public void BeginBattle()
    {
        if (BattleManager.Instance == null) return;
        if (BattleManager.Instance.CurrentPhase != BattleManager.Phase.Preparation) return;

        if (currentRound < 1 || currentRound > stages.Length)
        {
            Debug.LogWarning($"[RoundManager] Invalid round: {currentRound} (max {stages.Length})");
            return;
        }

        // Block start while over the board limit (e.g. auto-bench couldn't fit the excess)
        if (BoardManager.Instance != null && BoardManager.Instance.FieldCount > BoardManager.Instance.Capacity)
        {
            Debug.LogWarning($"[RoundManager] Over board capacity: {BoardManager.Instance.FieldCount}/{BoardManager.Instance.Capacity}");
            return;
        }

        // 1) Save player unit positions (field units only)
        SavePlayerUnitPositions();

        // 2) Register preview enemies with UnitManager
        RegisterPreviewEnemies();

        // 3) Start battle
        BattleManager.Instance.StartBattle();
        Debug.Log($"[RoundManager] === Round {currentRound} Battle Start ===");
    }

    // Battle End //

    private void OnBattleEnd(Team winner)
    {
        HandleBattleResultAsync(winner).Forget();
    }

    /// <summary>
    /// Handle battle result.
    /// Result phase wait -> clear enemies -> reset tiles -> restore players -> next round.
    /// </summary>
    private async UniTask HandleBattleResultAsync(Team winner)
    {
        Debug.Log($"[RoundManager] Round {currentRound} result: {winner} wins");

        // Result phase wait (allow death animations)
        await UniTask.WaitForSeconds(resultPhaseDuration, cancellationToken: cts.Token);

        // Clear enemy units
        ClearEnemyUnits();

        // Trim pools: remove unused VFX pools, shrink trail pools
        TrailPoolManager.Instance.Trim();
        VfxPoolManager.Instance.Trim();

        // Reset field tile occupancy (bench untouched)
        TileManager.Instance.ClearAllOccupied();

        // Restore player units to pre-battle positions
        RestorePlayerPositions();

        // Gain interest gold each round (calculated on held gold before this round's income)
        PlayerManager.Instance.GrantInterest();

        // Gain base turn gold — after interest so it doesn't inflate the interest calculation
        PlayerManager.Instance.GrantTurnGold();

        // Gain Synergy gold each round
        synergyManager.GrantRoundIncome();

        // Gain base EXP each round (auto level-up when the threshold is met)
        PlayerManager.Instance.GrantRoundExp();

        // Reroll the shop for the new round (uses the post-level-up level); skipped while locked
        if (shopManager != null)
        {
            shopManager.RefreshForNewRound();
            shopManager.AddFreeReroll(PlayerManager.Instance.FreeRerollsPerRound); // n free rerolls this round
        }

        // Advance to next round
        currentRound++;

        if (currentRound > stages.Length)
        {
            Debug.Log("[RoundManager] === All Stages Cleared! ===");
            await UniTask.WaitForSeconds(resultPhaseDuration, cancellationToken: cts.Token);
            BattleManager.Instance.ResetBattle();
            return;
        }

        // Preview-spawn enemies for next round
        SpawnEnemiesForPreview(stages[currentRound - 1]);

        BattleManager.Instance.ResetBattle();
        Debug.Log($"[RoundManager] === Round {currentRound} Preparation Phase ===");
    }


    // Debug / Forced Round Transition //

    /// <summary>Editor context-menu: force the round to <c>debugRound</c> (Play mode only).</summary>
    [ContextMenu("Debug: Force Set Round")]
    private void DebugForceSetRound() => ForceSetRound(debugRound);

    /// <summary>
    /// Safely jump to a round from any state (debug). Cancels any in-flight transition, tears down all
    /// enemies, returns player units to their saved tiles, forces the Preparation phase, and previews the
    /// new round. Defensive: no-ops with a warning if prerequisites aren't met, and can't throw.
    /// </summary>
    public void ForceSetRound(int round)
    {
        // Play-mode only: tiles, singletons, and unit prefabs aren't live in Edit mode.
        if (!Application.isPlaying)
        {
            Debug.LogWarning("[RoundManager] ForceSetRound is Play-mode only.");
            return;
        }
        if (stages == null || stages.Length == 0)
        {
            Debug.LogWarning("[RoundManager] No stages configured — cannot force round.");
            return;
        }
        if (BattleManager.Instance == null || TileManager.Instance == null
            || UnitManager.Instance == null || unitSpawner == null)
        {
            Debug.LogWarning("[RoundManager] Core managers/spawner not ready — cannot force round.");
            return;
        }

        int target = Mathf.Clamp(round, 1, stages.Length);
        if (target != round)
            Debug.LogWarning($"[RoundManager] Round {round} out of range [1,{stages.Length}] — clamped to {target}.");

        StageData stage = stages[target - 1];
        if (stage == null)
        {
            Debug.LogWarning($"[RoundManager] StageData for round {target} is null — aborting.");
            return;
        }

        // 1) Kill any in-flight result transition so it can't mutate state after this reset.
        ResetCts();

        // 2) A battle is "in progress" if we're past Preparation or still hold saved field positions.
        bool battleInProgress = BattleManager.Instance.CurrentPhase != BattleManager.Phase.Preparation
                                || savedFieldPositions.Count > 0;

        // 3) Tear down every enemy — preview (releases their tiles) and any live/registered ones.
        ClearPreviewEnemies();
        ClearEnemyUnits();

        // 4) Mid-battle: reset field occupancy and bring player units home (also resets their AI/stats).
        if (battleInProgress)
        {
            TileManager.Instance.ClearAllOccupied(); // field only; bench untouched
            if (savedFieldPositions.Count > 0) RestorePlayerPositions();
        }
        savedFieldPositions.Clear(); // never leave a stale save behind

        // 5) Force the Preparation phase (idempotent — skip if already there to avoid re-firing events).
        if (BattleManager.Instance.CurrentPhase != BattleManager.Phase.Preparation)
            BattleManager.Instance.ResetBattle();

        // 6) Commit the round and preview its enemies.
        currentRound  = target;
        previousRound = target;
        SpawnEnemiesForPreview(stage);

        Debug.Log($"[RoundManager] Forced round -> {target} (Preparation).");
    }

    /// <summary>Cancel and replace the transition CancellationTokenSource so a stale token can't fire.</summary>
    private void ResetCts()
    {
        cts?.Cancel();
        cts?.Dispose();
        cts = new CancellationTokenSource();
    }


    /// <summary>
    /// Spawn player units on bench from Inspector settings.
    /// Assigns empty bench slots in order; warns if no slots available.
    /// </summary>
    private void SpawnPlayerUnitsForTest()
    {
        foreach (var spawn in playerSpawns)
        {
            // Take a copy from the shared pool before placing
            if (!UnitPool.Instance.TryAcquire(spawn.unitData))
            {
                Debug.LogWarning($"[RoundManager] Pool empty — failed to place {spawn.unitData.unitName}");
                continue;
            }

            BenchTileScript slot = BenchManager.Instance.GetEmptySlot();
            if (slot == null)
            {
                Debug.LogWarning($"[RoundManager] No bench slot — failed to place {spawn.unitData.unitName}");
                UnitPool.Instance.Return(spawn.unitData); // refund the unplaced copy
                continue;
            }

            UnitController unit = unitSpawner.SpawnUnit(spawn.unitData, slot, Team.Player, register: false);
            if (unit != null)
                BenchManager.Instance.AddUnit(unit, slot);
        }
    }

    /// <summary>
    /// Preview-spawn enemy units during preparation phase.
    /// </summary>
    private void SpawnEnemiesForPreview(StageData stage)
    {
        // Destroy existing preview enemies and clear the list
        ClearPreviewEnemies();

        foreach (var enemy in stage.enemies)
        {
            TileScript tile = TileManager.Instance.GetTile(enemy.spawnCoordinate);
            if (tile != null)
            {
                UnitController unit = unitSpawner.SpawnUnit(enemy.unitData, tile, Team.Enemy, register: false);
                if (unit != null)
                {
                    ApplyEnemyBuffs(unit, stage); // per-stage stat modifiers (fresh each round, no revert needed)
                    previewEnemies.Add(unit);
                }
            }
            else
            {
                Debug.LogWarning($"[RoundManager] Enemy spawn tile ({enemy.spawnCoordinate}) not found");
            }
        }
    }

    /// <summary>Apply the stage's percent stat modifiers to a freshly spawned enemy.</summary>
    private void ApplyEnemyBuffs(UnitController unit, StageData stage)
    {
        if (stage.enemyBuffs == null || unit.Stats == null) return;
        foreach (StatBoostEntry b in stage.enemyBuffs)
            unit.Stats.ApplyStatModifier(b.statType, b.percentBoost);
    }

    /// <summary>
    /// Register preview-spawned enemies with UnitManager for combat.
    /// Called at battle start.
    /// </summary>
    private void RegisterPreviewEnemies()
    {
        foreach (var unit in previewEnemies)
        {
            if (unit != null)
                UnitManager.Instance.AddUnit(unit, Team.Enemy);
        }
        previewEnemies.Clear();
    }

    /// <summary>Save current positions of field player units before battle.</summary>
    private void SavePlayerUnitPositions()
    {
        savedFieldPositions.Clear();

        foreach (var unit in UnitManager.Instance.playerUnits)
        {
            if (unit != null && !unit.IsOnBench && unit.CurrentTile is TileScript hexTile)
                savedFieldPositions[unit] = hexTile;
        }

        Debug.Log($"[RoundManager] Positions saved — {savedFieldPositions.Count} field units");
    }

    /// <summary>
    /// Restore player field units after battle. Snapshot units go back to their saved tiles; any other
    /// live player field unit (e.g. a merge upgrade created mid-round) is reclaimed rather than orphaned.
    /// Reconciles against the live scene, not just the pre-battle snapshot.
    /// </summary>
    private void RestorePlayerPositions()
    {
        // Clear player unit roster (rebuilt below)
        UnitManager.Instance.ClearTeam(Team.Player);

        // 1) Restore pre-battle snapshot units to their saved tiles.
        var restored = new HashSet<UnitController>();
        foreach (var pair in savedFieldPositions)
        {
            UnitController unit = pair.Key;
            if (unit == null) continue;
            RestoreFieldUnit(unit, pair.Value);
            restored.Add(unit);
        }

        // 2) Reconcile against the live scene. Any active player field unit missing from the snapshot
        //    was created after SavePlayerUnitPositions (merge upgrade, deferred/cascaded spawn) — without
        //    this it would be left on the board in no roster, on an unoccupied tile, untargetable and
        //    unselectable. Reclaim it so it stays usable.
        int reclaimed = 0;
        foreach (UnitController unit in Object.FindObjectsByType<UnitController>(FindObjectsSortMode.None))
        {
            if (unit == null || restored.Contains(unit)) continue;
            if (unit.CurrentTeam != Team.Player || unit.IsOnBench) continue;
            ReclaimOrphan(unit);
            reclaimed++;
        }

        Debug.Log($"[RoundManager] Player restoration: {restored.Count} restored, {reclaimed} reclaimed");
    }

    /// <summary>Reactivate, reset, register, and place a player unit on a hex tile.</summary>
    private void RestoreFieldUnit(UnitController unit, TileScript tile)
    {
        unit.gameObject.SetActive(true);       // reactivate units deactivated by death
        unit.ResetForNewRound();               // reset stats/state (removes synergy buffs)
        UnitManager.Instance.AddUnit(unit, Team.Player); // register first (Recalculate iterates playerUnits)
        unit.PlaceOnTile(tile);                // OnBenchState -> synergy recalc
    }

    /// <summary>
    /// Reclaim a player field unit that isn't in the snapshot: keep it on its current tile if free,
    /// otherwise move it to a bench slot; as a last resort register it in place so it stays targetable.
    /// </summary>
    private void ReclaimOrphan(UnitController unit)
    {
        unit.gameObject.SetActive(true);
        unit.ResetForNewRound();

        TileScript tile = unit.CurrentHexTile;

        if (tile != null && !tile.IsOccupied)
        {
            RestoreFieldUnit(unit, tile);
            Debug.LogWarning($"[RoundManager] Reclaimed orphaned unit '{unit.name}' at {tile.GetCoordinate()}");
            return;
        }

        BenchTileScript slot = BenchManager.Instance.GetEmptySlot();
        if (slot != null)
        {
            BenchManager.Instance.AddUnit(unit, slot);
            unit.PlaceOnBench(slot);
            Debug.LogWarning($"[RoundManager] Reclaimed orphaned unit '{unit.name}' to bench (tile unavailable)");
            return;
        }

        if (tile != null)
        {
            UnitManager.Instance.AddUnit(unit, Team.Player);
            unit.PlaceOnTile(tile); // contested tile, but at least registered/targetable
            Debug.LogWarning($"[RoundManager] Reclaimed orphaned unit '{unit.name}' onto a contested tile (bench full)");
        }
        else
        {
            Debug.LogWarning($"[RoundManager] Could not reclaim orphaned unit '{unit.name}' (no tile/bench)");
        }
    }


    /// <summary>
    /// Destroy all preview units and clear the list.
    /// Used on SpawnEnemiesForPreview() re-call or round transition.
    /// </summary>
    private void ClearPreviewEnemies()
    {
        foreach (var enemy in previewEnemies)
        {
            if (enemy != null && enemy.gameObject != null)
            {
                // Release occupied tile
                if (enemy.CurrentTile != null)
                    enemy.CurrentTile.IsOccupied = false;
                Destroy(enemy.gameObject);
            }
        }
        previewEnemies.Clear();
    }

    /// <summary>
    /// Destroy all remaining enemy units and clear the roster.
    /// </summary>
    private void ClearEnemyUnits()
    {
        // Copy list before iterating (Destroy modifies the list)
        var remaining = new List<UnitController>(UnitManager.Instance.enemyUnits);
        foreach (var enemy in remaining)
        {
            if (enemy != null && enemy.gameObject != null)
            {
                // OnDestroy on each component cancels their CTS
                Destroy(enemy.gameObject);
            }
        }

        // Batch-clear enemy unit roster
        UnitManager.Instance.ClearTeam(Team.Enemy);
    }
}

[System.Serializable]
public class PlayerSpawnInfo
{
    public UnitData unitData;
    //public Vector2Int spawnCoordinate;
}
