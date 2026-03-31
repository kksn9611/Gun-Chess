using UnityEngine;

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
