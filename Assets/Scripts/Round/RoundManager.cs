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


    [Header("References")]
    [SerializeField] private UnitSpawner unitSpawner;

    private CancellationTokenSource cts;

    public int CurrentRound => currentRound;

    /// <summary>Saved field unit positions before battle starts</summary>
    private readonly Dictionary<UnitController, TileScript> savedFieldPositions = new();

    /// <summary>Enemy units spawned for current round preview</summary>
    private readonly List<UnitController> previewEnemies = new();

    private void OnValidate() // Editor: preview stage enemies by changing round
    {
        if (currentRound != previousRound && currentRound >= 1 && currentRound <= stages.Length)
        {
            SpawnEnemiesForPreview(stages[currentRound - 1]);
            previousRound = currentRound;
        }
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
        await UniTask.WaitForSeconds(0.3f, cancellationToken: cts.Token);

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


    /// <summary>
    /// Spawn player units on bench from Inspector settings.
    /// Assigns empty bench slots in order; warns if no slots available.
    /// </summary>
    private void SpawnPlayerUnitsForTest()
    {
        foreach (var spawn in playerSpawns)
        {
            BenchTileScript slot = BenchManager.Instance.GetEmptySlot();
            if (slot == null)
            {
                Debug.LogWarning($"[RoundManager] No bench slot — failed to place {spawn.unitData.unitName}");
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
                    previewEnemies.Add(unit);
            }
            else
            {
                Debug.LogWarning($"[RoundManager] Enemy spawn tile ({enemy.spawnCoordinate}) not found");
            }
        }
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
    /// Restore player units to saved positions.
    /// Reactivate dead units, reset stats, and re-place on original tiles.
    /// </summary>
    private void RestorePlayerPositions()
    {
        // Clear player unit roster
        UnitManager.Instance.ClearTeam(Team.Player);

        // Restore field units (synergy recalc runs once after full restore)
        foreach (var pair in savedFieldPositions)
        {
            UnitController unit = pair.Key;
            TileScript tile     = pair.Value;
            if (unit == null) continue;

            // Reactivate units deactivated by death
            unit.gameObject.SetActive(true);
            // Reset stats and state (including synergy buff removal)
            unit.ResetForNewRound();
            // Register with UnitManager first (Recalculate iterates playerUnits)
            UnitManager.Instance.AddUnit(unit, Team.Player);
            // Place on saved tile (OnBenchState -> Recalculate trigger)
            unit.PlaceOnTile(tile);
        }

        Debug.Log($"[RoundManager] Player unit restoration complete");
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
