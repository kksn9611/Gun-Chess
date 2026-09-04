using UnityEngine;

/// <summary>
/// Augment effect that adds player-team stat buffs. Recurring — the boosts are summed across all owned
/// augments by AugmentManager and applied over the player board each rebuild (stacking is additive).
/// </summary>
[CreateAssetMenu(fileName = "StatAugmentEffect", menuName = "Scriptable Objects/Augment/StatAugmentEffect")]
public class StatAugmentEffect : AugmentEffect
{
    [Tooltip("Percent stat boosts applied to every player board unit (stacks with other augments)")]
    public StatBoostEntry[] boosts;
}
