using UnityEngine;

/// <summary>
/// Augment effect that grants the player a one-shot lump of gold the moment it's chosen.
/// One-shot only (via OnAcquire) — nothing recurring to aggregate.
/// </summary>
[CreateAssetMenu(fileName = "GoldAugmentEffect", menuName = "Scriptable Objects/Augment/GoldAugmentEffect")]
public class GoldAugmentEffect : AugmentEffect
{
    [Tooltip("Gold granted to the player on pick")]
    public int gold = 10;

    public override void OnAcquire()
    {
        if (PlayerManager.Instance != null) PlayerManager.Instance.AddGold(gold);
    }
}
