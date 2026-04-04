using UnityEngine;

/// <summary>
/// 벤치 슬롯 타일 하나의 상태를 관리
/// </summary>
public class BenchTileScript : BaseTile
{
    [SerializeField] private int  slotIndex;

    public int SlotIndex  => slotIndex;

    public override Vector2Int GetCoordinate() => new Vector2Int(slotIndex, -1);
    // BenchLayout.LayoutBench() 에서 타일 생성 직후 호출
    public void Initialize(int index)
    {
        slotIndex = index;
    }
}
