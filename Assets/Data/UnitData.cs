using UnityEngine;

[CreateAssetMenu(fileName = "UnitData", menuName = "Scriptable Objects/UnitData")]
public class UnitData : ScriptableObject
{
    [Header("기본 정보")]
    public string unitName = "모브";
    public GameObject unitPrefab; // 유닛 외형

    [Header("전투 스탯")]
    public float maxHp = 100f;     // 최대 체력
    public float maxMp = 50f;
    public float attackDamage = 10f;   // 공격력
    public float attackRange = 1f;   // 공격 사거리 (격자 기준 1칸)
    public float attackSpeed = 1f;  // 공격 속도
    public float moveSpeed = 3f;       // 이동 속도
}
