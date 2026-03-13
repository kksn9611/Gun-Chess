using System;
using UnityEngine;

public class UnitSpawner : MonoBehaviour
{
    public void SpawnUnit(UnitData data, TileScript targetTile, Team team)
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
        //유닛 컨트롤러 갱신 후 유닛 리스트에 등록
        if (unitObj.TryGetComponent<UnitController>(out UnitController controller))
        {
            controller.Initialize(data, targetTile, team);
            UnitManager.Instance.AddUnitToList(controller, team);
        }

    }
}
