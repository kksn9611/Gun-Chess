using UnityEngine;

/// <summary>
/// Every event trigger synergy's parent class.
/// </summary>
public abstract class EventTriggerBehavior : SynergyBehavior
{
    [Header("Trigger Condition")]
    public bool triggerOnAttack = true;
    public bool triggerOnSkill = false;
    public bool triggerOnDamaged = false;

    public override void Apply(UnitController unit)
    {
        // Subscribe in inspector
        if (triggerOnAttack) unit.OnAttackHit += OnAttackTriggered;
        if (triggerOnSkill) unit.OnSkillHit += OnSkillTriggered;
        if (triggerOnDamaged) unit.OnBeforeTakeDamage += OnDamagedTriggered;
    }

    public override void Remove(UnitController unit)
    {
        // When disable synergy, unsubscribing from C# events.
        if (triggerOnAttack) unit.OnAttackHit -= OnAttackTriggered;
        if (triggerOnSkill) unit.OnSkillHit -= OnSkillTriggered;
        if (triggerOnDamaged) unit.OnBeforeTakeDamage -= OnDamagedTriggered;
    }

    // Recieve trigger inside, toss to child class
    private void OnAttackTriggered(UnitController attacker, UnitController target, float damage)
    {
        ExecuteAttackEffect(attacker, target);
    }

    private void OnSkillTriggered(UnitController caster, UnitController target, float damage)
    {
        ExecuteSkillEffect(caster, target);
    }

    private void OnDamagedTriggered(UnitController victim, float damage)
    {
        ExecuteDamageEffect(victim, damage);
    }

    // Must be implemented in child class
    protected virtual void ExecuteAttackEffect(UnitController attacker, UnitController target) { }
    // Skill hits reuse the attack effect by default; override for skill-specific behavior.
    protected virtual void ExecuteSkillEffect(UnitController caster, UnitController target) { ExecuteAttackEffect(caster, target); }
    protected virtual void ExecuteDamageEffect(UnitController victim, float damage) { }
}