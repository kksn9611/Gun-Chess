using UnityEngine;
using TMPro;

/// <summary>Shows the current stage number on the left of the TopBar. Updates when the round changes.</summary>
public class StageDisplay : MonoBehaviour
{
    [SerializeField] private RoundManager roundManager;
    [SerializeField] private TextMeshProUGUI label;
    [Tooltip("{0} = current round number")]
    [SerializeField] private string format = "Stage {0}";

    private int lastShown = -1;

    private void Update()
    {
        if (roundManager == null || label == null) return;
        int round = roundManager.CurrentRound;
        if (round == lastShown) return;
        lastShown = round;
        label.text = string.Format(format, round);
    }
}
