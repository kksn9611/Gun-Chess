using UnityEngine;

/// <summary>
/// Base for an augment's effect. Extensible: stat buffs (StatAugmentEffect), and later economy/utility
/// effects via OnAcquire or additional hooks. Recurring stat buffs are aggregated by AugmentManager and
/// applied over the reconciled player-board rebuild, not here.
/// </summary>
public abstract class AugmentEffect : ScriptableObject
{
    /// <summary>One-shot effect the moment the augment is chosen (e.g. grant gold, +1 board slot).</summary>
    public virtual void OnAcquire() { }
}
