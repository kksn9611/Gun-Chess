using UnityEngine;
using System.Collections;
public class TestScript : MonoBehaviour
{
    public UnitData mobUnit;
    IEnumerator Start()
    {
        // HexGridLayout이 타일을 다 생성할 때까지 대기
        yield return new WaitForSeconds(0.2f);

        TileScript spawnTile = TileManager.Instance.GetTile(new Vector2Int(0, 0));
        TileScript targetTile = TileManager.Instance.GetTile(new Vector2Int(5, 5));
        if (spawnTile != null)
        {
            UnitSpawner.Instance.SpawnUnit(mobUnit, spawnTile);
            Debug.Log("spawnTile에 유닛 소환");
        }
        if (targetTile != null)
        {
            UnitSpawner.Instance.SpawnUnit(mobUnit, targetTile);
            Debug.Log("targetTile에 유닛 소환");
        }
    }

    void Update()
    {
        
    }
}
