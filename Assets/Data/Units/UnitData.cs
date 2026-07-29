using UnityEngine;

[CreateAssetMenu(fileName = "UnitData", menuName = "Scriptable Objects/UnitData")]
public class UnitData : ScriptableObject
{
    [Header("Basic Info")]
    public string unitName;
    public GameObject unitPrefab; // Unit visual
    public Sprite portrait;       // Shop/UI portrait
    public int cost = 1;

    [Header("Upgrade")]
    [Tooltip("Current star level (1~3)")]
    public int starLevel = 1;
    [Tooltip("Unit to upgrade into when 3 copies merge. null = final tier")]
    public UnitData upgradeUnit;

    [Header("Base Combat Stats")]
    public float maxHp = 100f;     // Max HP
    public float maxMp = 50f;      // Max MP
    public float att = 10f; // Attack power
    public float def = 20f; // Defense (% damage reduction)

    public float attRange = 1f;  // Attack range (hex tiles)
    public float attSpd = 1f;  // Attack speed
    public float moveSpd = 3f;    // Movement speed
    public float critChance = 0.25f; // Critical hit chance (0~1)
    public float critDamage = 1.5f;  // Critical hit damage multiplier

    [Header("Synergy")]
    [Tooltip("Synergies this unit belongs to (can belong to multiple)")]
    public SynergyData[] synergies;

    [Header("Trail / Pool")]
    [Tooltip("prefab for normal attacks")]
    public TrailRenderer bulletTrailPrefab;
    [Tooltip("Projectile prefab for skill (null if skill has no projectile)")]
    public GameObject skillProjectilePrefab;
    [Tooltip("Pre-warm count for bullet trail pool")]
    public int poolSize = 5;
    [Tooltip("Pre-warm count for skill trail pool")]
    public int skillPoolSize = 3;

    [Header("Skill")]
    [Tooltip("Skill used by this unit. null = no skill")]
    public BaseSkill skill;

    [Tooltip("MP gained on attack")]
    public float mpGainOnAttack = 10f;

    [Tooltip("MP gained on being hit")]
    public float mpGainOnHit = 2f;
}
