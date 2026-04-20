using UnityEngine;

/// <summary>
/// Base class for all tile types.
/// </summary>
public abstract class BaseTile : MonoBehaviour
{
    [SerializeField] private bool isOccupied = false;

    public bool IsOccupied
    {
        get => isOccupied;
        set => isOccupied = value;
    }
    // Return tile coordinate
    public abstract Vector2Int GetCoordinate();

}
