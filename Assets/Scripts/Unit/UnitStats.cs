using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// Manages unit stats, HP/MP, skills, and synergy buffs.
/// Referenced by UnitController.
/// </summary>
[RequireComponent(typeof(UnitController))]
public class UnitStats : MonoBehaviour
{
    private UnitController unit; // Used for team check in synergy callbacks and behavior.Apply

    // Stats //

    [Header("Unit Data")]
    [SerializeField] private UnitData unitData;
    [SerializeField] private int starLevel;

    [Header("Combat Stats")]
    [SerializeField] private float currentHp;
    [SerializeField] private float currentMp;
    [SerializeField] private float currentAtt;
    [SerializeField] private float currentDef;
    [SerializeField] private float currentAttRange;
    [SerializeField] private float currentAttSpd;
    [SerializeField] private float currentMoveSpd;
    [SerializeField] private float currentMaxHp;
    [SerializeField] private float currentMaxMp;
    [SerializeField] private float mpGainOnAttack;
    [SerializeField] private float mpGainOnHit;
    [SerializeField] private BaseSkill skill; // null = no skill
    [SerializeField] private float currentSkillDmgMul = 1f; // Skill damage multiplier
    [SerializeField] private float currentCritChance = 0.25f; // Critical hit chance
    [SerializeField] private float currentCritDamage = 1.5f;  // Critical hit damage multiplier
    [SerializeField] private float currentLifesteal = 0f;     // Lifesteal ratio (0~1, -> 0% ~ 100%)

    // Synergy //

    [Header("Synergy")]
    [SerializeField] private SynergyState synergyState; // Assigned in Inspector
    private readonly Dictionary<SynergyData, int> appliedSynergyTiers = new Dictionary<SynergyData, int>();

    // Events //

    /// <summary>HP changed (currentHP, maxHP)</summary>
    public event Action<float, float> OnHpChanged;
    public event Action OnHealed;
    /// <summary>MP changed (currentMP, maxMP)</summary>
    public event Action<float, float> OnMpChanged;
    /// <summary>Attack speed changed (newAttSpd)</summary>
    public event Action<float> OnAttSpdChanged;
    

    // Properties //

    public UnitData UnitData       => unitData;
    public int      StarLevel      => starLevel;
    public float    CurrentHp      => currentHp;
    public float    CurrentMp      => currentMp;
    public float    CurrentAtt     => currentAtt;
    public float    CurrentDef     => currentDef;
    public float    CurrentMaxHp   => currentMaxHp;
    public float    CurrentAttRange => currentAttRange;
    public float    CurrentAttSpd  => currentAttSpd;
    public float    CurrentMoveSpd => currentMoveSpd;
    public float    CurrentMaxMp   => currentMaxMp;
    public float    MpGainOnAttack => mpGainOnAttack;
    public float    MpGainOnHit    => mpGainOnHit;
    public BaseSkill Skill         => skill;
    public float    SkillDamageMultiplier => currentSkillDmgMul;
    public float    CurrentCritChance => currentCritChance;
    public float    CurrentCritDamage => currentCritDamage;
    public float    CurrentLifesteal  => currentLifesteal;

    // Initialization //

    private void Awake()
    {
        unit = GetComponent<UnitController>();
    }



    /// <summary>Copy stats from UnitData. Called by UnitController.Initialize().</summary>
    public void Initialize(UnitData data)
    {
        unitData        = data;
        starLevel       = data.starLevel;
        currentAtt      = data.att;
        currentDef      = data.def;
        currentAttRange = data.attRange;
        currentMoveSpd  = data.moveSpd;
        currentMaxHp    = data.maxHp;
        currentMaxMp    = data.maxMp;
        mpGainOnAttack  = data.mpGainOnAttack;
        mpGainOnHit     = data.mpGainOnHit;
        skill           = data.skill;
        currentSkillDmgMul = 1f;
        currentCritChance = data.critChance;
        currentCritDamage = data.critDamage;
        currentLifesteal = 0f;

        SetAttSpd(data.attSpd);
        SetHp(data.maxHp);
        SetMp(0f);
    }

    /// <summary>Reset stats to base values on round transition.</summary>
    public void ResetStats()
    {
        CancelHealOverTime();
        RemoveAllSynergyBuffs();

        currentAtt      = unitData.att;
        currentDef      = unitData.def;
        currentAttRange = unitData.attRange;
        currentMoveSpd  = unitData.moveSpd;
        currentMaxHp    = unitData.maxHp;
        currentMaxMp    = unitData.maxMp;
        mpGainOnAttack  = unitData.mpGainOnAttack;
        mpGainOnHit     = unitData.mpGainOnHit;
        currentSkillDmgMul = 1f;
        currentCritChance = unitData.critChance;
        currentCritDamage = unitData.critDamage;
        currentLifesteal = 0f;

        SetAttSpd(unitData.attSpd);
        SetHp(unitData.maxHp);
        SetMp(0f);
    }

    // HP / MP //

    public void SetHp(float value)
    {
        currentHp = Mathf.Clamp(value, 0f, currentMaxHp);
        OnHpChanged?.Invoke(currentHp, currentMaxHp);
    }
    public void SetMp(float value)
    {
        currentMp = Mathf.Clamp(value, 0f, currentMaxMp);
        OnMpChanged?.Invoke(currentMp, currentMaxMp);
    }
    /// <summary>Gain MP, clamped to maxMp.</summary>
    public void GainMp(float amount)
    {
        if (amount <= 0f) return;
        SetMp(currentMp + amount);
    }

    // Heal //

    private CancellationTokenSource hotCts; // Heal over Time lifecycle

    /// <summary>Instantly heal by amount (clamped to MaxHp).</summary>
    public void ApplyHeal(float amount)
    {
        if (amount <= 0f || currentHp <= 0f) return;
        OnHealed?.Invoke();
        SetHp(currentHp + amount);
    }

    /// <summary>
    /// Heal totalAmount gradually over duration, split across tickCount ticks.
    /// </summary>
    public void ApplyHealOverTime(float totalAmount, float duration, int tickCount)
    {
        if (totalAmount <= 0f || tickCount < 1 || duration <= 0f) return;
        hotCts ??= new CancellationTokenSource();
        HealOverTimeAsync(totalAmount, duration, tickCount, hotCts.Token).Forget();
    }

    /// <summary>Tick loop for ApplyHealOverTime. Stops if the unit dies.</summary>
    private async UniTaskVoid HealOverTimeAsync(float totalAmount, float duration, int tickCount, CancellationToken ct)
    {
        float healPerTick  = totalAmount / tickCount;
        float tickInterval = duration / tickCount;

        for (int i = 0; i < tickCount; i++)
        {
            await UniTask.WaitForSeconds(tickInterval, cancellationToken: ct);
            if (currentHp <= 0f) return; // stop healing a dead unit
            ApplyHeal(healPerTick);
        }
    }

    /// <summary>Cancel any running Heal over Time.</summary>
    private void CancelHealOverTime()
    {
        hotCts?.Cancel();
        hotCts?.Dispose();
        hotCts = null;
    }

    // Attack Speed //

    public void SetAttSpd(float value)
    {
        currentAttSpd = value;
        OnAttSpdChanged?.Invoke(currentAttSpd);
    }

    // Critical Hit //

    /// <summary>Roll crit and return modified damage. Returns original damage on non-crit.</summary>
    public float ApplyCrit(float damage, out bool isCrit)
    {
        isCrit = UnityEngine.Random.value < currentCritChance;
        return isCrit ? damage * currentCritDamage : damage;
    }

    // Skill //

    /// <summary>Whether skill can be cast. True if skill exists and MP >= maxMp.</summary>
    public bool CanCastSkill()
    {
        return skill != null && currentMp >= currentMaxMp;
    }

    // Synergy Event Subscription //

    private void OnEnable()
    {
        if (synergyState != null)
            synergyState.OnSynergyChanged += OnSynergyChanged;
    }

    private void OnDisable()
    {
        if (synergyState != null)
            synergyState.OnSynergyChanged -= OnSynergyChanged;
        CancelHealOverTime();
    }

    // Synergy Buffs //

    /// <summary>Called on SynergyState change. Apply/remove buffs based on tier.</summary>
    private void OnSynergyChanged()
    {
        if (unitData == null || unitData.synergies == null) return;
        if (synergyState == null) return;

        // Only apply synergy buffs to player units
        if (unit.CurrentTeam != Team.Player) return;

        foreach (var synergy in unitData.synergies)
        {
            if (synergy == null) continue;

            int newTierIndex = synergyState.GetActiveTierIndex(synergy);
            appliedSynergyTiers.TryGetValue(synergy, out int oldTierIndex);

            if (!appliedSynergyTiers.ContainsKey(synergy))
                oldTierIndex = -1;

            if (newTierIndex == oldTierIndex) continue;

            // Remove previous tier effects
            if (oldTierIndex >= 0 && oldTierIndex < synergy.tiers.Length)
            {
                var oldBehaviors = synergy.tiers[oldTierIndex].behaviors;
                if (oldBehaviors != null)
                    foreach (var behavior in oldBehaviors)
                        behavior?.Remove(unit);
            }

            // Apply new tier effects
            if (newTierIndex >= 0 && newTierIndex < synergy.tiers.Length)
            {
                var newBehaviors = synergy.tiers[newTierIndex].behaviors;
                if (newBehaviors != null)
                    foreach (var behavior in newBehaviors)
                        behavior?.Apply(unit);
            }

            appliedSynergyTiers[synergy] = newTierIndex;
        }
    }

    /// <summary>Remove all synergy buffs. Called on round reset.</summary>
    private void RemoveAllSynergyBuffs()
    {
        if (unitData == null || unitData.synergies == null) return;

        foreach (var pair in appliedSynergyTiers)
        {
            SynergyData synergy = pair.Key;
            int tierIndex = pair.Value;

            if (tierIndex >= 0 && synergy != null && tierIndex < synergy.tiers.Length)
            {
                var behaviors = synergy.tiers[tierIndex].behaviors;
                if (behaviors != null)
                    foreach (var behavior in behaviors)
                        behavior?.Remove(unit);
            }
        }
        appliedSynergyTiers.Clear();
    }

    // Stat Modifiers //

    /// <summary>
    /// Modify stat by percentage, based on UnitData base values.
    /// </summary>
    public void ApplyStatModifier(StatType stat, float percentDelta)
    {
        switch (stat)
        {
            case StatType.Att:
                currentAtt += unitData.att * (percentDelta / 100f);
                break;
            case StatType.Def:
                currentDef += unitData.def * (percentDelta / 100f);
                break;
            case StatType.AttSpd:
                SetAttSpd(currentAttSpd + unitData.attSpd * (percentDelta / 100f));
                break;
            case StatType.MaxHp:
                float hpDelta = unitData.maxHp * (percentDelta / 100f);
                currentMaxHp += hpDelta;
                SetHp(currentHp + hpDelta); // Scale current HP proportionally
                break;
            case StatType.MoveSpd:
                currentMoveSpd += unitData.moveSpd * (percentDelta / 100f);
                break;
            case StatType.MpGain:
                mpGainOnAttack += unitData.mpGainOnAttack * (percentDelta / 100f);
                mpGainOnHit    += unitData.mpGainOnHit    * (percentDelta / 100f);
                break;
            case StatType.SkillDmg:
                currentSkillDmgMul += percentDelta / 100f;
                break;
            case StatType.CritChance:
                currentCritChance += percentDelta / 100f;
                break;
            case StatType.CritDamage:
                currentCritDamage += unitData.critDamage * (percentDelta / 100f);
                break;
            case StatType.Lifesteal:
                currentLifesteal += percentDelta / 100f;
                break;
        }
    }
}
