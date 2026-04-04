using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 벤치 유닛 목록 관리
/// </summary>
public class BenchManager
{
    private static BenchManager instance;
    public static BenchManager Instance => instance ??= new BenchManager();
    private BenchManager() { }

    private readonly Dictionary<int, BenchTileScript> benchList = new Dictionary<int, BenchTileScript>();
    private readonly List<UnitController> benchUnitList = new List<UnitController>();
    public IReadOnlyList<UnitController> benchUnits => benchUnitList;

    public void RegisterTile(int slotIndex, BenchTileScript tileScript)
    {
        benchList[slotIndex] = tileScript;
    }

    /// <summary>
    /// 비어있는 슬롯(IsOccupied == false)을 인덱스 순으로 탐색해 반환한다.
    /// 모든 슬롯이 찼으면 null.
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
    /// 유닛을 지정한 슬롯에 등록한다.
    /// IsOccupied 갱신과 이동은 UnitController.PlaceOnBench()가 담당한다.
    /// </summary>
    public void AddUnit(UnitController unit, BenchTileScript slot)
    {
        if (!benchUnitList.Contains(unit))
            benchUnitList.Add(unit);
    }

    /// <summary>
    /// 유닛을 벤치 목록에서 제거한다.
    /// IsOccupied 해제는 PlaceOnTile() / PlaceOnBench() 가 담당한다.
    /// </summary>
    public void RemoveUnit(UnitController unit)
    {
        benchUnitList.Remove(unit);
    }

    /// <summary>
    /// 해당 슬롯에 올라있는 유닛을 반환한다. 없으면 null.
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
