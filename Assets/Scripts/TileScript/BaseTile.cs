using UnityEngine;

/// <summary>
/// Base class for all tile types.
/// </summary>
public abstract class BaseTile : MonoBehaviour
{
    [SerializeField] private bool isOccupied = false;
    [SerializeField] private TileOverlay overlay; // glowing placement overlay (assigned by layout)

    public bool IsOccupied
    {
        get => isOccupied;
        set => isOccupied = value;
    }

    public TileOverlay Overlay => overlay;
    public void SetOverlay(TileOverlay o) => overlay = o;

    // Return tile coordinate
    public abstract Vector2Int GetCoordinate();

}
