using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Slide-in intro (same design as BottomBarIntro): caches the element's original anchored position and
/// pushes it off-screen in the chosen direction in Awake. Does NOT auto-play — call Play() (e.g. wired to
/// MapIntro.onComplete) to slide it back to its original position, then fire onComplete.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class SlideInIntro : MonoBehaviour
{
    private enum From { Top, Bottom, Left, Right }

    [Tooltip("Edge the element slides in FROM")]
    [SerializeField] private From from = From.Bottom;
    [SerializeField] private float duration = 1f;
    [SerializeField] private Ease ease = Ease.OutCubic;
    [Tooltip("Extra distance beyond the element's size so it starts fully off-screen")]
    [SerializeField] private float extraOffset = 40f;
    [Tooltip("Invoked when the slide-in completes")]
    [SerializeField] private UnityEvent onComplete;

    private RectTransform rt;
    private Vector2 originalPos;

    private void Awake()
    {
        rt = (RectTransform)transform;
        originalPos = rt.anchoredPosition;                       // cache target position
        rt.anchoredPosition = originalPos + OffscreenOffset();   // push off-screen (from the chosen edge)
    }

    private Vector2 OffscreenOffset()
    {
        switch (from)
        {
            case From.Top:    return Vector2.up    * (rt.rect.height + extraOffset);
            case From.Bottom: return Vector2.down  * (rt.rect.height + extraOffset);
            case From.Left:   return Vector2.left  * (rt.rect.width  + extraOffset);
            case From.Right:  return Vector2.right * (rt.rect.width  + extraOffset);
            default:          return Vector2.zero;
        }
    }

    /// <summary>Slide the element from its off-screen start back to its original position, then fire onComplete.</summary>
    public void Play()
    {
        rt.DOAnchorPos(originalPos, duration).SetEase(ease)
          .OnComplete(() => onComplete?.Invoke());
    }
}
