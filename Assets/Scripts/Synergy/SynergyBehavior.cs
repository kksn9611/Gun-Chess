using UnityEngine;

/// <summary>
/// Abstract base ScriptableObject for synergy effects.
/// Drag-and-drop assets into SynergyTier.behaviors array.
/// </summary>
public abstract class SynergyBehavior : ScriptableObject
{
    /// <summary>
    /// Apply synergy activation effect.
    /// </summary>
    public abstract void Apply(UnitController unit);

    /// <summary>
    /// Remove synergy effect — exactly revert what Apply() did.
    /// </summary>
    public abstract void Remove(UnitController unit);
}
