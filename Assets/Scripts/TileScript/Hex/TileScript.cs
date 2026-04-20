using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Manages the state of a single battlefield hex tile.
/// </summary>
public class TileScript : BaseTile
{
    [SerializeField] private Vector2Int gridCoordinate;
    private int movementCost = 1;
    private Vector3Int cubeCoordinate;
    private List<TileScript> neighbors;
    public Vector2Int GridCoordinate // Grid coordinate
    {
        get => gridCoordinate;
        set => gridCoordinate = value;
    }

    public int MovementCost // Movement cost
    {
        get => movementCost;
    }

    public Vector3Int CubeCoordinate // Cube coordinate
    {
        get => cubeCoordinate;
        private set => cubeCoordinate = value;
    }

    public List<TileScript> Neighbors // Neighboring tiles
    {
        get => neighbors;
        private set => neighbors = value;
    }

    public override Vector2Int GetCoordinate() => gridCoordinate; // Return own coordinate
    public void Initialize()
    {
        cubeCoordinate = HexCoordCal.OffsetToCube(gridCoordinate);
        neighbors = HexCoordCal.GetTileNeighbors(this);
    }

}
