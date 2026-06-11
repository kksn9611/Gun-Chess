using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Temporary debug button that spawns a unit on the first empty bench slot.
/// Assign UnitData and UnitSpawner in Inspector.
/// </summary>
public class DebugSpawnButton : MonoBehaviour
{
    [SerializeField] private UnitData unitData;
    [SerializeField] private UnitSpawner unitSpawner;
    [SerializeField] private Button spawnButton;

    private void Awake()
    {
        if (spawnButton != null)
            spawnButton.onClick.AddListener(SpawnUnit);
    }

    private void SpawnUnit()
    {
        if (unitData == null || unitSpawner == null)
        {
            Debug.LogWarning("[DebugSpawn] UnitData or UnitSpawner not assigned");
            return;
        }

        BenchTileScript slot = BenchManager.Instance.GetEmptySlot();
        if (slot == null)
        {
            Debug.LogWarning("[DebugSpawn] No empty bench slot");
            return;
        }

        // Take a copy from the shared pool before spawning
        if (!UnitPool.Instance.TryAcquire(unitData))
        {
            Debug.Log("[DebugSpawn] Pool empty");
            return;
        }

        UnitController unit = unitSpawner.SpawnUnit(unitData, slot, Team.Player, false);
        if (unit != null)
        {
            BenchManager.Instance.AddUnit(unit, slot);
        }
        else
        {
            UnitPool.Instance.Return(unitData); // refund on spawn failure
        }
    }

    private void OnDestroy()
    {
        if (spawnButton != null)
            spawnButton.onClick.RemoveListener(SpawnUnit);
    }
}
