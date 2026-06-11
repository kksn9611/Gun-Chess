using System;
using UnityEngine;
using System.Collections.Generic;


public struct ShopSlot
{
    public UnitData currnetUnit;
    public bool isPurchased;

}
public class ShopManager : MonoBehaviour
{
    [SerializeField] private ProbabilityTable probabilitySO;
    [SerializeField] private UnitPool unitPool;
    [SerializeField] private PlayerManager player;
    [SerializeField] private UnitSpawner unitSpawner;

    [SerializeField] private int rerollCost = 2;
    [SerializeField] private int freeRerolls; // free rerolls used before spending gold

    [SerializeField] private ShopSlot[] currnetShop =  new ShopSlot[5];
    public List<UnitData> shopList;

    private bool isLocked; // keep shop across round refresh

    public bool IsLocked => isLocked;
    public int FreeRerolls => freeRerolls;


    // Events //
    public event Action OnShopChanged;       // Slots rerolled or a unit purchased
    public event Action<bool> OnLockChanged;  // Lock toggled (new state)
    public event Action<int> OnFreeRerollChanged; // Free reroll count changed


    private void Start()
    {
        RollShop(); // initial shop
    }


    // Reroll //

    /// <summary>Reroll all slots: consume a free reroll if any, otherwise spend gold.</summary>
    public void Reroll()
    {
        if (freeRerolls > 0)
        {
            freeRerolls--;
            OnFreeRerollChanged?.Invoke(freeRerolls);
        }
        else if (!player.TrySpendGold(rerollCost))
        {
            Debug.Log("[Shop] Not enough gold to reroll");
            return;
        }
        RollShop();
    }

    /// <summary>Grant free rerolls that bypass the gold cost.</summary>
    public void AddFreeReroll(int amount)
    {
        if (amount <= 0) return;
        freeRerolls += amount;
        OnFreeRerollChanged?.Invoke(freeRerolls);
    }

    /// <summary>Free reroll on round transition; skipped while locked, then unlocked.</summary>
    public void RefreshForNewRound()
    {
        if (isLocked)
        {
            SetLock(false); // lock holds for one round only
            return;
        }
        RollShop();
    }

    /// <summary>Return any reserved slots, then fill every slot with a fresh weighted roll.</summary>
    private void RollShop()
    {
        ReturnReservedUnits();

        int level = player.CurrentLevel;
        shopList.Clear();
        for (int i = 0; i < currnetShop.Length; i++)
        {
            int cost = probabilitySO.RollCostTier(level);
            UnitData unit = unitPool.GetRandomAvailableUnit(cost);
            if (unit != null && !unitPool.TryAcquire(unit)) unit = null; // reserve the shown copy

            currnetShop[i].currnetUnit = unit; // null if tier depleted
            currnetShop[i].isPurchased = false;
            shopList.Add(unit);
        }
        OnShopChanged?.Invoke();
    }

    /// <summary>Return every still-shown (unpurchased) copy to the pool and clear the slots.</summary>
    private void ReturnReservedUnits()
    {
        for (int i = 0; i < currnetShop.Length; i++)
        {
            if (currnetShop[i].currnetUnit == null) continue;
            unitPool.Return(currnetShop[i].currnetUnit);
            currnetShop[i].currnetUnit = null;
        }
    }


    // Purchase //

    /// <summary>Buy the unit in a slot: pay gold, take a pool copy, place on bench. False on any failure.</summary>
    public bool Purchase(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= currnetShop.Length) return false;

        UnitData unit = currnetShop[slotIndex].currnetUnit;
        if (unit == null) return false; // empty or already bought

        // Need a free bench slot before spending anything
        BenchTileScript benchSlot = BenchManager.Instance.GetEmptySlot();
        if (benchSlot == null)
        {
            Debug.Log("[Shop] No empty bench slot");
            return false;
        }

        // Pay (copy stays reserved in the slot on failure)
        if (!player.TrySpendGold(unit.cost))
        {
            Debug.Log("[Shop] Not enough gold");
            return false;
        }

        // The copy was already reserved when it appeared; just spawn it onto the bench
        UnitController spawned = unitSpawner.SpawnUnit(unit, benchSlot, Team.Player, false);
        if (spawned == null)
        {
            unitPool.Return(unit);     // spawn failed -> release the reserved copy
            player.AddGold(unit.cost); // refund
            currnetShop[slotIndex].currnetUnit = null;
            OnShopChanged?.Invoke();
            return false;
        }
        BenchManager.Instance.AddUnit(spawned, benchSlot);

        // Consume the slot (copy now owned by the bench unit, not the pool)
        currnetShop[slotIndex].currnetUnit = null;
        currnetShop[slotIndex].isPurchased = true;
        OnShopChanged?.Invoke();
        return true;
    }


    // Lock //

    /// <summary>Toggle the shop lock (prevents the next round refresh).</summary>
    public void ToggleLock() => SetLock(!isLocked);

    /// <summary>Set the shop lock state.</summary>
    public void SetLock(bool locked)
    {
        if (isLocked == locked) return;
        isLocked = locked;
        OnLockChanged?.Invoke(isLocked);
    }

}
