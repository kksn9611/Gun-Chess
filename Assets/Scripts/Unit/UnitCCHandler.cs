using UnityEngine;
/// <summary>
/// Unit CC Timer
/// </summary>
[RequireComponent(typeof(UnitController))]
public class UnitCCHandler : MonoBehaviour
{
    // Stun, Taunt Control //
    public bool IsStunned {get; private set;}
    public Transform stunTransform;
    public UnitController TauntSource { get; private set;}
    private UnitController unit;
    [SerializeField] private GameObject stunVfxPrefab;
    private GameObject stunVfxInstance;
    private float stunTimer;
    private float tauntTimer;

    private void Awake()
    {
        unit = GetComponent<UnitController>();
        stunVfxInstance = Instantiate(stunVfxPrefab, stunTransform);
        stunVfxInstance.SetActive(false);
    }

public void ApplyStun(float duration)
{
    // ignore CC on dead units (e.g. stun applied after a lethal hit)
    if (unit.AI.CurrentState == UnitState.Dead) return;
    // ignore shorter duration
    if (duration <= stunTimer) return;
    stunTimer = duration;
    if (!IsStunned)
    {
        IsStunned = true;
        if (stunVfxInstance == null) stunVfxInstance = Instantiate(stunVfxPrefab, stunTransform);
        stunVfxInstance.SetActive(true);
        unit.OnStunApplied();
    }
}

    public void ApplyTaunt(UnitController sourceUnit, float duration)
    {
        if (unit.AI.CurrentState == UnitState.Dead) return; // ignore CC on dead units
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
                stunVfxInstance.SetActive(false);
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
    /// Clear all CC: reset timers, flags, and hide the stun VFX.
    /// Called on cleanse and on death.
    /// </summary>
    public void ClearCC()
    {
        stunTimer = 0f;
        tauntTimer = 0f;
        IsStunned = false;
        TauntSource = null;
        if (stunVfxInstance != null) stunVfxInstance.SetActive(false);
    }

}
