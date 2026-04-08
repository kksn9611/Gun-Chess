using System;
using UnityEngine;

public class UnitSpawner : MonoBehaviour
{
    /// <summary>
    /// 유닛을 생성하여 타일(전장 헥스 또는 벤치) 위에 배치한다.
    /// BaseTile을 받으므로 TileScript(전장)와 BenchTileScript(벤치) 모두 사용 가능하다.
    /// register : true면 UnitManager에 즉시 등록하여 전투에 참여시킨다.
    /// </summary>
    public UnitController SpawnUnit(UnitData data, BaseTile targetTile, Team team, bool register = true)
    {
        // 배치 검사
        if (targetTile == null || targetTile.IsOccupied)
        {
            Debug.LogWarning("배치 불가");
            return null;
        }

        // 유닛 생성
        GameObject unitObj =
            Instantiate(data.unitPrefab, targetTile.transform.position, Quaternion.identity);
        // 유닛 컨트롤러 갱신 후 유닛 리스트에 등록
        if (unitObj.TryGetComponent<UnitController>(out UnitController controller))
        {
            controller.Initialize(data, targetTile, team);
            if (register)
                UnitManager.Instance.AddUnit(controller, team);
            return controller;
        }

        Debug.LogError($"{data.unitPrefab.name}의 UnitContorller가 없음, 유닛 생성 실패");
        Destroy(unitObj);
        return null;
    }
}
