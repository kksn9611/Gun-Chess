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
        TileScript spawnTile2 = TileManager.Instance.GetTile(new Vector2Int(2, 2));
        TileScript targetTile = TileManager.Instance.GetTile(new Vector2Int(5, 5));
        if (spawnTile != null)
        {
            unitSpawner.SpawnUnit(mobUnit, spawnTile, Team.Player);
        }
        if (spawnTile2 != null)
        {
            unitSpawner.SpawnUnit(mobUnit, spawnTile2, Team.Player);
        }
        if (targetTile != null)
        {
            unitSpawner.SpawnUnit(mobUnit, targetTile, Team.Enemy);
        }

        // 모든 유닛 소환 완료 후 전투 시작
        // BattleManager.OnBattleStart 이벤트가 발동되어
        // 목록에 등록된 모든 유닛의 EnterIdleState()가 일제히 호출된다
        BattleManager.Instance.StartBattle();
    }
}
