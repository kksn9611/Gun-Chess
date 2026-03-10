using UnityEngine;

public class UnitController : MonoBehaviour
{
    [SerializeField] private UnitData unitData;
    [SerializeField] private float currentHp;
    [SerializeField] private Vector2Int currentCoord;
public void Initialize(UnitData data, TileScript spawnTile)
    {
        unitData = data;
        currentCoord = spawnTile.GridCoordinate;
        currentHp = unitData.maxHp;
        
        Debug.Log($"{unitData.unitName}이(가) {currentCoord} 위치에 소환되었습니다!");
    }
}
