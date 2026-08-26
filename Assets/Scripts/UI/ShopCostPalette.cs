using UnityEngine;

/// <summary>
/// Shared cost-tier palette for shop slots. Index 0 = 1-cost … index 4 = 5-cost.
/// </summary>
[CreateAssetMenu(fileName = "ShopCostPalette", menuName = "Scriptable Objects/ShopCostPalette")]
public class ShopCostPalette : ScriptableObject
{
    [Tooltip("Frame color per cost tier (index 0 = 1-cost)")]
    public Color[] costColors =
    {
        new Color(0.42f, 0.45f, 0.50f, 1f), // 1 gray
        new Color(0.07f, 0.70f, 0.53f, 1f), // 2 green
        new Color(0.13f, 0.48f, 0.78f, 1f), // 3 blue
        new Color(0.77f, 0.25f, 0.85f, 1f), // 4 purple
        new Color(1.00f, 0.72f, 0.23f, 1f), // 5 gold
    };

    [Tooltip("Minimum cost that shows the glow")]
    public int glowMinCost = 5;

    /// <summary>Frame color for a cost (clamped to the table).</summary>
    public Color ColorFor(int cost) => costColors[Mathf.Clamp(cost - 1, 0, costColors.Length - 1)];

    /// <summary>Whether this cost should show the glow.</summary>
    public bool ShouldGlow(int cost) => cost >= glowMinCost;
}
