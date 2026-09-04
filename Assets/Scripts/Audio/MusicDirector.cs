using UnityEngine;

/// <summary>
/// Plays the configured looping BGM on start, plus optional win/lose SFX stingers on battle end.
/// (Set `bgm` to MainBgm in the Main scene, TitleBgm in the Title scene.)
/// </summary>
public class MusicDirector : MonoBehaviour
{
    [Header("Track (SoundId in the SoundLibrary)")]
    [SerializeField] private SoundId bgm = SoundId.MainBgm;

    [Header("Result stingers (played as SFX)")]
    [SerializeField] private SoundId victory = SoundId.Victory;
    [SerializeField] private SoundId defeat = SoundId.Defeat;
    [SerializeField] private bool playVictoryDefeat = false; // off until those clips are authored

    private void OnEnable()  => BattleManager.OnBattleEnd += OnBattleEnd;
    private void OnDisable() => BattleManager.OnBattleEnd -= OnBattleEnd;

    private void Start()
    {
        if (SoundManager.Instance != null) SoundManager.Instance.PlayMusic(bgm);
    }

    private void OnBattleEnd(Team winner)
    {
        if (!playVictoryDefeat || SoundManager.Instance == null) return;
        SoundManager.Instance.Play(winner == Team.Player ? victory : defeat); // SFX one-shot
    }
}
