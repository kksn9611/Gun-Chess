using System;
using UnityEngine;
using System.Collections.Generic;

public class UnitController : MonoBehaviour
{
    [SerializeField] private UnitData unitData;
    [SerializeField] private float currentHp;
    [SerializeField] private float currentMp;
    [SerializeField] private float currentAtt;
    [SerializeField] private float currentDef;
    [SerializeField] private float currentAttRange;
    [SerializeField] private float currentAttSpd;
    [SerializeField] private float currentMoveSpd;
    
    private Vector2Int currentCoord;
    [SerializeField] private Team currentTeam;

//유닛 초기화 및 소환
public void Initialize(UnitData data, TileScript spawnTile, Team team)
    {
        unitData = data;
        currentHp = unitData.maxHp;
        currentMp = 0f;
        currentAtt = unitData.att;
        currentDef = unitData.def;
        currentAttRange = unitData.attRange;
        currentAttSpd = unitData.attSpd;
        currentMoveSpd = unitData.moveSpd;
        currentTeam = team;
        currentCoord = spawnTile.GridCoordinate;
        
        Debug.Log($"{unitData.unitName}이(가) {currentCoord} 위치에 소환되었습니다!");
    }
public UnitController FindClosestTarget()
    {
        UnitController closestTarget = null;
        int minDistance = int.MaxValue;
        List<UnitController> targetList = new List<UnitController>();
        if (currentTeam == Team.Player)
        {
            targetList = UnitManager.Instance.enemyUnitList;
        }
        else if (currentTeam == Team.Enemy)
        {
            targetList = UnitManager.Instance.playerUnitList;    
        }

        foreach (UnitController target in targetList)
        {
            // 타겟 미스 방지
            if (target == null || target.currentHp <= 0) continue;
            // 거리 계산
            int distance = HexCoordCal.GetDistance(this.currentCoord, target.currentCoord);
            // 거리가 짧다면 타겟
            if (minDistance > distance)
            {
                minDistance = distance;
                closestTarget = target;
            }
            // 거리가 같다면
            else if (minDistance == distance)
            {
                // 사거리가 짧은 적 우선 타겟
                if (closestTarget.currentAttRange > target.currentAttRange)
                {
                    closestTarget = target;
                }
            }
        }
        Debug.Log($"{currentTeam}팀의 {UnitData.unitName}유닛이 {closestTarget.currentTeam}의 {closestTarget.UnitData.unitName} 타겟");
        return closestTarget;
    }

// 유닛 데이터 얻는 프로퍼티
public UnitData UnitData {get => unitData;}

    private void Update()
    {
        FindClosestTarget();
    }
}
