using System;
using System.Collections.Generic;
using UnityEngine;

// 유닛 팀 enum
public enum Team
    {
        Player,
        Enemy,
        Neutral
    }
public class UnitManager : MonoBehaviour
{
// 싱글톤
public static UnitManager Instance {get; private set;}
private List<UnitController> playerUnitList = new List<UnitController>();
private List<UnitController> enemyUnitList = new List<UnitController>();
private List<UnitController> neutralUnitList = new List<UnitController>();

    void Awake()
    {
        if(Instance == null)
        {
            Instance = this;
        }
        else Destroy(gameObject);
    }

    public void AddUnitToList(UnitController unit, Team team)
    {
        if(team == Team.Player)
        {
            playerUnitList.Add(unit);
        }
        else if(team == Team.Enemy)
        {
            enemyUnitList.Add(unit);
        }
        else
        {
            neutralUnitList.Add(unit);
        }
        Debug.Log(team + "unitList에 " + unit.UnitData.name + "추가");
    }




}
