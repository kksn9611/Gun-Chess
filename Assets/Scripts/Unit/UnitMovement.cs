using UnityEngine;
using System.Collections;

/// <summary>
/// Unit Move and Rotation Componenet.
/// </summary>
[RequireComponent(typeof(UnitController))]
public class UnitMovement : MonoBehaviour
{
    private UnitController unit;
    private Coroutine moveCoroutine;

    [Header("Rotation")]
    [SerializeField] private float rotationSpeed = 10f;
    [SerializeField] private float rotationAngle;

    private void Awake()
    {
        unit = GetComponent<UnitController>();
    }

    /// <summary>
    /// Lerp from current position to target tile.
    /// Duration = 1 / moveSpd.
    /// </summary>
    public IEnumerator LerpToTile(TileScript tile)
    {
        Vector3 startPos = transform.position;
        Vector3 endPos = tile.transform.position;

        float duration = 1f / unit.Stats.CurrentMoveSpd;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            transform.position = Vector3.Lerp(startPos, endPos, t);
            LookAtDirection(endPos);
            elapsed += Time.deltaTime;
            yield return null;
        }
        transform.position = endPos;
    }

    /// <summary>
    /// Stop active movement and snap position to current tile.
    /// Called by EnterAttackState(), Die(), or forced restart.
    /// </summary>
    public void StopMovement()
    {
        if (moveCoroutine != null)
        {
            StopCoroutine(moveCoroutine);
            moveCoroutine = null;
        }
        // If interrupted mid-lerp, snap to the current logical tile position
        if (unit.CurrentHexTile != null)
            transform.position = unit.CurrentHexTile.transform.position;
    }

    /// <summary>
    /// Rotate to target
    /// </summary>
    public void LookAtTarget(Transform targetTransform)
    {
        if (targetTransform == null) return;

        Vector3 direction = targetTransform.position - transform.position;
        direction.y = 0f; // Ignoring Y to prevent body tilting.

        // Prevent excessive rotation.
        if (direction.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            Quaternion offsetRotation = Quaternion.Euler(0f, rotationAngle, 0f);

            Quaternion finalRotation = targetRotation * offsetRotation;
            transform.rotation = Quaternion.Slerp(transform.rotation, finalRotation, Time.deltaTime * rotationSpeed);
        }
    }
    /// <summary>
    /// Rotate to direction
    /// </summary>
    public void LookAtDirection(Vector3 targetPosition)
    {
        Vector3 direction = targetPosition - transform.position;
        direction.y = 0f; // ignore Y axis

        if (direction.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
        }
    }
}
