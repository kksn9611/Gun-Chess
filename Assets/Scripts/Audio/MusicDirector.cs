using UnityEngine;

/// <summary>
/// Drives BGM off battle phase: preparation vs battle, plus optional win/lose stingers.
/// Subscribes to BattleManager phase events (Observer), delegates crossfading to SoundManager.
/// </summary>
public class MusicDirector : MonoBehaviour
{
    [Header("Tracks (SoundId in the SoundLibrary)")]
    [SerializeField] private SoundId preparation = SoundId.BgmPreparation;
    [SerializeField] private SoundId battle = SoundId.BgmBattle;

    [Header("Result stingers (played as SFX)")]
    [SerializeField] private SoundId victory = SoundId.Victory;
    [SerializeField] private SoundId defeat = SoundId.Defeat;
    [SerializeField] private bool playVictoryDefeat = false; // off until those clips are authored

    private void OnEnable()
    {
        BattleManager.OnPreparationStart += OnPreparation;
        BattleManager.OnBattleStart      += OnBattle;
        BattleManager.OnBattleEnd        += OnBattleEnd;
    }

    private void OnDisable()
    {
        BattleManager.OnPreparationStart -= OnPreparation;
        BattleManager.OnBattleStart      -= OnBattle;
        BattleManager.OnBattleEnd        -= OnBattleEnd;
    }

    private void Start()
    {
        // Match the current phase on load (events may have fired before this object existed).
        if (BattleManager.Instance != null && BattleManager.Instance.CurrentPhase == BattleManager.Phase.Battle)
            OnBattle();
        else
            OnPreparation();
    }

    // Phase handlers //

    private void OnPreparation() => Play(preparation);
    private void OnBattle()      => Play(battle);

    private void OnBattleEnd(Team winner)
    {
        if (!playVictoryDefeat || SoundManager.Instance == null) return;
        bool playerWon = winner == Team.Player;
        SoundManager.Instance.Play(playerWon ? victory : defeat); // SFX one-shot, not BGM
    }

    private void Play(SoundId id)
    {
        if (SoundManager.Instance != null) SoundManager.Instance.PlayMusic(id);
    }
}
