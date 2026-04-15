/// <summary>
/// 시너지 버프가 보정할 수 있는 스탯 종류.
/// SynergyBehavior(StatBoostBehavior)에서 사용한다.
/// </summary>
public enum StatType
{
    Att,        // 공격력 (%)
    Def,        // 방어력 (%)
    AttSpd,     // 공격 속도 (%)
    MaxHp,      // 최대 체력 (%)
    MoveSpd,    // 이동 속도 (%)
    MpGain,     // MP 획득량 (%)
    SkillDmg    // 스킬 데미지 배율 (%)
}
