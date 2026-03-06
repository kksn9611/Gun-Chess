using UnityEngine;

[CreateAssetMenu(fileName = "UnitData", menuName = "Scriptable Objects/UnitData")]
public class UnitData : ScriptableObject
{
    [Header("기본 정보")]
    public string unitName = "모브";
    public GameObject unitPrefab; // 유닛 외형

    [Header("전투 스탯")]
    public float maxHealth = 100f;     // 최대 체력
    public float attackDamage = 10f;   // 공격력
    public float attackRange = 1f;   // 공격 사거리 (격자 기준 1칸~2칸 등)
    public float attackCooldown = 1f;  // 공격 속도 (몇 초에 한 번 때릴지)
    public float moveSpeed = 3f;       // 이동 속도
}
