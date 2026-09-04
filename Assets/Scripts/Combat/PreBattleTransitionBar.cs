using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// TopBar bar shown full during the Preparation phase, then drained to empty over the pre-battle
/// transition. Hidden during Battle/Result (the anti-stall combat timer takes the slot). Separate bar
/// from the anti-stall timer, so the two never contend for the same fill.
/// </summary>
public class PreBattleTransitionBar : MonoBehaviour
{
    [SerializeField] private Image fill;         // Filled Horizontal
    [SerializeField] private CanvasGroup group;  // show/hide without disabling this component
    [SerializeField] private Color drainColor = new Color(0.3f, 0.8f, 1f, 1f);

    private void OnEnable()
    {
        BattleManager.OnPreparationStart += ShowFull;
        BattleManager.OnBattleStart      += Hide;
        BattleManager.OnBattleEnd        += HideOnEnd;
    }

    private void OnDisable()
    {
        BattleManager.OnPreparationStart -= ShowFull;
        BattleManager.OnBattleStart      -= Hide;
        BattleManager.OnBattleEnd        -= HideOnEnd;
    }

    // Initial state (the first round does not fire OnPreparationStart).
    private void Start()
    {
        bool prep = BattleManager.Instance == null
                 || BattleManager.Instance.CurrentPhase == BattleManager.Phase.Preparation;
        if (prep) ShowFull(); else Hide();
    }

    /// <summary>Drain fill 1 -> 0 over duration. The bar is already visible from Preparation; visibility
    /// afterward is handled by the phase events (OnBattleStart hides it right after StartBattle).</summary>
    public async UniTask Drain(float duration, CancellationToken ct)
    {
        if (fill != null) { fill.color = drainColor; fill.fillAmount = 1f; }
        SetVisible(true);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            if (fill != null) fill.fillAmount = duration > 0f ? Mathf.Clamp01(1f - elapsed / duration) : 0f;
            await UniTask.Yield(ct);
        }
        if (fill != null) fill.fillAmount = 0f;
    }

    // Visibility //

    private void ShowFull()
    {
        if (fill != null) { fill.color = drainColor; fill.fillAmount = 1f; }
        SetVisible(true);
    }

    private void Hide()             => SetVisible(false);
    private void HideOnEnd(Team _)  => SetVisible(false);

    private void SetVisible(bool on)
    {
        if (group != null) group.alpha = on ? 1f : 0f;
    }
}
