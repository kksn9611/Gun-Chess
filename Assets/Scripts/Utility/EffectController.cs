using UnityEngine;

/// <summary>Plays a one-shot SFX (2D) when the effect object is enabled, after delayTime.</summary>
public class EffectController : MonoBehaviour
{
    public float delayTime = 0.5f;

    [Header("Sound")]
    [SerializeField] private AudioClip clip;    // played on enable
    [Range(0f, 1f)]
    [SerializeField] private float volume = 1f;

    void OnEnable()
    {
        if (clip != null && SoundManager.Instance != null)
            SoundManager.Instance.PlaySfx(clip, volume, delayTime);
    }
}
