using DG.Tweening;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

/// <summary>
/// TITLE SCENE. Lives on the menu Canvas. When ANY child button is clicked, slides every menu button
/// off-screen to the left (DOTween). Auto-hooks each button's onClick, so it applies to all buttons
/// without per-button wiring. Runs once.
/// </summary>
public class TitleMenuSlideOut : MonoBehaviour
{
    [SerializeField] private float duration = 0.4f;
    [SerializeField] private Ease ease = Ease.InQuint;   
    [Tooltip("Extra horizontal distance past the edge so the buttons fully clear the screen")]
    [FormerlySerializedAs("extraOffset")]
    [SerializeField] private float extraXOffset = 100f;
    [Tooltip("Extra vertical offset applied as the buttons slide out (positive = up)")]
    [SerializeField] private float extraYOffset = 0f;
    [SerializeField] RectTransform titleTransform;

    private RectTransform[] targets;
    private bool sliding;

    private void Awake()
    {
        Button[] buttons = GetComponentsInChildren<Button>(true);
        targets = new RectTransform[buttons.Length+1]; // buttons + title
        for (int i = 0; i < buttons.Length; i++)
        {
            targets[i] = (RectTransform)buttons[i].transform;
            buttons[i].onClick.AddListener(SlideOut);   // any button click -> slide all out
        }
        targets[buttons.Length] = titleTransform; // add title
    }

    /// <summary>Slide all menu buttons off-screen to the left. Idempotent.</summary>
    public void SlideOut()
    {
        if (sliding) return;
        sliding = true;

        float canvasWidth = ((RectTransform)transform).rect.width;
        foreach (RectTransform rt in targets)
        {
            float moveX = canvasWidth + rt.rect.width + extraXOffset;        // guaranteed off the left edge
            Vector2 target = rt.anchoredPosition + new Vector2(-moveX, extraYOffset);
            rt.DOAnchorPos(target, duration).SetEase(ease);
        }
    }
}
