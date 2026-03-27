using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 전투 참여 목록 관리
/// </summary>
public class UnitManager
{
    private static UnitManager instance;
    public static UnitManager Instance => instance ??= new UnitManager();

    private UnitManager() { }

    private readonly List<UnitController> playerUnitList  = new List<UnitController>();
    private readonly List<UnitController> enemyUnitList   = new List<UnitController>();
    private readonly List<UnitController> neutralUnitList = new List<UnitController>();

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
    /// 유닛 사망 시 호출. 어느 한 팀의 목록이 비면 BattleManager에 전투 종료를 알린다.
    /// BattleManager가 Battle 페이즈가 아닐 때는 무시한다 (준비 페이즈 중 유닛 제거 대비).
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


    public void Clear()
    {
        playerUnitList.Clear();
        enemyUnitList.Clear();
        neutralUnitList.Clear();
    }
}
