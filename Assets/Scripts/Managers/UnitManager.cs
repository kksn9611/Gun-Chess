using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages combat unit rosters per team.
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

        Debug.Log($"Added {unit.Stats.UnitData.name} to {team} roster");
    }

    public void RemoveUnit(UnitController unit, Team team)
    {
        if      (team == Team.Player)  playerUnitList.Remove(unit);
        else if (team == Team.Enemy)   enemyUnitList.Remove(unit);
        else if (team == Team.Neutral) neutralUnitList.Remove(unit);
    }

    public IReadOnlyList<UnitController> GetEnemiesOf(Team team)
        => team == Team.Player ? enemyUnitList : playerUnitList;

    public IReadOnlyList<UnitController> GetAlliesOf(Team team)
        => team == Team.Player ? playerUnitList : enemyUnitList;

    /// <summary>
    /// Called on battle start and unit death to check if the battle has ended.
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


    /// <summary>Clear the unit list for a specific team.</summary>
    public void ClearTeam(Team team)
    {
        if      (team == Team.Player)  playerUnitList.Clear();
        else if (team == Team.Enemy)   enemyUnitList.Clear();
        else if (team == Team.Neutral) neutralUnitList.Clear();
    }

    /// <summary>Clear all team unit lists.</summary>
    public void ClearAll()
    {
        playerUnitList.Clear();
        enemyUnitList.Clear();
        neutralUnitList.Clear();
    }
}
