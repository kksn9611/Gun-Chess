using UnityEngine;

/// <summary>
/// 타일 시스템 기본 타일
/// </summary>
public abstract class BaseTile : MonoBehaviour
{
    [SerializeField] private bool isOccupied = false;

    public bool IsOccupied
    {
        get => isOccupied;
        set => isOccupied = value;
    }
    // 타일 좌표 반환
    public abstract Vector2Int GetCoordinate();

}
