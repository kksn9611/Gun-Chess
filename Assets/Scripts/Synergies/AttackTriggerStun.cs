using UnityEngine;

[CreateAssetMenu(fileName = "AttackTriggerStun", menuName = "Scriptable Objects/Synergy/EventTriggerSynergy/AttackTriggerStun")]
public class AttackTriggerStun : EventTriggerBehavior
{   [Header("Stun Setting")]
    [Range(0f, 1f)] 
    [Tooltip("0 = 0%, 1 = 100%")]
    public float stunChance = 0.1f; // 10%
    public float stunDuration = 1.5f;

    protected override void ExecuteAttackEffect(UnitController attacker, UnitController target)
    {
        if (target == null || target.Stats.CurrentHp <= 0) return;

        if (Random.value < stunChance)
        {
            target.CCHandler.ApplyStun(stunDuration);
        }
    }
}
