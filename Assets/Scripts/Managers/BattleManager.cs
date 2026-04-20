using System;
using UnityEngine;

/// <summary>
/// Phase transitions and static events (OnBattleStart / OnBattleEnd).
/// Units subscribe to OnBattleStart via OnEnable/OnDisable.
/// </summary>
public class BattleManager : MonoBehaviour
{
    public static BattleManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }
    public enum Phase { Preparation, Battle, Result }

    // Current battle phase
    [field: SerializeField] public Phase CurrentPhase { get; private set; } = Phase.Preparation;


    // Events //
    public static event Action OnBattleStart;   // Fires on StartBattle(); activates all unit AI
    public static event Action<Team> OnBattleEnd; // Fires on EndBattle(); passes winning team

    /// <summary>
    /// Start battle and fire OnBattleStart event.
    /// </summary>
    public void StartBattle()
    {
        if (CurrentPhase == Phase.Battle)
        {
            Debug.LogWarning("[BattleManager] Already in battle");
            return;
        }

        CurrentPhase = Phase.Battle;
        Debug.Log("[BattleManager] Battle started");
        OnBattleStart?.Invoke();
        UnitManager.Instance.CheckBattleEnd();
    }

    /// <summary>
    /// End battle. Transition Battle → Result and pass winning team to OnBattleEnd.
    /// </summary>
    public void EndBattle(Team winner)
    {
        if (CurrentPhase != Phase.Battle)
        {
            Debug.LogWarning("[BattleManager] EndBattle called outside of battle");
            return;
        }

        CurrentPhase = Phase.Result;
        Debug.Log($"[BattleManager] Battle ended — Winner: {winner}");
        OnBattleEnd?.Invoke(winner);
    }

    /// <summary>
    /// Reset phase to Preparation for the next round.
    /// </summary>
    public void ResetBattle()
    {
        CurrentPhase = Phase.Preparation;
        Debug.Log("[BattleManager] Reset to Preparation phase");
    }
}
