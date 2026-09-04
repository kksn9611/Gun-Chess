using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Bottom bar intro: caches the bar's original anchored position and drops it off-screen in Awake. Does
/// NOT auto-play — call Play() (triggered after the Map intro finishes) to slide it back up, then fire
/// onComplete (shop slot reveal + initial enemy placement).
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class BottomBarIntro : MonoBehaviour
{
    [SerializeField] private float duration = 1f;
    [SerializeField] private Ease ease = Ease.OutCubic;
    [Tooltip("Extra drop beyond the bar's height so it starts fully off-screen")]
    [SerializeField] private float extraOffset = 40f;

    [Tooltip("Invoked when the slide-up completes")]
    [SerializeField] private UnityEvent onComplete;

    private RectTransform rt;
    private Vector2 originalPos;

    private void Awake()
    {
        rt = (RectTransform)transform;
        originalPos = rt.anchoredPosition;                       // cache target position
        float drop = rt.rect.height + extraOffset;
        rt.anchoredPosition = originalPos + Vector2.down * drop; // instantly move off-screen (below)
    }

    /// <summary>Slide the bar up to its original position, then fire onComplete.</summary>
    public void Play()
    {
        rt.DOAnchorPos(originalPos, duration).SetEase(ease)     // slide up to the cached position
          .OnComplete(() => onComplete?.Invoke());
    }
}
