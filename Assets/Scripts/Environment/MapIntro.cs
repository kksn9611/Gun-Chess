using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// MAIN SCENE. On enter, slides the Map from its current position to (0,0,0) over `duration` seconds
/// (DOTween), then fires onComplete — used to trigger the BottomBar intro. If the Map is already at
/// (0,0,0) the move is skipped and onComplete fires immediately. The board's tiles/bench are children of
/// Map so they slide in with it; units are NOT parented, so unit placement is gated on WaitForCompletion()
/// to run only once the board is at rest.
/// </summary>
public class MapIntro : MonoBehaviour
{
    [SerializeField] private float startDelay = 0.5f;               // wait for objects to load before moving
    [SerializeField] private float duration = 1.5f;                 // n seconds
    [SerializeField] private Ease ease = Ease.OutCubic;
    [Tooltip("Invoked when the map reaches (0,0,0) (e.g. -> BottomBarIntro.Play)")]
    [SerializeField] private UnityEvent onComplete;

    private bool complete;

    public bool Complete => complete;

    private async UniTaskVoid Start()
    {
        CancellationToken ct = this.GetCancellationTokenOnDestroy();

        // Give objects a moment to finish loading before the intro moves the Map.
        try { await UniTask.WaitForSeconds(startDelay, cancellationToken: ct); }
        catch (System.OperationCanceledException) { return; }

        // Already at (0,0,0): nothing to animate — complete immediately so the chain continues.
        if (transform.localPosition == Vector3.zero)
        {
            complete = true;
            onComplete?.Invoke();
            return;
        }

        transform.DOMove(Vector3.zero, duration).SetEase(ease);

        // Wait on the duration (robust) rather than the tween callback, then guarantee the final state.
        try { await UniTask.WaitForSeconds(duration, cancellationToken: ct); }
        catch (System.OperationCanceledException) { return; }

        transform.localPosition = Vector3.zero;
        complete = true;
        onComplete?.Invoke();
    }

    /// <summary>Await until the map has finished moving (returns immediately if already done).</summary>
    public async UniTask WaitForCompletion(CancellationToken ct = default)
    {
        if (complete) return;
        await UniTask.WaitUntil(() => complete, cancellationToken: ct);
    }
}
