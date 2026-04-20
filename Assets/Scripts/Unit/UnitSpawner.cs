using System;
using UnityEngine;

public class UnitSpawner : MonoBehaviour
{
    /// <summary>
    /// Spawn a unit and place it on a tile (hex or bench).
    /// Accepts BaseTile, so both TileScript and BenchTileScript work.
    /// register=true: immediately register with UnitManager for combat.
    /// </summary>
    public UnitController SpawnUnit(UnitData data, BaseTile targetTile, Team team, bool register = true)
    {
        // Placement validation
        if (targetTile == null || targetTile.IsOccupied)
        {
            Debug.LogWarning("Cannot place unit");
            return null;
        }

        // Spawn unit
        GameObject unitObj =
            Instantiate(data.unitPrefab, targetTile.transform.position, Quaternion.identity);
        // Initialize controller and register to unit list
        if (unitObj.TryGetComponent<UnitController>(out UnitController controller))
        {
            controller.Initialize(data, targetTile, team);
            if (register)
                UnitManager.Instance.AddUnit(controller, team);
            return controller;
        }

        Debug.LogError($"UnitController missing on {data.unitPrefab.name}, spawn failed");
        Destroy(unitObj);
        return null;
    }
}
