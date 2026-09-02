/// <summary>
/// Stat types that synergy buffs can modify.
/// Used by SynergyBehavior (StatBoostBehavior).
/// </summary>
public enum StatType
{
    Att,        // Attack power (%)
    Def,        // Defense (%)
    AttSpd,     // Attack speed (%)
    MaxHp,      // Max HP (%)
    MoveSpd,    // Move speed (%)
    MpGain,     // MP gain (%)
    SkillDmg,   // Skill damage multiplier (%)
    CritChance, // Critical hit chance (%)
    CritDamage, // Critical hit damage multiplier (%)
    Lifesteal   // Lifesteal (% of damage dealt healed)
}

public static class StatTypeExtensions
{
    public static string ToKorean(this StatType statType)
    {
        return statType switch
        {
            StatType.Att => "공격력",
            StatType.Def => "방어력",
            StatType.AttSpd => "공격속도",
            StatType.MaxHp => "최대체력",
            StatType.MoveSpd => "이동속도",
            StatType.MpGain => "마나획득",
            StatType.SkillDmg => "스킬배율",
            StatType.CritChance => "치명타율",
            StatType.CritDamage => "치명피해",
            StatType.Lifesteal => "생명흡수",
            _ => "알 수 없음" // safe guard
        };
    }
}