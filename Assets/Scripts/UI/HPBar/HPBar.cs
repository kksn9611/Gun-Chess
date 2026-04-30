using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
public class HPBar : MonoBehaviour
{
    [SerializeField] private Image fill;
    [SerializeField] private RawImage tickImage;

    private UnitController targetUnit;
    private Camera mainCam;
    private Transform targetAnchor;

    public void Initialize(UnitController target, Transform uiAnchor)
    {
        targetUnit = target;
        targetUnit.Stats.OnHpChanged += UpdateHp;
        targetUnit.OnBenchState += BarStateChanged;
        mainCam = Camera.main;
        targetAnchor = uiAnchor;

        // Apply initial state (hide immediately if spawned on bench)
        gameObject.SetActive(!targetUnit.IsOnBench);

        // Set initial fill and ticks
        UpdateHp(targetUnit.Stats.CurrentHp, targetUnit.Stats.CurrentMaxHp);
    }

    /// <summary>Show/hide bar on bench ↔ field transition.</summary>
    private void BarStateChanged(bool isOnBench)
    {
        gameObject.SetActive(!isOnBench);
    }

    public void UpdateHp(float currentHp, float maxHp)
    {
        if (fill == null) return;

        fill.fillAmount = currentHp / maxHp;
        

        // Hide immediately when HP reaches 0
        if (currentHp <= 0f)
            gameObject.SetActive(false);

        if (tickImage != null)
        {
            float hpPerTick = 100f; // 1 tick per 100 HP

            // e.g., maxHp 500 → tickCount = 5
            float tickCount = maxHp / hpPerTick;

            tickImage.uvRect = new Rect(0, 0, tickCount-1f, 1);
        }
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

        transform.position = mainCam.WorldToScreenPoint(targetAnchor.position);
    }

    private void OnDestroy()
    {
        if (targetUnit != null)
        {
            targetUnit.Stats.OnHpChanged -= UpdateHp;
            targetUnit.OnBenchState -= BarStateChanged;
        }
    }
}

