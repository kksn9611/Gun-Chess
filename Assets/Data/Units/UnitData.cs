using UnityEngine;

[CreateAssetMenu(fileName = "UnitData", menuName = "Scriptable Objects/UnitData")]
public class UnitData : ScriptableObject
{
    [Header("기본 정보")]
    public string unitName = "Mob";
    public GameObject unitPrefab; // 유닛 외형
    public int cost = 1;

    [Header("승급")]
    [Tooltip("현재 유닛의 성급 (1~3)")]
    public int starLevel = 1;
    [Tooltip("3합 시 변환될 상위 유닛. null이면 최종 단계")]
    public UnitData upgradedUnit;

    [Header("기본 전투 스탯")]
    public float maxHp = 100f;     // 최대 체력
    public float maxMp = 50f;      // 최대 마나
    public float att = 10f; // 공격력
    public float def = 20f; // 방어력 (% 데미지 감소)
    
    public float attRange = 1f;  // 공격 사거리 (격자 기준 칸)
    public float attSpd = 1f;  // 공격 속도
    public float moveSpd = 3f;    // 이동 속도

    [Header("시너지")]
    [Tooltip("이 유닛이 소속된 시너지 목록 (여러 시너지에 소속 가능)")]
    public SynergyData[] synergies;

    [Header("스킬")]
    [Tooltip("이 유닛이 사용하는 스킬. null이면 스킬 없음")]
    public BaseSkill skill;

    [Tooltip("공격 시 획득하는 MP")]
    public float mpGainOnAttack = 10f;

    [Tooltip("피격 시 획득하는 MP")]
    public float mpGainOnHit = 2f;
}
