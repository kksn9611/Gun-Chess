using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 전장에 배치된 플레이어 유닛의 시너지를 집계하고,
/// SynergyState SO에 활성 상태를 기록
/// </summary>
public class SynergyManager : MonoBehaviour
{
    [Header("공유 데이터")]
    [Tooltip("프로젝트 전체에서 공유하는 SynergyState 에셋")]
    [SerializeField] private SynergyState synergyState;

    /// <summary>
    /// 유닛 스폰 시 OnBenchState 이벤트를 구독하여,
    /// 전장↔벤치 전환 시 시너지를 재계산한다.
    /// </summary>
    private void OnEnable()
    {
        UnitController.OnUnitSpawned += OnUnitSpawned;
        BattleManager.OnBattleEnd   += OnBattleEnd;
    }

    private void OnDisable()
    {
        UnitController.OnUnitSpawned -= OnUnitSpawned;
        BattleManager.OnBattleEnd   -= OnBattleEnd;
    }

    /// <summary>
    /// 유닛이 스폰될 때 OnBenchState 이벤트를 구독한다.
    /// 플레이어 유닛만 시너지에 영향을 준다.
    /// </summary>
    private void OnUnitSpawned(UnitController unit)
    {
        if (unit.CurrentTeam != Team.Player) return;
        unit.OnBenchState += _ => Recalculate();
        // 스폰 직후에도 재계산 (전장에 직접 스폰되는 경우)
        Recalculate();
    }

    /// <summary>
    /// 전투 종료 시 시너지를 재계산한다.
    /// RestorePlayerPositions()에서 PlaceOnTile() → OnBenchState → Recalculate()가
    /// 트리거되지만, 복원 완료 후에도 한 번 더 재계산하여 최종 상태를 보장한다.
    /// Clear()는 하지 않는다 — Recalculate()가 매번 전체 상태를 덮어쓰므로 불필요.
    /// </summary>
    private void OnBattleEnd(Team winner)
    {
        // Recalculate는 RestorePlayerPositions() 완료 후 OnBenchState 이벤트로 자동 트리거됨
    }

    /// <summary>
    /// 전장(벤치 제외)에 배치된 플레이어 유닛의 시너지 태그를 집계하고,
    /// SynergyData의 활성 구간과 비교하여 SynergyState를 갱신한다.
    /// </summary>
    public void Recalculate()
    {
        if (synergyState == null) return;

        // 1) 전장 유닛 시너지 집계 (같은 UnitData는 1회만 카운트)
        Dictionary<SynergyData, int> synergyCounts = new Dictionary<SynergyData, int>();
        HashSet<UnitData> countedUnitData = new HashSet<UnitData>();

        foreach (var unit in UnitManager.Instance.playerUnits)
        {
            if (unit == null || unit.IsOnBench) continue;
            if (unit.UnitData == null || unit.UnitData.synergies == null) continue;

            // 같은 UnitData를 가진 유닛이 여럿이면 한 번만 집계
            if (!countedUnitData.Add(unit.UnitData)) continue;

            foreach (var synergy in unit.UnitData.synergies)
            {
                if (synergy == null) continue;
                if (synergyCounts.ContainsKey(synergy))
                    synergyCounts[synergy]++;
                else
                    synergyCounts[synergy] = 1;
            }
        }

        // 벤치 유닛도 순회하여 태그가 있지만 전장에 없는 시너지도 0카운트로 표시
        //foreach (var unit in BenchManager.Instance.benchUnits)
        //{
        //    if (unit == null || unit.UnitData.synergies == null) continue;
        //    foreach (var synergy in unit.UnitData.synergies)
        //    {
        //        if (synergy == null) continue;
        //        if (!synergyCounts.ContainsKey(synergy))
        //            synergyCounts[synergy] = 0;
        //    }
        //}

        // 2) SynergyEntry 리스트 생성
        List<SynergyEntry> newEntries = new List<SynergyEntry>();

        foreach (var pair in synergyCounts)
        {
            SynergyData synergy = pair.Key;
            int count = pair.Value;
            int tierIndex = synergy.GetActiveTierIndex(count);

            newEntries.Add(new SynergyEntry
            {
                synergy = synergy,
                currentCount = count,
                activeTierIndex = tierIndex
            });
        }

        // 3) SynergyState에 기록 → OnSynergyChanged 발행
        synergyState.UpdateEntries(newEntries);

        // 디버그 로그
        foreach (var entry in newEntries)
        {
            if (entry.activeTierIndex >= 0)
            {
                Debug.Log($"[시너지] {entry.synergy.synergyName}: " +
                          $"{entry.currentCount}기 → Tier {entry.activeTierIndex + 1} 활성");
            }
        }
    }
}
