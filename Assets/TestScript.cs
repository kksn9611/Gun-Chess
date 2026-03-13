using UnityEngine;
using System.Collections;
public class TestScript : MonoBehaviour
{
    public UnitData mobUnit;
    [SerializeField]private UnitSpawner unitSpawner;
    IEnumerator Start()
    {
        // HexGridLayout이 타일을 다 생성할 때까지 대기
        yield return new WaitForSeconds(0.2f);

        TileScript spawnTile = TileManager.Instance.GetTile(new Vector2Int(0, 0));
        TileScript targetTile = TileManager.Instance.GetTile(new Vector2Int(5, 5));
        if (spawnTile != null)
        {
            unitSpawner.SpawnUnit(mobUnit, spawnTile, Team.Player);
        }
        if (targetTile != null)
        {
            unitSpawner.SpawnUnit(mobUnit, targetTile, Team.Enemy);
        }
        
    }
}
