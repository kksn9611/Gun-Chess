using System;
using UnityEngine;

/// <summary>
/// Player gold, EXP, level.
/// </summary>
public class PlayerManager : MonoBehaviour
{
    public static PlayerManager Instance { get; private set; }

    // EXP required to advance from level N to N+1
    // Index = current level. Max level = expTable.Length + 1.
    private static readonly int[] expTable =
    {
        2,   // 1 -> 2
        2,   // 2 -> 3
        6,   // 3 -> 4
        10,  // 4 -> 5
        20,  // 5 -> 6
        36,  // 6 -> 7
        48,  // 7 -> 8
        76,  // 8 -> 9
        84,  // 9 -> 10
    };

    [Header("Starting Values")]
    [SerializeField] private int startingGold = 3;
    [SerializeField] private int startingLevel = 1;

    [Header("Round Income")]
    [SerializeField] private int baseTurnGold = 2; // base gold granted each turn

    [Header("Interest")]
    [SerializeField] private int goldPerInterest = 10; // 1 interest per this much gold held
    [SerializeField] private int interestCap = 50;     // gold above this earns no extra interest

    [Header("State (Read-Only)")]
    [SerializeField] private int gold;          // Current gold
    [SerializeField] private int currentLevel;  // Current player level
    [SerializeField] private int currentExp;    // EXP accumulated toward next level

    public int Gold => gold;
    public int BaseTurnGold => baseTurnGold; // flat gold granted each turn
    public int CurrentLevel => currentLevel;
    public int CurrentExp => currentExp;
    /// <summary>EXP required to reach next level; 0 if at max level.</summary>
    public int ExpToNextLevel => IsMaxLevel ? 0 : expTable[currentLevel - 1];
    public int MaxLevel => expTable.Length + 1;
    public bool IsMaxLevel => currentLevel >= MaxLevel;


    // Events //
    public static event Action<int> OnGoldChanged;          // Fires with new gold amount
    public static event Action<int, int> OnExpChanged;      // Fires with (currentExp, expToNext)
    public static event Action<int> OnLevelChanged;         // Fires with new level


    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        gold = startingGold;
        currentLevel = Mathf.Clamp(startingLevel, 1, MaxLevel);
        currentExp = 0;
    }

    private void Start()
    {
        // Broadcast initial state
        OnGoldChanged?.Invoke(gold);
        OnLevelChanged?.Invoke(currentLevel);
        OnExpChanged?.Invoke(currentExp, ExpToNextLevel);
    }


    // Gold //

    /// <summary>Add gold (clamped to >= 0).</summary>
    public void AddGold(int amount)
    {
        if (amount == 0) return;
        gold = Mathf.Max(0, gold + amount);
        OnGoldChanged?.Invoke(gold);
    }

    /// <summary>Try to spend gold. Returns false if insufficient.</summary>
    public bool TrySpendGold(int amount)
    {
        if (amount < 0)
        {
            return false;
        }
        if (gold < amount) return false;

        gold -= amount;
        OnGoldChanged?.Invoke(gold);
        return true;
    }


    /// <summary>Grant the flat base gold for the turn and return the amount granted.</summary>
    public int GrantTurnGold()
    {
        if (baseTurnGold > 0) AddGold(baseTurnGold);
        return baseTurnGold;
    }


    // Interest //

    /// <summary>Interest for the current gold: 1 per goldPerInterest held, counting up to interestCap.</summary>
    public int CalculateInterest()
    {
        if (goldPerInterest <= 0) return 0;
        int counted = Mathf.Min(gold, interestCap); // only gold up to the cap earns interest
        return counted / goldPerInterest;
    }

    /// <summary>Grant interest based on current gold and return the amount granted.</summary>
    public int GrantInterest()
    {
        int interest = CalculateInterest();
        if (interest > 0) AddGold(interest);
        return interest;
    }


    // EXP / Level //

    /// <summary>Add EXP; auto level-up while threshold met. Ignored at max level.</summary>
    public void AddExp(int amount)
    {
        if (amount <= 0 || IsMaxLevel) return;

        currentExp += amount;

        // Multi-level gain handled by loop
        while (!IsMaxLevel && currentExp >= ExpToNextLevel)
        {
            currentExp -= ExpToNextLevel;
            currentLevel++;
            OnLevelChanged?.Invoke(currentLevel);
        }

        if (IsMaxLevel) currentExp = 0;

        OnExpChanged?.Invoke(currentExp, ExpToNextLevel);
    }
}
