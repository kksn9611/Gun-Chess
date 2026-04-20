using UnityEngine;

/// <summary>
/// Manages the state of a single bench slot tile.
/// </summary>
public class BenchTileScript : BaseTile
{
    [SerializeField] private int  slotIndex;

    public int SlotIndex  => slotIndex;

    public override Vector2Int GetCoordinate() => new Vector2Int(slotIndex, -1);
    // Called by BenchLayout.LayoutBench() right after tile creation
    public void Initialize(int index)
    {
        slotIndex = index;
    }
}
