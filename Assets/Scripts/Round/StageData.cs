using UnityEngine;

/// <summary>
/// ScriptableObject defining enemy spawn info per stage.
/// Configure enemy unit types and spawn coordinates in Inspector.
/// </summary>
[CreateAssetMenu(fileName = "StageData", menuName = "Scriptable Objects/StageData")]
public class StageData : ScriptableObject
{
    [Header("Enemy Composition")]
    public EnemySpawnInfo[] enemies;

    [Header("Enemy Modifiers")]
    [Tooltip("Percent stat boosts applied to every enemy spawned this stage")]
    public StatBoostEntry[] enemyBuffs;
}

/// <summary>
/// Spawn info for a single enemy unit: which unit at which coordinate.
/// </summary>
[System.Serializable]
public class EnemySpawnInfo
{
    public UnitData unitData;
    public Vector2Int spawnCoordinate;
}
