using UnityEngine;

public class TileScript : MonoBehaviour
{
    [SerializeField] private Vector2Int gridCoordinate;
    [SerializeField] private bool isOccupied = false;


    public Vector2Int GridCoordinate 
    { 
        get => gridCoordinate; 
        set => gridCoordinate = value; 
    }

    public bool IsOccupied 
    { 
        get => isOccupied; 
        set => isOccupied = value; 
    }

}
