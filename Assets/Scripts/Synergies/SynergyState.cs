using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Shared data store ScriptableObject for current active synergy state.
/// One asset shared project-wide.
/// SynergyManager writes data; UI and UnitController subscribe to OnSynergyChanged.
/// </summary>
[CreateAssetMenu(fileName = "SynergyState", menuName = "Scriptable Objects/SynergyState")]
public class SynergyState : ScriptableObject
{
    /// <summary>Current active synergy list (updated by SynergyManager)</summary>
    [SerializeField] private List<SynergyEntry> entries = new List<SynergyEntry>();

    /// <summary>Event fired when synergy state changes</summary>
    public event Action OnSynergyChanged;

    /// <summary>Current active synergy list (read-only)</summary>
    public IReadOnlyList<SynergyEntry> Entries => entries;

    /// <summary>
    /// Write recalculated synergy list from SynergyManager.
    /// Replaces previous list and fires OnSynergyChanged.
    /// </summary>
    public void UpdateEntries(List<SynergyEntry> newEntries)
    {
        entries.Clear();
        entries.AddRange(newEntries);
        OnSynergyChanged?.Invoke();
    }

    /// <summary>
    /// Return the active tier index for a synergy. Returns -1 if not registered.
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
    /// Clear state on game start or round reset.
    /// </summary>
    public void Clear()
    {
        entries.Clear();
        OnSynergyChanged?.Invoke();
    }
}

/// <summary>
/// Current active state of a single synergy.
/// Stored in SynergyState.entries list.
/// </summary>
[System.Serializable]
public struct SynergyEntry
{
    [Tooltip("Synergy data reference")]
    public SynergyData synergy;

    [Tooltip("Number of this synergy's units on the field")]
    public int currentCount;

    [Tooltip("Currently active tier index (-1 = inactive)")]
    public int activeTierIndex;
}
