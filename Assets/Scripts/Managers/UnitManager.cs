using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 전투 참여 목록 관리
/// </summary>
public class UnitManager : MonoBehaviour
{
    public static UnitManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }
    private UnitManager() { }

    [SerializeField] private  List<UnitController> playerUnitList  = new List<UnitController>();
    [SerializeField] private  List<UnitController> enemyUnitList   = new List<UnitController>();
    [SerializeField] private  List<UnitController> neutralUnitList = new List<UnitController>();
    public IReadOnlyList<UnitController> playerUnits  => playerUnitList;
    public IReadOnlyList<UnitController> enemyUnits   => enemyUnitList;
    public IReadOnlyList<UnitController> neutralUnits => neutralUnitList;

    public void AddUnit(UnitController unit, Team team)
    {
        if      (team == Team.Player)  playerUnitList.Add(unit);
        else if (team == Team.Enemy)   enemyUnitList.Add(unit);
        else if (team == Team.Neutral) neutralUnitList.Add(unit);

        Debug.Log($"{team} 팀 목록에 {unit.UnitData.name} 추가");
    }

    public void RemoveUnit(UnitController unit, Team team)
    {
        if      (team == Team.Player)  playerUnitList.Remove(unit);
        else if (team == Team.Enemy)   enemyUnitList.Remove(unit);
        else if (team == Team.Neutral) neutralUnitList.Remove(unit);
    }

    public IReadOnlyList<UnitController> GetEnemiesOf(Team team)
        => team == Team.Player ? enemyUnitList : playerUnitList;

    /// <summary>
    /// 전투 시작, 유닛 사망 시 호출해 전투가 끝났는지 확인.
    /// </summary>
    public void CheckBattleEnd()
    {
        if (BattleManager.Instance == null ||
            BattleManager.Instance.CurrentPhase != BattleManager.Phase.Battle)
            return;

        if (enemyUnitList.Count == 0)
        {
            BattleManager.Instance.EndBattle(Team.Player);
        }
        else if (playerUnitList.Count == 0)
        {
            BattleManager.Instance.EndBattle(Team.Enemy);
        }
    }


    /// <summary>특정 팀의 유닛 목록만 비운다.</summary>
    public void ClearTeam(Team team)
    {
        if      (team == Team.Player)  playerUnitList.Clear();
        else if (team == Team.Enemy)   enemyUnitList.Clear();
        else if (team == Team.Neutral) neutralUnitList.Clear();
    }

    /// <summary>모든 팀의 유닛 목록을 비운다.</summary>
    public void ClearAll()
    {
        playerUnitList.Clear();
        enemyUnitList.Clear();
        neutralUnitList.Clear();
    }
}
