using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// Merges 3 identical player units into their upgrade. Counts copies across board and bench.
/// Runs only when a unit is added or sold. On merge, MergeVfxManager flies projectiles to the
/// target tile and the upgraded unit spawns when they land (reachTime); cascades to chain-merge.
/// </summary>
public class MergeManager : MonoBehaviour
{
    public static MergeManager Instance { get; private set; }

    [SerializeField] private UnitSpawner unitSpawner;

    [Tooltip("Delay between consecutive/cascading merges (0 = immediate)")]
    [SerializeField] private float mergeDelay = 0f;

    private const int MergeCount = 3; // copies required to merge

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    private void OnEnable()  { BattleManager.OnPreparationStart += CheckAllMerges; }
    private void OnDisable() { BattleManager.OnPreparationStart -= CheckAllMerges; }


    // Public API //

    /// <summary>Check for a merge of this unit type. Call after a unit is added or sold.</summary>
    public void CheckMerge(UnitData data)
    {
        if (data == null || data.upgradeUnit == null) return; // final tier never merges

        List<UnitController> copies = CollectPlayerCopies(data);
        if (copies.Count < MergeCount) return;

        Merge(data, copies);
        // Cascade runs after the upgrade actually spawns (see SpawnUpgrade), because the
        // spawn is deferred until the merge projectiles land.
    }

    /// <summary>True if buying one more copy of this unit would complete a merge (used for bench-full purchases).</summary>
    public bool WouldMergeOnAdd(UnitData data)
    {
        if (data == null || data.upgradeUnit == null) return false; // final tier never merges
        return CollectPlayerCopies(data).Count >= MergeCount - 1;
    }

    /// <summary>
    /// Complete a merge driven by a purchase whose new copy is folded in virtually (bench full).
    /// Consumes the owned copies into the upgrade; the purchased copy is conserved into it without ever
    /// being placed. State-identical to a normal 3-copy merge. Only call when WouldMergeOnAdd() is true.
    /// </summary>
    public void MergeFromPurchase(UnitData data)
    {
        if (data == null || data.upgradeUnit == null) return;

        List<UnitController> copies = CollectPlayerCopies(data);
        if (copies.Count < MergeCount - 1) return; // not actually a completing purchase

        Merge(data, copies); // consumes the owned copies; the purchased copy is the virtual third
    }

    /// <summary>Re-evaluate every owned unit type for merges. Called when the Preparation phase begins.</summary>
    public void CheckAllMerges()
    {
        // Snapshot distinct datas first — CheckMerge mutates the rosters as it consumes/spawns.
        var datas = new HashSet<UnitData>();
        foreach (UnitController u in UnitManager.Instance.playerUnits)
            if (u != null && u.Stats.UnitData != null) datas.Add(u.Stats.UnitData);
        foreach (UnitController u in BenchManager.Instance.benchUnits)
            if (u != null && u.Stats.UnitData != null) datas.Add(u.Stats.UnitData);

        // Stagger each type so multiple prep-start merges don't all pop at once
        int i = 0;
        foreach (UnitData d in datas)
        {
            CheckMergeDelayed(d, mergeDelay * i).Forget();
            i++;
        }
    }

    /// <summary>Run CheckMerge after a delay so consecutive/cascading merges are spaced out.</summary>
    private async UniTaskVoid CheckMergeDelayed(UnitData data, float delay)
    {
        if (delay > 0f)
        {
            try { await UniTask.WaitForSeconds(delay, cancellationToken: this.GetCancellationTokenOnDestroy()); }
            catch (System.OperationCanceledException) { return; }
        }
        CheckMerge(data);
    }


    // Merge //

    /// <summary>Consume three copies, fly merge projectiles, and spawn the upgrade when they land.</summary>
    private void Merge(UnitData data, List<UnitController> copies)
    {
        // Prefer a fielded copy as the anchor so a board unit stays on the board
        UnitController anchor = copies.Find(u => !u.IsOnBench);
        if (anchor == null) anchor = copies[0];

        bool onField = !anchor.IsOnBench;
        TileScript      hexTile   = anchor.CurrentHexTile;
        BenchTileScript benchTile = onField ? null : anchor.CurrentTile as BenchTileScript;
        BaseTile targetTile = onField ? (BaseTile)hexTile : benchTile;
        Vector3  targetPos  = HitboxPos(anchor); // converge on the anchor's hitbox (where the upgrade appears)

        // Pick exactly three copies (anchor first) and capture their positions before destroying
        var toConsume = new List<UnitController> { anchor };
        foreach (UnitController u in copies)
        {
            if (toConsume.Count >= MergeCount) break;
            if (u == anchor) continue;
            toConsume.Add(u);
        }
        var sources = new List<Vector3>(toConsume.Count);
        foreach (UnitController u in toConsume) sources.Add(HitboxPos(u));

        // Reserve the target tile so nothing fills it during the projectile flight
        if (targetTile != null) targetTile.IsOccupied = true;

        // Consume the three (keep the anchor tile reserved, free the others)
        foreach (UnitController u in toConsume)
            ConsumeCopy(u, freeTile: u != anchor);

        // Spawn the upgrade when the projectiles land (or immediately if no VFX manager)
        void SpawnOnReach() => SpawnUpgrade(data, targetTile, onField, benchTile);

        if (MergeVfxManager.Instance != null)
            MergeVfxManager.Instance.PlayMerge(sources, targetPos, data.upgradeUnit.starLevel, SpawnOnReach);
        else
            SpawnOnReach();
    }

    /// <summary>Spawn the upgraded unit on the reserved tile, then cascade for a chain merge.</summary>
    private void SpawnUpgrade(UnitData data, BaseTile targetTile, bool onField, BenchTileScript benchTile)
    {
        if (targetTile != null) targetTile.IsOccupied = false; // free the reservation right before spawning (no await here)

        UnitController upgraded = unitSpawner.SpawnUnit(data.upgradeUnit, targetTile, Team.Player, onField);
        if (upgraded == null)
        {
            Debug.LogError($"[Merge] Failed to spawn {data.upgradeUnit.unitName}");
            return;
        }
        if (!onField) BenchManager.Instance.AddUnit(upgraded, benchTile);

        Debug.Log($"[Merge] 3x {data.unitName} -> {data.upgradeUnit.unitName} (star {data.upgradeUnit.starLevel})");
        CheckMergeDelayed(data.upgradeUnit, mergeDelay).Forget(); // cascade after mergeDelay so chained merges are spaced out
    }

    /// <summary>Remove a unit from its roster, optionally free its tile, and destroy it. Pool counts are conserved (not returned).</summary>
    private void ConsumeCopy(UnitController unit, bool freeTile)
    {
        if (unit.IsOnBench) BenchManager.Instance.RemoveUnit(unit);
        else                UnitManager.Instance.RemoveUnit(unit, unit.CurrentTeam);

        if (freeTile)
        {
            BaseTile tile = unit.CurrentTile;
            if (tile != null) tile.IsOccupied = false;
        }

        unit.gameObject.SetActive(false); // vanish immediately as the projectiles launch
        Destroy(unit.gameObject);
    }


    // Collection //

    /// <summary>Player-owned copies whose data matches. Board is excluded during Battle (field units are locked).</summary>
    private List<UnitController> CollectPlayerCopies(UnitData data)
    {
        var result = new List<UnitController>();

        // During Battle, field units are locked in combat — merge from the bench only.
        if (!IsBattlePhase())
            foreach (UnitController u in UnitManager.Instance.playerUnits)
                if (u != null && u.Stats.UnitData == data) result.Add(u);

        foreach (UnitController u in BenchManager.Instance.benchUnits)
            if (u != null && u.Stats.UnitData == data && !result.Contains(u)) result.Add(u);

        return result;
    }

    private static bool IsBattlePhase()
        => BattleManager.Instance != null && BattleManager.Instance.CurrentPhase == BattleManager.Phase.Battle;

    /// <summary>Unit's hitbox world position, falling back to its transform if no hitbox is assigned.</summary>
    private static Vector3 HitboxPos(UnitController unit)
        => unit.Visuals != null && unit.Visuals.HitBox != null ? unit.Visuals.HitBox.position : unit.transform.position;
}
