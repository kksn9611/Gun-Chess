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

    /// <summary>Initialize runtime data and fire toward target.</summary>
    public void Fire(float damage, float explosionDamage, Team team, ProjectileData data, Transform target)
    {
        this.damage = damage;
        this.explosionDamage = explosionDamage;
        this.team = team;
        this.data = data;
        this.target = target;
        HomingAsync().Forget();
    }

    private async UniTaskVoid HomingAsync()
    {
        float reachTime = data.reachTime;
        if (reachTime <= 0f) reachTime = 0.01f;

        Vector3 initialTarget = (target != null) ? target.position : transform.position;
        float speed = Vector3.Distance(transform.position, initialTarget) / reachTime;
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
            unit.TakeDamage(damage);

        if (data.applyStun)
            unit.CCHandler.ApplyStun(data.stunDuration);
    }

    // Explosion //

    private void Explode()
    {
        Vector3 center = transform.position;

        // Spawn explosion VFX or indicator fallback
        if (data.explodeVfxPrefab != null)
        {
            GameObject vfx = Instantiate(data.explodeVfxPrefab, center, Quaternion.identity);
            Destroy(vfx, 3f);
        }
        else
        {
            AreaShapeData circleShape = new AreaShapeData
            {
                shapeType = AreaShapeType.Circle,
                radius = data.explodeRadius
            };
            var indicator = SkillAreaRenderer.Create(circleShape, center, center);
            indicator.ShowForDuration(0.3f).Forget();
        }

        // Explosion Sound
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

        // AoE damage
        List<UnitController> targets = AreaTargetingUtility.GetTargetsInCircle(center, data.explodeRadius, team);

        foreach (UnitController hit in targets)
        {
            hit.TakeDamage(explosionDamage);

            if (data.applyStun)
                hit.CCHandler.ApplyStun(data.stunDuration);
        }
    }
}
