using UnityEngine;
using System.Collections.Generic;

public class TileScript : MonoBehaviour
{
    [SerializeField] private Vector2Int gridCoordinate;
    [SerializeField] private bool isOccupied = false;
    private int movementCost = 1;
    private Vector3Int cubeCoordinate;
    private List<TileScript> neighbors;
    public Vector2Int GridCoordinate // ������ ��ǥ
    { 
        get => gridCoordinate; 
        set => gridCoordinate = value; 
    }

    public bool IsOccupied // ���� ����
    { 
        get => isOccupied; 
        set => isOccupied = value; 
    }

    public int MovementCost // �̵� ���
    {
        get => movementCost;
    }

    public Vector3Int CubeCoordinate // ���� ť����ǥ
    {
        get => cubeCoordinate;
        private set => cubeCoordinate = value;
    }

    public List<TileScript> Neighbors // �̿��� Ÿ�ϵ�
    {
        get => neighbors;
        private set => neighbors = value;
    }

    public void Initialize()
    {
        cubeCoordinate = HexCoordCal.OffsetToCube(gridCoordinate);
        neighbors = HexCoordCal.GetTileNeighbors(this);
    }
    
}
