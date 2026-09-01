using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Right-click a unit to open the status window; click anywhere outside it to close.
/// </summary>
public class UnitStatusController : MonoBehaviour
{
    [SerializeField] private UnitStatusWindow window;
    [SerializeField] private Camera cam;

    private RectTransform windowRect;
    private Canvas canvas;

    private void Awake()
    {
        if (cam == null) cam = Camera.main;
        if (window != null)
        {
            windowRect = (RectTransform)window.transform;
            canvas     = window.GetComponentInParent<Canvas>();
            window.Hide();
        }
    }

    // Input //

    private void Update()
    {
        if (window == null || Mouse.current == null) return;

        bool left  = Mouse.current.leftButton.wasPressedThisFrame;
        bool right = Mouse.current.rightButton.wasPressedThisFrame;
        if (!left && !right) return;

        Vector2 mouse = Mouse.current.position.ReadValue();

        // Clicks on the window itself keep it open.
        if (window.gameObject.activeSelf && PointerOverWindow(mouse)) return;

        // Right-click on a unit opens / updates the window.
        if (right)
        {
            UnitController unit = RaycastUnit(mouse);
            if (unit != null) { window.Show(unit); return; }
        }

        // Any other click outside the window closes it.
        window.Hide();
    }

    // Hit Testing //

    private bool PointerOverWindow(Vector2 screenPos)
    {
        if (windowRect == null) return false;
        Camera uiCam = (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay) ? canvas.worldCamera : null;
        return RectTransformUtility.RectangleContainsScreenPoint(windowRect, screenPos, uiCam);
    }

    /// <summary>The unit under the cursor via the shared resolver (unit collider or tile occupant).</summary>
    private UnitController RaycastUnit(Vector2 screenPos)
    {
        ClickResolver.TryResolve(cam, screenPos, out _, out UnitController unit);
        return unit;
    }
}
