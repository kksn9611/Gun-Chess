using UnityEngine;

/// <summary>
/// A chosen-between-rounds augment. Effects apply to the player side and persist across rounds; stacking
/// with other augments is additive.
/// </summary>
[CreateAssetMenu(fileName = "Augment", menuName = "Scriptable Objects/Augment/AugmentData")]
public class AugmentData : ScriptableObject
{
    public string augmentName;
    [TextArea] public string description;
    public Sprite icon;
    [Tooltip("Power tier — drives card color and how often it's offered (rarer = fewer in the pool)")]
    public AugmentRarity rarity = AugmentRarity.Common;
    [Tooltip("If true, cannot be offered or stacked once already owned")]
    public bool unique;
    public AugmentEffect[] effects;
}
