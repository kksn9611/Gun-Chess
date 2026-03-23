using System;
using System.Collections.Generic;
using UnityEngine;
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
    [SerializeField] private TileScript currentTile;
    [SerializeField] private Vector2Int currentCoord;
    [SerializeField] private Team currentTeam;
    [SerializeField] private UnitState currentState = UnitState.Idle;
    [SerializeField] private UnitController currentTarget;
    // 유닛 데이터 얻는 프로퍼티
    public UnitData UnitData { get => unitData; }
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
        currentTile = spawnTile;
        spawnTile.IsOccupied = true;
        currentState = UnitState.Idle;

        Debug.Log($"{unitData.unitName}이(가) {currentCoord} 위치에 소환되었습니다!");
    }

private void Update()
 {
        switch (currentState)
        {
            case UnitState.Idle:
                break;
            case UnitState.Moving:
                break;
            case UnitState.Attacking:
                break;
        }
 }
//유닛 타겟 함수
public UnitController FindClosestTarget()
    {
        UnitController closestTarget = null;
        int minDistance = int.MaxValue;
        IReadOnlyList<UnitController> targetList = new List<UnitController>();
        targetList = UnitManager.Instance.GetEnemiesOf(currentTeam);

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
        if (closestTarget != null)
        {
            Debug.Log($"{currentTeam}팀의 {UnitData.unitName}유닛이 {closestTarget.currentTeam}의 {closestTarget.UnitData.unitName} 타겟");
            return closestTarget;
        }
        return closestTarget;
    }

    public void EnterIdleState() // 다른 상태에서 Idle로 전환할 때 호출
    {
        currentState = UnitState.Idle;
        currentTarget = FindClosestTarget();

        if (currentTarget == null) return; // 타겟 없으면 행동 종료

        int distance = HexCoordCal.GetDistance(currentCoord, currentTarget.currentCoord);
        if (distance <= currentAttRange) // 사거리 안이면 공격, 밖이면 이동
            EnterAttackState();
        else
            EnterMoveState();
    }
public void EnterAttackState() // 다른 상태에서 Attack로 전환할 때 호출
    {
        currentState = UnitState.Attacking;
    }
public void EnterMoveState() // 다른 상태에서 Move로 전환할 때 호출
    {
        currentState = UnitState.Moving;
    }
public void TakeDamage(float damage)
    {
        float actualDamage = damage * (1f - currentDef / 100f); // 방어력 10이면 10% 감소
        currentHp -= actualDamage;

        if (currentHp <= 0f)
        {
            currentHp = 0f;
            Die();
        }
    }

public void Die()
    {
        currentState = UnitState.Dead;
        currentTile.IsOccupied = false;
        currentTile = null;

        UnitManager.Instance.RemoveUnit(this, currentTeam);
      //UnitManager.Instance.OnUnitDied(currentTeam); // 승패 판정 (미구현)
        Destroy(gameObject, 3f);
    }
}
