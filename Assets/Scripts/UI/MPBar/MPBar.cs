using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// MP bar with same structure as HPBar.
/// Subscribes to OnMpChanged event to update fill.
/// </summary>
public class MPBar : MonoBehaviour
{
    [SerializeField] private Image fill;

    [SerializeField] private Vector3 screenOffset = new Vector3(0f, -9f, 0f); // Offset below HP bar (screen pixels)

    private UnitController targetUnit;
    private Camera mainCam;
    private Transform targetAnchor;

    public void Initialize(UnitController target, Transform uiAnchor)
    {
        targetUnit = target;
        targetUnit.Stats.OnMpChanged += UpdateMp;
        targetUnit.Stats.OnHpChanged += OnHpChanged;
        targetUnit.OnBenchState += BarStateChanged;
        mainCam = Camera.main;
        targetAnchor = uiAnchor;

        // Apply initial state (hide immediately if spawned on bench)
        gameObject.SetActive(!targetUnit.IsOnBench);
    }

    /// <summary>Show/hide bar on bench ↔ field transition.</summary>
    private void BarStateChanged(bool isOnBench)
    {
        gameObject.SetActive(!isOnBench);
    }

    /// <summary>MP change callback. Update fill amount.</summary>
    public void UpdateMp(float currentMp, float maxMp)
    {
        if (fill == null) return;
        fill.fillAmount = maxMp > 0f ? currentMp / maxMp : 0f;
    }

    /// <summary>Hide bar on death when HP changes to 0.</summary>
    private void OnHpChanged(float currentHp, float maxHp)
    {
        if (currentHp <= 0f)
            gameObject.SetActive(false);
    }

    private void LateUpdate()
    {
        // Destroy bar only when unit is fully destroyed
        if (targetUnit == null)
        {
            Destroy(gameObject);
            return;
        }

        // Hide bar when unit is deactivated (death etc.), show on reactivation
        if (!targetUnit.gameObject.activeInHierarchy)
        {
            if (gameObject.activeSelf) gameObject.SetActive(false);
            return;
        }
        else if (!gameObject.activeSelf && !targetUnit.IsOnBench)
        {
            gameObject.SetActive(true);
        }

        transform.position = mainCam.WorldToScreenPoint(targetAnchor.position) + screenOffset;
    }

    private void OnDestroy()
    {
        if (targetUnit != null)
        {
            targetUnit.Stats.OnMpChanged -= UpdateMp;
            targetUnit.Stats.OnHpChanged -= OnHpChanged;
            targetUnit.OnBenchState -= BarStateChanged;
        }
    }
}
