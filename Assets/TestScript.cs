using UnityEngine;
using System.Collections;
public class TestScript : MonoBehaviour
{
    public UnitData mobUnit;
    IEnumerator Start()
    {
        // HexGridLayout이 타일을 다 생성할 때까지 대기
        yield return new WaitForSeconds(0.2f);

        // 시간이 지났으니 이제 딕셔너리에 값이 꽉 차 있습니다!
        TileScript targetTile = TileManager.Instance.GetTile(new Vector2Int(0, 0));

        if (targetTile != null)
        {
            UnitSpawner.Instance.SpawnUnit(mobUnit, targetTile);
        }
    }

    void Update()
    {
        
    }
}
