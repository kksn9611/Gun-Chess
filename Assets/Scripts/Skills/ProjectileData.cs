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
    [Tooltip("Enable explosion: sticks to target and explodes after delay")]
    public bool useExplosion;
    public float explodeDelay = 2f;
    public float explodeRadius = 2f;
    [Tooltip("Explosion VFX prefab (spawned at explosion point)")]
    public GameObject explodeVfxPrefab;

    [Header("Sound")]
    public AudioClip explodeSound;
    [Range(0f, 1f)] public float explodeSoundVolume = 1f;
}
