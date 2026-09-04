using UnityEngine;

/// <summary>
/// Bottom-right "Start Battle" button. Starts the round on click and is only visible/interactable during
/// the Preparation phase (hidden during Battle/Result). Toggles a CanvasGroup so this component stays
/// enabled and keeps receiving phase events.
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class StartBattleButton : MonoBehaviour
{
    [SerializeField] private RoundManager roundManager;
    [SerializeField] private CanvasGroup group; // defaults to this object's CanvasGroup

    private void Awake()
    {
        if (group == null) group = GetComponent<CanvasGroup>();
    }

    private void OnEnable()
    {
        BattleManager.OnPreparationStart += ShowForPrep;
        BattleManager.OnBattleStart      += HideForBattle;
        BattleManager.OnBattleEnd        += HideForResult;
    }

    private void OnDisable()
    {
        BattleManager.OnPreparationStart -= ShowForPrep;
        BattleManager.OnBattleStart      -= HideForBattle;
        BattleManager.OnBattleEnd        -= HideForResult;
    }

    // Set initial visibility after all Awakes (OnPreparationStart does not fire on the first round).
    private void Start() => Refresh();

    /// <summary>Button onClick target.</summary>
    public void OnClick()
    {
        // Hide as soon as the transition commits so it can't be re-clicked during the drain.
        if (roundManager != null && roundManager.BeginBattle()) SetVisible(false);
    }

    // Phase handlers //

    private void ShowForPrep()      => SetVisible(true);
    private void HideForBattle()    => SetVisible(false);
    private void HideForResult(Team _) => SetVisible(false);

    private void Refresh()
    {
        bool prep = BattleManager.Instance == null
                 || BattleManager.Instance.CurrentPhase == BattleManager.Phase.Preparation;
        SetVisible(prep);
    }

    private void SetVisible(bool on)
    {
        if (group == null) return;
        group.alpha         = on ? 1f : 0f;
        group.interactable  = on;
        group.blocksRaycasts = on;
    }
}
