using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Prototype augment selection window. Offers a set of augments rolled from an AugmentPool SO; picking one
/// applies it (via AugmentManager) and closes. Testing: pops up at game start when showOnStart is true.
/// Hides via CanvasGroup so this component stays active and Start() runs.
/// </summary>
public class AugmentSelectUI : MonoBehaviour
{
    [SerializeField] private CanvasGroup group;           // panel show/hide
    [SerializeField] private AugmentOptionUI[] options;   // choice cards
    [SerializeField] private AugmentPool pool;            // SO source of truth for offerable augments
    [SerializeField] private bool showOnStart = true;     // TEMPORARY: pop up at game start

    private void Awake() => SetVisible(false);

    private void Start()
    {
        if (showOnStart) Offer(options != null ? options.Length : 0);
    }

    /// <summary>Offer up to 'count' distinct augments rolled from the pool and show the window.</summary>
    public void Offer(int count)
    {
        if (options == null || pool == null) return;
        var owned = AugmentManager.Instance != null ? AugmentManager.Instance.Owned : null;
        List<AugmentData> choices = pool.Roll(count, owned);
        for (int i = 0; i < options.Length; i++)
            if (options[i] != null) options[i].Bind(i < choices.Count ? choices[i] : null, Pick);
        SetVisible(true);
        if (SoundManager.Instance != null) SoundManager.Instance.PlayUi(SoundId.UiReroll); // window appear
    }

    private void Pick(AugmentData a)
    {
        if (a == null) { SetVisible(false); return; }
        if (AugmentManager.Instance != null) AugmentManager.Instance.Choose(a);
        if (SoundManager.Instance != null) SoundManager.Instance.PlayUi(SoundId.UiSelect); // pick confirm
        SetVisible(false);
    }

    private void SetVisible(bool on)
    {
        if (group == null) return;
        group.alpha = on ? 1f : 0f;
        group.interactable = on;
        group.blocksRaycasts = on;
    }
}
