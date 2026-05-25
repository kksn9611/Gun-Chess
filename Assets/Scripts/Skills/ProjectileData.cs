using UnityEngine;

/// <summary>
/// Static configuration for a skill projectile.
/// Referenced by ProjectileSkill to define what gets fired.
/// </summary>
[CreateAssetMenu(fileName = "ProjectileData", menuName = "Scriptable Objects/Skill/ProjectileData")]
public class ProjectileData : ScriptableObject
{
    [Header("Projectile")]
    [Tooltip("Projectile prefab (must have Projectile component)")]
    public GameObject prefab;
    public float reachTime = 0.3f;
    public int poolSize = 3;

    [Header("Status Effect")]
    public bool applyStun;
    public float stunDuration;

    [Header("Explosion")]
    [Tooltip("Stick to target and explode after delay")]
    public bool explodeOnDelay;
    public float explodeDelay = 2f;
    public float explodeRadius = 2f;
    [Tooltip("Explosion damage multiplier relative to the projectile's damage")]
    public float explodeDamageMultiplier = 1f;
    [Tooltip("Explosion VFX prefab (spawned at explosion point)")]
    public GameObject explodeVfxPrefab;
}
