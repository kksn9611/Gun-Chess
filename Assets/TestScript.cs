using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class TestScript : MonoBehaviour
{
    public UnitData mobUnit;
    [SerializeField] private UnitSpawner unitSpawner;

    IEnumerator Start()
    {
        // HexGridLayout이 타일을 다 생성할 때까지 대기
        yield return new WaitForSeconds(0.2f);

        // PlayerZone (y < 4): 플레이어 유닛 배치
        SpawnAt(0, 0, Team.Player);
        SpawnAt(2, 1, Team.Player);
        SpawnAt(4, 2, Team.Player);

        // EnemyZone (y >= 4): 적 유닛 배치
        SpawnAt(1, 5, Team.Enemy);
        SpawnAt(3, 6, Team.Enemy);
    }

    private void SpawnAt(int x, int y, Team team)
    {
        TileScript tile = TileManager.Instance.GetTile(new Vector2Int(x, y));
        if (tile != null)
            unitSpawner.SpawnUnit(mobUnit, tile, team);
        else
            Debug.LogWarning($"[TestScript] 타일 ({x},{y}) 없음");
    }
    
    /// <summary>
    /// space로 전투 시작
    /// </summary>
    public void OnSpace(InputAction.CallbackContext context)
    {
        if(context.performed)
        {
            if (BattleManager.Instance != null && BattleManager.Instance.CurrentPhase == BattleManager.Phase.Preparation)
            {
                Debug.Log("[TestScript] 전투 시작!");
                BattleManager.Instance.StartBattle();
            }
        }
    }
}
