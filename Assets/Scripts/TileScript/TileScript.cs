using UnityEngine;
using System.Collections.Generic;

public class TileScript : MonoBehaviour
{
    [SerializeField] private Vector2Int gridCoordinate;
    [SerializeField] private bool isOccupied = false;
    private int movementCost = 1;
    private Vector3Int cubeCoordinate;
    private List<TileScript> neighbors;
    public Vector2Int GridCoordinate // À°°¢Çü ÁÂÇ¥
    { 
        get => gridCoordinate; 
        set => gridCoordinate = value; 
    }

    public bool IsOccupied // À¯´Ö ¿©ºÎ
    { 
        get => isOccupied; 
        set => isOccupied = value; 
    }

    public int MovementCost // ÀÌµ¿ ºñ¿ë
    {
        get => movementCost;
    }

    public Vector3Int CubeCoordinate // °è»ê¿ë Å¥ºêÁÂÇ¥
    {
        get => cubeCoordinate;
        private set => cubeCoordinate = value;
    }

    public List<TileScript> Neighbors // ÀÌ¿ôÇÑ Å¸ÀÏµé
    {
        get => neighbors;
        private set => neighbors = value;
    }

    public void Initialize()
    {
        cubeCoordinate = HexCoordCal.OffsetToCube(gridCoordinate);
        neighbors = HexCoordCal.GetTileNeighbors(this);
        Debug.Log(Neighbors);
    }
    
}
