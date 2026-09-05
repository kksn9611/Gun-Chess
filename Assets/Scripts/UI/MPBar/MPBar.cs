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

    // Screen-to-canvas conversion (works for Overlay and Camera/World Space canvases alike) //
    private RectTransform canvasRect;
    private Camera uiCamera; // null for Screen Space - Overlay, the canvas's render camera otherwise

    public void Initialize(UnitController target, Transform uiAnchor)
    {
        targetUnit = target;
        targetUnit.Stats.OnMpChanged += UpdateMp;
        targetUnit.Stats.OnHpChanged += OnHpChanged;
        targetUnit.OnBenchState += BarStateChanged;
        mainCam = Camera.main;
        targetAnchor = uiAnchor;

        canvasRect = transform.parent as RectTransform; // bar is instantiated directly under the canvas
        Canvas canvas = canvasRect != null ? canvasRect.GetComponent<Canvas>() : null;
        uiCamera = (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay) ? canvas.worldCamera : null;

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

        // Convert to canvas-local space instead of assigning world position directly: only equivalent to
        // screen pixels under Screen Space - Overlay, and silently wrong (bar flies off to whatever world
        // point those pixel numbers name) under Screen Space - Camera / World Space.
        Vector2 screenPoint = (Vector2)mainCam.WorldToScreenPoint(targetAnchor.position) + (Vector2)screenOffset;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPoint, uiCamera, out Vector2 localPoint))
            transform.localPosition = localPoint;
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
