using System.Collections;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using DG.Tweening;

/// <summary>
/// Unit Move and Rotation Componenet.
/// </summary>
[RequireComponent(typeof(UnitController))]
public class UnitMovement : MonoBehaviour
{
    private UnitController unit;
    private CancellationTokenSource moveCts;

    [Header("Rotation")]
    [SerializeField] private float rotationSpeed = 10f;
    [Tooltip("Attack rotation adjust")]
    [SerializeField] private float rotationAngle;
    [SerializeField] private float rotationDuration = 0.2f;


    private Tween currentRotationTween;

    private void Awake()
    {
        unit = GetComponent<UnitController>();
    }

    /// <summary>
    /// Async lerp from current position to target tile.
    /// Duration = 1 / moveSpd.
    /// </summary>
    public async UniTask LerpToTileAsync(TileScript tile)
    {
        moveCts?.Cancel();
        moveCts?.Dispose();
        moveCts = new CancellationTokenSource();
        CancellationToken ct = moveCts.Token;

        Vector3 startPos = transform.position;
        Vector3 endPos = tile.transform.position;

        float duration = 1f / unit.Stats.CurrentMoveSpd;
        LookAtDirection(endPos, ct).Forget();
        await transform.DOMove(endPos, duration)
                .SetEase(Ease.Linear)
                .ToUniTask(cancellationToken: ct);
    }

    /// <summary>
    /// Stop active movement and snap position to current tile.
    /// Called by EnterAttackState(), Die(), or forced restart.
    /// </summary>
    public void StopMovement()
    {
        moveCts?.Cancel();
        moveCts?.Dispose();
        moveCts = null;

        // If interrupted mid-lerp, snap to the current logical tile position
        if (unit.CurrentHexTile != null)
            transform.position = unit.CurrentHexTile.transform.position;
    }

    /// <summary>
    /// Rotate to target
    /// </summary>
    public async UniTask LookAtTarget(Transform targetTransform, CancellationToken ct)
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
            
            // Prevent animation collide
            currentRotationTween?.Kill();

            currentRotationTween = transform.DORotateQuaternion(finalRotation, rotationDuration).SetEase(Ease.OutQuad);
            await currentRotationTween.ToUniTask(cancellationToken: ct);
        }
    }
    /// <summary>
    /// Rotate to direction
    /// </summary>
    public async UniTaskVoid LookAtDirection(Vector3 targetPosition, CancellationToken ct)
    {
        Vector3 direction = targetPosition - transform.position;
        direction.y = 0f; // ignore Y axis

        if (direction.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);

            // Prevent animation collide
            currentRotationTween?.Kill();
            currentRotationTween = transform.DORotateQuaternion(targetRotation, rotationDuration).SetEase(Ease.OutQuad);

            await currentRotationTween.ToUniTask(cancellationToken: ct);
        }
    }
}
