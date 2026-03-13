using UnityEngine;

[CreateAssetMenu(fileName = "UnitData", menuName = "Scriptable Objects/UnitData")]
public class UnitData : ScriptableObject
{
    [Header("기본 정보")]
    public string unitName = "Mob";
    public GameObject unitPrefab; // 유닛 외형

    [Header("기본 전투 스탯")]
    public float maxHp = 100f;     // 최대 체력
    public float maxMp = 50f;      // 최대 마나
    public float att = 10f; // 공격력
    public float def = 20f; // 방어력 (% 데미지 감소)
    
    public float attRange = 1f;  // 공격 사거리 (격자 기준 칸)
    public float attSpd = 1f;  // 공격 속도
    public float moveSpd = 3f;    // 이동 속도
}
