using UnityEngine;

/// <summary>
/// Every event trigger synergy's parent class.
/// </summary>
public abstract class EventTriggerBehavior : SynergyBehavior
{
    [Header("Trigger Condition")]
    public bool triggerOnAttack = true;
    public bool triggerOnDamaged = false;

    public override void Apply(UnitController unit)
    {
        // Subscribe in inspector
        if (triggerOnAttack) unit.OnAttackHit += OnAttackTriggered;
        if (triggerOnDamaged) unit.OnBeforeTakeDamage += OnDamagedTriggered;
    }

    public override void Remove(UnitController unit)
    {
        // When disable synergy, unsubscribing from C# events.
        if (triggerOnAttack) unit.OnAttackHit -= OnAttackTriggered;
        if (triggerOnDamaged) unit.OnBeforeTakeDamage -= OnDamagedTriggered;
    }

    // Recieve trigger inside, toss to child class
    private void OnAttackTriggered(UnitController attacker, UnitController target, float damage)
    {
        ExecuteAttackEffect(attacker, target);
    }

    private void OnDamagedTriggered(UnitController victim, float damage)
    {
        ExecuteDamageEffect(victim, damage);
    }

    // Must be implemented in child class
    protected virtual void ExecuteAttackEffect(UnitController attacker, UnitController target) { }
    protected virtual void ExecuteDamageEffect(UnitController victim, float damage) { }
}