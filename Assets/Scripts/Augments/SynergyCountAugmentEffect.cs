using UnityEngine;

/// <summary>
/// Augment effect that grants bonus synergy count to a specific synergy (e.g. +1). Aggregated by
/// AugmentManager and folded into the field tally each rebuild, so it can activate a synergy from zero.
/// </summary>
[CreateAssetMenu(fileName = "SynergyCountAugmentEffect", menuName = "Scriptable Objects/Augment/SynergyCountAugmentEffect")]
public class SynergyCountAugmentEffect : AugmentEffect
{
    [Tooltip("Synergy that gets the bonus count")]
    public SynergyData targetSynergy;
    [Tooltip("Extra count added to the synergy tally (activates it from zero)")]
    public int bonusCount = 1;
}
