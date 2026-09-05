using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
public class HPBar : MonoBehaviour
{
    [SerializeField] private Image fill;
    [SerializeField] private Image shieldFill; // shield overlay (scaled to maxHp)
    [SerializeField] private RawImage tickImage;

    private UnitController targetUnit;
    private Camera mainCam;
    private Transform targetAnchor;

    // Screen-to-canvas conversion (works for Overlay and Camera/World Space canvases alike) //
    private RectTransform canvasRect;
    private Camera uiCamera; // null for Screen Space - Overlay, the canvas's render camera otherwise

    public void Initialize(UnitController target, Transform uiAnchor)
    {
        targetUnit = target;
        targetUnit.Stats.OnHpChanged += UpdateHp;
        targetUnit.Stats.OnShieldChanged += UpdateShield;
        targetUnit.OnBenchState += BarStateChanged;
        mainCam = Camera.main;
        targetAnchor = uiAnchor;

        canvasRect = transform.parent as RectTransform; // bar is instantiated directly under the canvas
        Canvas canvas = canvasRect != null ? canvasRect.GetComponent<Canvas>() : null;
        uiCamera = (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay) ? canvas.worldCamera : null;

        // Apply initial state (hide immediately if spawned on bench)
        gameObject.SetActive(!targetUnit.IsOnBench);

        // Set initial fill and ticks
        UpdateHp(targetUnit.Stats.CurrentHp, targetUnit.Stats.CurrentMaxHp);
        UpdateShield(targetUnit.Stats.CurrentShield, targetUnit.Stats.CurrentMaxHp);
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
            float hpPerTick = 250f; // 1 tick per 100 HP

            float tickCount = maxHp / hpPerTick;

            tickImage.uvRect = new Rect(0, 0, tickCount, 1);
        }
    }

    /// <summary>Update the shield overlay, scaled to maxHp like the HP fill.</summary>
    public void UpdateShield(float currentShield, float maxHp)
    {
        if (shieldFill == null) return;
        shieldFill.fillAmount = maxHp > 0f ? Mathf.Clamp01(currentShield / maxHp) : 0f;
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

        // Convert to canvas-local space instead of assigning world position directly: only equivalent to
        // screen pixels under Screen Space - Overlay, and silently wrong (bar flies off to whatever world
        // point those pixel numbers name) under Screen Space - Camera / World Space.
        Vector2 screenPoint = mainCam.WorldToScreenPoint(targetAnchor.position);
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPoint, uiCamera, out Vector2 localPoint))
            transform.localPosition = localPoint;
    }

    private void OnDestroy()
    {
        if (targetUnit != null)
        {
            targetUnit.Stats.OnHpChanged -= UpdateHp;
            targetUnit.Stats.OnShieldChanged -= UpdateShield;
            targetUnit.OnBenchState -= BarStateChanged;
        }
    }
}

