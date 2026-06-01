using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// Attach to projectile prefab. Receives runtime data on fire,
/// handles homing movement, applies damage + status on hit, returns to pool.
/// Supports delayed explosion: sticks to target, explodes after delay.
/// </summary>
public class Projectile : MonoBehaviour
{
    private float damage;
    private float explosionDamage;
    private Team team;
    private ProjectileData data;
    private Transform target;
    private UnitController source; // damage source for lifesteal
    private Vector3 travelDirection; // projectile's incoming direction

    /// <summary>Initialize runtime data and fire toward target.</summary>
    public void Fire(float damage, float explosionDamage, Team team, ProjectileData data, Transform target, UnitController source = null)
    {
        this.damage = damage;
        this.explosionDamage = explosionDamage;
        this.team = team;
        this.data = data;
        this.target = target;
        this.source = source;
        HomingAsync().Forget();
    }

    private async UniTaskVoid HomingAsync()
    {
        float reachTime = data.reachTime;
        if (reachTime <= 0f) reachTime = 0.01f;

        Vector3 spawnPos = transform.position;
        Vector3 initialTarget = (target != null) ? target.position : spawnPos;
        float speed = Vector3.Distance(spawnPos, initialTarget) / reachTime;
        float elapsed = 0f;

        while (elapsed < reachTime)
        {
            if (this == null) return;
            Vector3 targetPos = (target != null) ? target.position : transform.position;
            if (transform.position != targetPos)
                transform.LookAt(targetPos);
            transform.position = Vector3.MoveTowards(transform.position, targetPos, speed * Time.deltaTime);
            elapsed += Time.deltaTime;
            await UniTask.Yield();
        }

        if (target != null)
        {
            transform.position = target.position;
            Vector3 dir = target.position - spawnPos;
            dir.y = 0f;
            travelDirection = dir.sqrMagnitude > 0.001f ? dir.normalized : transform.forward;

            if (data.useExplosion)
            {
                // Stick to target, then explode
                transform.SetParent(target);
                OnHit();
                await UniTask.WaitForSeconds(data.explodeDelay);
                Explode();
                transform.SetParent(null);
                await UniTask.WaitForSeconds(2f); // wait for VFX/sound to finish
            }
            else
            {
                OnHit();
            }
        }

        VfxPoolManager.Instance.Return(data.prefab, gameObject);
    }

    // Direct Hit //

    private void OnHit()
    {
        UnitController unit = target.GetComponentInParent<UnitController>();
        if (unit == null || unit.Stats.CurrentHp <= 0) return;

        if (damage > 0f)
            unit.TakeDamage(damage, source);

        if (data.applyStun)
            unit.CCHandler.ApplyStun(data.stunDuration);
    }

    // Explosion //

    private void Explode()
    {
        Vector3 center = transform.position;
        Vector3 explodeDir = travelDirection; // spreads backwards

        // Spawn explosion VFX or indicator fallback
        if (data.explodeVfxPrefab != null)
        {
            Quaternion vfxRot = (explodeDir.sqrMagnitude > 0.001f)
                ? Quaternion.LookRotation(explodeDir)
                : Quaternion.identity;
            GameObject vfx = Instantiate(data.explodeVfxPrefab, center, vfxRot);
            Destroy(vfx, 3f);
        }
        else
        {
            var indicator = SkillAreaRenderer.Create(data.explodeArea, center, center + explodeDir);
            indicator.ShowForDuration(0.3f).Forget();
        }

        // Sound (2D — no distance falloff, routed to SFX mixer group)
        if (data.explodeSound != null)
        {
            GameObject sfxObj = new GameObject("ExplosionSFX");
            AudioSource src = sfxObj.AddComponent<AudioSource>();
            src.clip = data.explodeSound;
            src.volume = data.explodeSoundVolume;
            src.spatialBlend = 0f;

            AudioMixer mixer = Resources.Load<AudioMixer>("Sound/Mixer");
            if (mixer != null)
            {
                AudioMixerGroup[] groups = mixer.FindMatchingGroups("SFX");
                if (groups.Length > 0)
                    src.outputAudioMixerGroup = groups[0];
            }

            src.Play();
            Destroy(sfxObj, data.explodeSound.length + 0.1f);
        }

        // AoE damage (direction-based for Cone/Laser, position-based for Circle)
        List<UnitController> targets = AreaTargetingUtility.GetTargetsInArea(
            data.explodeArea, center, explodeDir, team);

        foreach (UnitController hit in targets)
        {
            hit.TakeDamage(explosionDamage, source);

            if (data.applyStun)
                hit.CCHandler.ApplyStun(data.stunDuration);
        }
    }
}
