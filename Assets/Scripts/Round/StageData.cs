using UnityEngine;

/// <summary>
/// 스테이지별 적 유닛 스폰 정보를 정의하는 ScriptableObject.
/// Inspector에서 적 유닛 종류와 스폰 좌표를 설정한다.
/// </summary>
[CreateAssetMenu(fileName = "StageData", menuName = "Scriptable Objects/StageData")]
public class StageData : ScriptableObject
{
    [Header("적 유닛 구성")]
    public EnemySpawnInfo[] enemies;
}

/// <summary>
/// 적 유닛 1기의 스폰 정보: 어떤 유닛을 어느 좌표에 배치할지 정의한다.
/// </summary>
[System.Serializable]
public class EnemySpawnInfo
{
    public UnitData unitData;
    public Vector2Int spawnCoordinate;
}
