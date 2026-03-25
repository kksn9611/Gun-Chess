using System;
using UnityEngine;

/// <summary>
/// 전투 흐름을 관리하는 싱글톤.
/// Phase 전환과 static 이벤트(OnBattleStart / OnBattleEnd)
/// 유닛은 OnEnable/OnDisable에서 OnBattleStart를 구독해 전투 시작 신호를 받는다.
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

    // 현재 전투 페이즈.
    [field: SerializeField] public Phase CurrentPhase { get; private set; } = Phase.Preparation;


    // 이벤트 — 유닛/UI 등 외부에서 구독

    //StartBattle() 호출 시 발동. 모든 유닛의 AI를 시작
    public static event Action OnBattleStart;

    //EndBattle() 호출 시 발동. 승리 팀을 인자로 전달
    public static event Action<Team> OnBattleEnd;

    /// <summary>
    /// 전투 시작 OnBattleStart 이벤트 발동
    /// </summary>
    public void StartBattle()
    {
        if (CurrentPhase == Phase.Battle)
        {
            Debug.LogWarning("[BattleManager] 이미 전투 중");
            return;
        }

        CurrentPhase = Phase.Battle;
        Debug.Log("[BattleManager] 전투 시작");
        OnBattleStart?.Invoke();
    }

    /// <summary>
    /// 전투를 종료한다. Battle → Result 페이즈로 전환하고
    /// OnBattleEnd 이벤트에 승리 팀을 전달
    /// </summary>
    public void EndBattle(Team winner)
    {
        if (CurrentPhase != Phase.Battle)
        {
            Debug.LogWarning("[BattleManager] 전투 중이 아닌데 EndBattle 호출");
            return;
        }

        CurrentPhase = Phase.Result;
        Debug.Log($"[BattleManager] 전투 종료 — 승리: {winner}");
        OnBattleEnd?.Invoke(winner);
    }

    /// <summary>
    /// 다음 라운드를 위해 페이즈를 Preparation으로 초기화
    /// </summary>
    public void ResetBattle()
    {
        CurrentPhase = Phase.Preparation;
        Debug.Log("[BattleManager] 준비 페이즈로 리셋");
    }
}
