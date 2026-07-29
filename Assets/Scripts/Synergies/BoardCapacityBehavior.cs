using UnityEngine;

/// <summary>
/// Grants extra board slots while this synergy tier is active. Global (not per-unit),
/// summed by SynergyManager.CalculateBoardBonus() and read by BoardManager.
/// </summary>
[CreateAssetMenu(fileName = "BoardCapacity", menuName = "Scriptable Objects/Synergy/BoardCapacityBehavior")]
public class BoardCapacityBehavior : SynergyBehavior
{
    [Tooltip("Extra field slots granted while this tier is active")]
    public int bonusSlots = 1;

    // Board capacity is a global effect, not a per-unit buff.
    public override void Apply(UnitController unit) { }
    public override void Remove(UnitController unit) { }
}
