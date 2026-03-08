using UnityEngine;

public class TileScript : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
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
