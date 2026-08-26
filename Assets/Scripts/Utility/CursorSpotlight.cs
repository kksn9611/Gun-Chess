using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Aims a Spot Light (placed at the camera) at the ground point under the mouse cursor.
/// </summary>
public class CursorSpotlight : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera targetCamera; // projects the cursor; defaults to Camera.main

    private float groundHeight = 0f;
    [Tooltip("Rotation follow speed; <= 0 instant follow")]
    [SerializeField] private float followSpeed = 12f;

    private void Awake()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;
    }

    // Aim Update //

    private void Update()
    {
        if (targetCamera == null || Mouse.current == null) return;

        // Project the cursor onto the ground plane (matches UnitPlacer's drag raycast)
        Ray ray = targetCamera.ScreenPointToRay(Mouse.current.position.value);
        Plane ground = new Plane(Vector3.up, new Vector3(0f, groundHeight, 0f));
        if (!ground.Raycast(ray, out float enter)) return;

        Vector3 aimPoint = ray.GetPoint(enter);
        Vector3 dir      = aimPoint - transform.position;
        if (dir.sqrMagnitude < 0.0001f) return; // cursor over the light itself

        Quaternion targetRot = Quaternion.LookRotation(dir);
        transform.rotation = followSpeed > 0f
            ? Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * followSpeed)
            : targetRot;
    }
}
