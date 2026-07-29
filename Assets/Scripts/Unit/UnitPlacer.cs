using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Pick up player units with mouse and place on tiles.
/// </summary>
public class UnitPlacer : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private int    playerZoneMaxRow = 4;
    [SerializeField] private Camera mainCamera;
    [SerializeField] private float  dragHeightOffset;
    [SerializeField] private float  dragFollowSpeed;

    [Header("Visuals")]
    [SerializeField] private Material highlightMaterial;

    [Header("Placement Overlay")]
    [SerializeField] private Color overlayPlaceableColor = new Color(1f, 1f, 1f, 1f); // valid drop targets
    [SerializeField] private Color overlayHoverColor     = new Color(1f, 1f, 1f, 1f);   // tile under cursor
    [SerializeField] private float overlayColorLerpTime  = 0.5f;                             // hover fade duration (s)

    private UnitController heldUnit;      // Currently held unit
    private BaseTile       originalTile;  // Original tile before pickup (hex or bench)

    private BaseTile hoveredTile;                // Tile under mouse cursor
    private Material hoveredOriginalMaterial;   // Original material before highlight

    private readonly List<BaseTile> activeOverlayTiles = new List<BaseTile>(); // overlays lit for current drag

    // Ground plane raycast for unit dragging
    private readonly Plane groundPlane = new Plane(Vector3.up, Vector3.zero);


    private void Awake()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;
    }

    private void Update()
    {
        if (!IsActivePhase())
        {
            if (heldUnit != null) CancelPlacement();
            return;
        }

        HandleHover();

        if (heldUnit != null)
            UpdateDragVisuals();
    }

    /// <summary>
    /// Allow operation in all phases if BattleManager instance exists.
    /// </summary>
    private bool IsActivePhase()
        => BattleManager.Instance != null;

    /// <summary>
    /// In non-Preparation phases (Battle / Result), operate in bench-only mode.
    /// </summary>
    private bool IsBenchOnlyPhase()
        => BattleManager.Instance != null
        && BattleManager.Instance.CurrentPhase != BattleManager.Phase.Preparation;

    // ── Drag Visuals ───────��──────────────────────────────────

    private void UpdateDragVisuals()
    {
        Ray ray = mainCamera.ScreenPointToRay(Mouse.current.position.value);
        if (groundPlane.Raycast(ray, out float enter))
        {
            Vector3 hit    = ray.GetPoint(enter);
            Vector3 target = new Vector3(hit.x, hit.y + dragHeightOffset, hit.z);
            heldUnit.transform.position =
                Vector3.Lerp(heldUnit.transform.position, target, Time.deltaTime * dragFollowSpeed);
        }
    }

    public void OnPlaceClick(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            OnLeftClick();
        }
    }

    public void OnPlaceCancel(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            CancelPlacement();
        }
    }

    private void HandleHover()
    {
        // BaseTile raycast — detects both hex and bench tiles
        BaseTile tile = RaycastTarget<BaseTile>();

        if (tile == hoveredTile) return;

        ClearHover();

        if (tile != null && IsValidDropTarget(tile))
        {
            hoveredTile = tile;

            // Existing material-swap highlight (kept alongside the overlay)
            MeshRenderer mr = tile.GetComponent<MeshRenderer>();
            if (mr != null && highlightMaterial != null)
            {
                hoveredOriginalMaterial = mr.sharedMaterial;
                mr.sharedMaterial       = highlightMaterial;
            }

            // Overlay hover tint (only while dragging a unit)
            if (heldUnit != null && tile.Overlay != null)
                tile.Overlay.AnimateColor(overlayHoverColor, overlayColorLerpTime);
        }
    }

    private void ClearHover()
    {
        if (hoveredTile == null) return;

        MeshRenderer mr = hoveredTile.GetComponent<MeshRenderer>();
        if (mr != null && hoveredOriginalMaterial != null)
            mr.sharedMaterial = hoveredOriginalMaterial;

        // Fade overlay back to base tint if it's currently lit
        if (heldUnit != null && hoveredTile.Overlay != null)
            hoveredTile.Overlay.AnimateColor(overlayPlaceableColor, overlayColorLerpTime);

        hoveredTile             = null;
        hoveredOriginalMaterial = null;
    }

    private void OnLeftClick()
    {
        if (heldUnit == null) TryPickUp();
        else                  TryDrop();
    }

    private void TryPickUp()
    {
        BaseTile tile = RaycastTarget<BaseTile>();
        if (tile == null || !tile.IsOccupied) return;

        // Battle phase: can only pick from bench slots (field units locked)
        if (IsBenchOnlyPhase())
        {
            if (!(tile is BenchTileScript)) return;
        }
        else
        {
            if (tile is TileScript hexTile && !IsPlayerZone(hexTile)) return;
        }

        UnitController unit = GetUnitOnTile(tile);
        if (unit == null || unit.CurrentTeam != Team.Player) return;

        heldUnit     = unit;
        originalTile = tile;
        ShowPlacementOverlays();
    }

    /// <summary>
    /// Drop unit. origin(hex/bench) × target(hex/bench) × occupancy.
    /// </summary>
    private void TryDrop()
    {
        BaseTile targetTile = RaycastTarget<BaseTile>();

        if (targetTile == null || !IsValidDropTarget(targetTile))
        {
            CancelPlacement();
            return;
        }
        if (targetTile == originalTile)
        {
            CancelPlacement();
            return;
        }

        bool heldFromHex  = originalTile is TileScript;
        bool targetIsHex  = targetTile   is TileScript;
        UnitController other = targetTile.IsOccupied ? GetUnitOnTile(targetTile) : null;

        if (heldFromHex && targetIsHex)
        {
            // Hex → Hex
            if (other != null)
            {
                // Swap: both units stay in UnitManager
                other.PlaceOnTile((TileScript)originalTile, clearCurrent: false);
                heldUnit.PlaceOnTile((TileScript)targetTile,   clearCurrent: false);
            }
            else
            {
                heldUnit.PlaceOnTile((TileScript)targetTile);
            }
        }
        else if (heldFromHex) // Hex → Bench
        {
            var benchTarget = (BenchTileScript)targetTile;
            if (other != null)
            {
                // Swap: other(bench) → hex / held(hex) → bench
                BenchManager.Instance.RemoveUnit(other);
                UnitManager.Instance.AddUnit(other, other.CurrentTeam);
                other.PlaceOnTile((TileScript)originalTile, clearCurrent: false);

                UnitManager.Instance.RemoveUnit(heldUnit, heldUnit.CurrentTeam);
                BenchManager.Instance.AddUnit(heldUnit, benchTarget);
                heldUnit.PlaceOnBench(benchTarget, clearCurrent: false);
            }
            else
            {
                // Move: held → empty bench
                UnitManager.Instance.RemoveUnit(heldUnit, heldUnit.CurrentTeam);
                BenchManager.Instance.AddUnit(heldUnit, benchTarget);
                heldUnit.PlaceOnBench(benchTarget);
            }
        }
        else if (targetIsHex) // Bench → Hex
        {
            var hexTarget    = (TileScript)targetTile;
            var benchOrigin  = (BenchTileScript)originalTile;
            if (other != null)
            {
                // Swap: other(hex) → bench / held(bench) → hex
                UnitManager.Instance.RemoveUnit(other, other.CurrentTeam);
                BenchManager.Instance.AddUnit(other, benchOrigin);
                other.PlaceOnBench(benchOrigin, clearCurrent: false);

                BenchManager.Instance.RemoveUnit(heldUnit);
                UnitManager.Instance.AddUnit(heldUnit, heldUnit.CurrentTeam);
                heldUnit.PlaceOnTile(hexTarget, clearCurrent: false);
            }
            else
            {
                // Move: held → empty hex (blocked when the board is at capacity)
                if (BoardManager.Instance != null && !BoardManager.Instance.HasRoom)
                {
                    CancelPlacement();
                    return;
                }
                BenchManager.Instance.RemoveUnit(heldUnit);
                UnitManager.Instance.AddUnit(heldUnit, heldUnit.CurrentTeam);
                heldUnit.PlaceOnTile(hexTarget);
            }
        }
        else // Bench → Bench
        {
            var benchTarget = (BenchTileScript)targetTile;
            if (other != null)
            {
                // Swap: both units stay in BenchManager
                other.PlaceOnBench((BenchTileScript)originalTile, clearCurrent: false);
                heldUnit.PlaceOnBench(benchTarget, clearCurrent: false);
            }
            else
            {
                heldUnit.PlaceOnBench(benchTarget);
            }
        }

        heldUnit     = null;
        originalTile = null;
        HidePlacementOverlays();
    }

    private void CancelPlacement()
    {
        if (heldUnit != null && originalTile != null)
        {
            if (originalTile is TileScript hexTile)
                heldUnit.PlaceOnTile(hexTile);
            else if (originalTile is BenchTileScript benchSlot)
                heldUnit.PlaceOnBench(benchSlot);
        }
        heldUnit     = null;
        originalTile = null;
        HidePlacementOverlays();
    }

    // Placement Overlays //

    /// <summary>Light overlays on every valid drop target for the held unit.</summary>
    private void ShowPlacementOverlays()
    {
        activeOverlayTiles.Clear();
        foreach (BaseTile tile in TileManager.Instance.AllTiles) // field hexes
            LightOverlay(tile);
        foreach (BaseTile tile in BenchManager.Instance.AllTiles) // bench slots
            LightOverlay(tile);
    }

    /// <summary>Show and base-tint one tile's overlay if it's a valid drop target.</summary>
    private void LightOverlay(BaseTile tile)
    {
        if (tile == null || tile.Overlay == null) return;
        if (!IsValidDropTarget(tile)) return;

        tile.Overlay.Show();
        tile.Overlay.SetColor(overlayPlaceableColor);
        activeOverlayTiles.Add(tile);
    }

    /// <summary>Hide all overlays lit for the current drag.</summary>
    private void HidePlacementOverlays()
    {
        foreach (BaseTile tile in activeOverlayTiles)
            if (tile != null && tile.Overlay != null) tile.Overlay.Hide();
        activeOverlayTiles.Clear();
    }
/// <summary>
    /// Check if tile is a valid drop target.
    /// Battle phase: bench slots only.
    /// Preparation phase: hex (PlayerZone) + bench.
    /// </summary>
    private bool IsValidDropTarget(BaseTile tile)
    {
        if (IsBenchOnlyPhase())
            return tile is BenchTileScript;

        if (tile is TileScript hexTile) return IsPlayerZone(hexTile);
        if (tile is BenchTileScript)    return true;
        return false;
    }

    private bool IsPlayerZone(TileScript tile)
        => tile.GridCoordinate.y < playerZoneMaxRow;

    /// <summary>
    /// Raycast from mouse position and return T component on hit.
    /// </summary>
    private T RaycastTarget<T>() where T : Component
    {
        Ray ray = mainCamera.ScreenPointToRay(Mouse.current.position.value);
        if (Physics.Raycast(ray, out RaycastHit hit))
            return hit.collider.GetComponent<T>();
        return null;
    }

    /// <summary>
    /// Return the player unit on the given tile.
    /// Searches both hex field (UnitManager) and bench (BenchManager).
    /// </summary>
    private UnitController GetUnitOnTile(BaseTile tile)
    {
        foreach (UnitController unit in UnitManager.Instance.playerUnits)
            if (unit != null && unit.CurrentTile == tile) return unit;
        foreach (UnitController unit in BenchManager.Instance.benchUnits)
            if (unit != null && unit.CurrentTile == tile) return unit;
        return null;
    }
}
