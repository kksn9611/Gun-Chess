using System;
using System.Collections.Generic;
using UnityEngine;

public class UnitManager : MonoBehaviour
{
// 싱글톤
public static UnitManager Instance {get; private set;}
private readonly List<UnitController> playerUnitList = new List<UnitController>();
private readonly List<UnitController> enemyUnitList = new List<UnitController>();
private readonly List<UnitController> neutralUnitList = new List<UnitController>();

    public IReadOnlyList<UnitController> playerUnits => playerUnitList;
    public IReadOnlyList<UnitController> enemyUnits => enemyUnitList;
    public IReadOnlyList<UnitController> neutralUnits => neutralUnitList;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else Destroy(gameObject);
    }

    public void AddUnit(UnitController unit, Team team)
    {
        if(team == Team.Player) playerUnitList.Add(unit);
        else if(team == Team.Enemy)enemyUnitList.Add(unit);
        else if (team == Team.Neutral) neutralUnitList.Add(unit);
        
        Debug.Log(team + "unitList에 " + unit.UnitData.name + "추가");
    }
    public void RemoveUnit(UnitController unit, Team team)
    {
        if (team == Team.Player) playerUnitList.Remove(unit);
        else if (team == Team.Enemy) enemyUnitList.Remove(unit);
        else if (team == Team.Neutral) neutralUnitList.Remove(unit);
    }
    public IReadOnlyList<UnitController> GetEnemiesOf(Team team)
    {
        return team == Team.Player ? enemyUnitList : playerUnitList;
    }
}
