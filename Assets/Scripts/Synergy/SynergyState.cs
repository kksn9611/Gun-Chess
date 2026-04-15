using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 현재 활성화된 시너지 상태를 저장하는 공유 데이터 저장소 ScriptableObject.
/// 프로젝트 전체에서 에셋 1개를 공유한다.
/// SynergyManager가 데이터를 기록하고, UI와 UnitController가 OnSynergyChanged를 구독한다.
/// </summary>
[CreateAssetMenu(fileName = "SynergyState", menuName = "Scriptable Objects/SynergyState")]
public class SynergyState : ScriptableObject
{
    /// <summary>현재 활성화된 시너지 목록 (SynergyManager가 갱신)</summary>
    [SerializeField] private List<SynergyEntry> entries = new List<SynergyEntry>();

    /// <summary>시너지 상태가 변경될 때 발행되는 이벤트</summary>
    public event Action OnSynergyChanged;

    /// <summary>현재 활성 시너지 목록 (읽기 전용)</summary>
    public IReadOnlyList<SynergyEntry> Entries => entries;

    /// <summary>
    /// SynergyManager가 재계산한 시너지 목록을 기록한다.
    /// 이전 목록을 교체하고 OnSynergyChanged를 발행한다.
    /// </summary>
    public void UpdateEntries(List<SynergyEntry> newEntries)
    {
        entries.Clear();
        entries.AddRange(newEntries);
        OnSynergyChanged?.Invoke();
    }

    /// <summary>
    /// 특정 시너지의 현재 활성 구간 인덱스를 반환한다.
    /// 등록되지 않은 시너지이면 -1을 반환한다.
    /// </summary>
    public int GetActiveTierIndex(SynergyData synergy)
    {
        foreach (var entry in entries)
        {
            if (entry.synergy == synergy)
                return entry.activeTierIndex;
        }
        return -1;
    }

    /// <summary>
    /// 게임 시작 또는 라운드 리셋 시 상태를 초기화한다.
    /// </summary>
    public void Clear()
    {
        entries.Clear();
        OnSynergyChanged?.Invoke();
    }
}

/// <summary>
/// 시너지 하나의 현재 활성 상태.
/// SynergyState의 entries 리스트에 담긴다.
/// </summary>
[System.Serializable]
public struct SynergyEntry
{
    [Tooltip("시너지 데이터 참조")]
    public SynergyData synergy;

    [Tooltip("전장에 배치된 해당 시너지 유닛 수")]
    public int currentCount;

    [Tooltip("현재 활성화된 구간 인덱스 (-1 = 미활성)")]
    public int activeTierIndex;
}
