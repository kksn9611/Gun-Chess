using UnityEngine;

/// <summary>
/// Chance-based shield gain when taking damage. Requires triggerOnDamaged = true.
/// Shield amount = victim max HP * shieldPercentMaxHp.
/// The shield VFX is the persistent effect on UnitVisuals (auto-shown via OnShieldChanged).
/// Note: fires before the hit's shield absorption, so the granted shield softens the triggering hit.
/// </summary>
[CreateAssetMenu(fileName = "DamageTriggerShield", menuName = "Scriptable Objects/Synergy/EventTriggerSynergy/DamageTriggerShield")]
public class DamageTriggerShield : EventTriggerBehavior
{
    [Header("Shield Setting")]
    [Range(0f, 1f)]
    [Tooltip("0 = 0%, 1 = 100%")]
    public float shieldChance = 0.1f; // 10%
    [Tooltip("Shield as a fraction of the victim's max HP (0.1 = 10%)")]
    public float shieldPercentMaxHp = 0.1f;

    protected override void ExecuteDamageEffect(UnitController victim, float damage)
    {
        if (victim == null || victim.Stats.CurrentHp <= 0) return;

        if (Random.value < shieldChance)
        {
            float amount = victim.Stats.CurrentMaxHp * shieldPercentMaxHp;
            victim.Stats.ApplyShield(amount); // OnShieldChanged auto-activates UnitVisuals.shieldEffect
        }
    }
}
