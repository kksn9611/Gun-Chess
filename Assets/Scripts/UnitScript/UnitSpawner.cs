using UnityEngine;

public class UnitSpawner : MonoBehaviour
{
    public static UnitSpawner Instance { get; private set; }
    private void Awake()
    {
        // 싱글톤 
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void SpawnUnit(UnitData data, TileScript targetTile)
    {
        // 배치 검사
        if (targetTile == null || targetTile.IsOccupied)
        {
            Debug.LogWarning("배치 불가");
            return;
        }

        // 유닛 생성
        GameObject unitObj = 
            Instantiate(data.unitPrefab, targetTile.transform.position, Quaternion.identity);
        // 타일 채우기
        targetTile.IsOccupied = true;

        //유닛 컨트롤러 갱신
        if (unitObj.TryGetComponent<UnitController>(out UnitController controller))
        {
            controller.Initialize(data, targetTile);
        }

    }
}
