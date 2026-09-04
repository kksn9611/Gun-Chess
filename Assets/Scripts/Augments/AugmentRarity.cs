using UnityEngine;

/// <summary>
/// Augment power tiers. Higher rarities carry stronger effects and are mixed into the offer pool in
/// smaller numbers (see AugmentSelectUI rarity weights), so they appear far less often.
/// </summary>
public enum AugmentRarity
{
    Common = 0,
    Bronze = 1,
    Silver = 2,
    Gold = 3
}

public static class AugmentRarityExtensions
{
    // Card Tint //

    /// <summary>Distinct card color per rarity (readable on the dark select panel).</summary>
    public static Color ToColor(this AugmentRarity rarity)
    {
        switch (rarity)
        {
            case AugmentRarity.Bronze:      return new Color(0.55f, 0.27f, 0.08f); //
            case AugmentRarity.Silver:      return new Color(0.6f, 0.6f, 0.6f); // 
            case AugmentRarity.Gold: return new Color(1.00f, 0.75f, 0.15f); // gold
            default:                      return new Color(0.2f, 0.2f, 0.2f); // 
        }
    }
}
