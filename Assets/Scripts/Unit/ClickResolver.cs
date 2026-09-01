using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Shared click resolution: one raycast that returns the tile and its occupant, whether the ray hit a
/// unit's collider or the tile itself. Clicking a unit resolves to the same tile as clicking the hex.
/// </summary>
public static class ClickResolver
{
    /// <summary>
    /// Raycast from the camera through screenPos. Returns true if a tile was hit (directly or via a unit).
    /// </summary>
    public static bool TryResolve(Camera cam, Vector2 screenPos, out BaseTile tile, out UnitController unit)
    {
        tile = null;
        unit = null;
        if (cam == null) return false;

        Ray ray = cam.ScreenPointToRay(screenPos);
        if (!Physics.Raycast(ray, out RaycastHit hit)) return false;

        // Hit a unit directly -> derive its tile (collider may live on a child mesh).
        UnitController hitUnit = hit.collider.GetComponentInParent<UnitController>();
        if (hitUnit != null)
        {
            unit = hitUnit;
            tile = hitUnit.CurrentTile;
            return tile != null;
        }

        // Hit a tile -> resolve its occupant (any team or bench).
        BaseTile hitTile = hit.collider.GetComponent<BaseTile>();
        if (hitTile != null)
        {
            tile = hitTile;
            unit = ResolveOccupant(hitTile);
            return true;
        }

        return false;
    }

    /// <summary>The unit standing on the given tile (any team or bench), or null.</summary>
    public static UnitController ResolveOccupant(BaseTile tile)
    {
        if (tile == null) return null;

        if (UnitManager.Instance != null)
        {
            UnitController u = Find(UnitManager.Instance.playerUnits, tile)
                            ?? Find(UnitManager.Instance.enemyUnits, tile)
                            ?? Find(UnitManager.Instance.neutralUnits, tile);
            if (u != null) return u;
        }
        if (BenchManager.Instance != null)
            return Find(BenchManager.Instance.benchUnits, tile);
        return null;
    }

    private static UnitController Find(IEnumerable<UnitController> list, BaseTile tile)
    {
        if (list == null) return null;
        foreach (UnitController u in list)
            if (u != null && u.CurrentTile == tile) return u;
        return null;
    }
}
