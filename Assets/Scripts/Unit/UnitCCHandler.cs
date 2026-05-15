using UnityEngine;
/// <summary>
/// Unit CC Timer
/// </summary>
[RequireComponent(typeof(UnitController))]
public class UnitCCHandler : MonoBehaviour
{
    // Stun, Taunt Control //
    public bool IsStunned {get; private set;}
    public UnitController TauntSource { get; private set;}
    private UnitController unit;

    private float stunTimer;
    private float tauntTimer;

public void ApplyStun(float duration)
{   
    // ignore shorter duration
    if (duration <= stunTimer) return;
    stunTimer = duration;
    if (!IsStunned)
    {
        IsStunned = true;
        unit.OnStunApplied();
    }
}

    public void ApplyTaunt(UnitController sourceUnit, float duration)
    {
        TauntSource = sourceUnit;
        tauntTimer = Mathf.Max(tauntTimer, duration);
    }
    private void Update()
    {
        if(IsStunned)
        {
            stunTimer -= Time.deltaTime;
            if(stunTimer <= 0f) 
            {
                IsStunned = false;
                unit.OnStunEnded();
            }
        }
        if(TauntSource != null)
        {
            tauntTimer -= Time.deltaTime;
            if (tauntTimer <= 0f || TauntSource.Stats.CurrentHp <= 0) TauntSource = null;
        }
    }
    /// <summary>
    /// Cleanse (Temp)
    /// </summary>
    public void ClearCC()
    {
        IsStunned = false;
        TauntSource = null;
    }

}
