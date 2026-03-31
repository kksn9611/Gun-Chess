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
    public IReadOnlyList<UnitController> BenchUnits => benchUnitList;

    public void RegisterTile(int slotIndex, BenchTileScript tileScript)
    {
        benchList[slotIndex] = tileScript;
    }
}
