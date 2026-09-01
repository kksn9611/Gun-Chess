using UnityEngine;

/// <summary>Anti-stall reaction: plays an SFX on each trigger via SoundManager.</summary>
public class AntiStallAudioReactor : MonoBehaviour
{
    [SerializeField] private AudioClip sfx;
    [Range(0f, 1f)]
    [SerializeField] private float volume = 1f;

    private void OnEnable()  => AntiStallController.OnTriggered += Play;
    private void OnDisable() => AntiStallController.OnTriggered -= Play;

    private void Play(int stack)
    {
        if (sfx != null && SoundManager.Instance != null)
            SoundManager.Instance.PlaySfx(sfx, volume);
    }
}
