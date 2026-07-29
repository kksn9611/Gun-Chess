using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages bench unit list and slot assignments.
/// </summary>
public class BenchManager
{
    private static BenchManager instance;
    public static BenchManager Instance => instance ??= new BenchManager();
    private BenchManager() { }

    private readonly Dictionary<int, BenchTileScript> benchList = new Dictionary<int, BenchTileScript>();
    private readonly List<UnitController> benchUnitList = new List<UnitController>();
    public IReadOnlyList<UnitController> benchUnits => benchUnitList;

    /// <summary>All registered bench slots (used for overlay/placement sweeps).</summary>
    public IEnumerable<BenchTileScript> AllTiles => benchList.Values;

    public void RegisterTile(int slotIndex, BenchTileScript tileScript)
    {
        benchList[slotIndex] = tileScript;
    }

    /// <summary>
    /// Return the first empty slot, or null if all slots are occupied.
    /// </summary>
    public BenchTileScript GetEmptySlot()
    {
        foreach (var item in benchList)
        {
            if (!item.Value.IsOccupied)
                return item.Value;
        }
        return null;
    }

    /// <summary>
    /// Register a unit on the given slot.
    /// IsOccupied update and positioning are handled by UnitController.PlaceOnBench().
    /// </summary>
    public void AddUnit(UnitController unit, BenchTileScript slot)
    {
        if (!benchUnitList.Contains(unit))
            benchUnitList.Add(unit);
    }

    /// <summary>
    /// Remove a unit from the bench list.
    /// IsOccupied release is handled by PlaceOnTile() / PlaceOnBench().
    /// </summary>
    public void RemoveUnit(UnitController unit)
    {
        benchUnitList.Remove(unit);
    }

    /// <summary>
    /// Return the unit occupying the given slot, or null if empty.
    /// </summary>
    public UnitController GetUnitOnSlot(BenchTileScript slot)
    {
        foreach (var unit in benchUnitList)
            if (unit != null && unit.CurrentTile == slot) return unit;
        return null;
    }

    public void Clear()
    {
        benchUnitList.Clear();
    }
}
