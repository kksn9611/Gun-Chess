using UnityEngine;

[CreateAssetMenu(fileName = "GoldPerRound", menuName = "Scriptable Objects/Synergy/GoldPerRoundBehavior")]
public class GoldPerRoundBehavior : SynergyBehavior
{
    [Tooltip("Gold granted to the player each round while this tier is active")]
    public int goldPerRound = 2;

    // Round Income //
    /// <summary>Grant this tier's gold to the player.</summary>
    public void GrantIncome()
    {
        if (PlayerManager.Instance != null) PlayerManager.Instance.AddGold(goldPerRound);
    }

    // Economy is global per-round, not a per-unit buff.
    public override void Apply(UnitController unit) { }
    public override void Remove(UnitController unit) { }
}
