using UnityEngine;

/// <summary>
/// Anti-stall reaction: the battlefield spot light stays off until the anti-stall system first fires,
/// then shifts toward a mood color/intensity per stack. Switches off the instant the battle is decided.
/// </summary>
public class AntiStallLightReactor : MonoBehaviour
{
    [SerializeField] private Light spotLight;                 // BattleField Spot Light
    [SerializeField] private Color moodColor = new Color(0.85f, 0.12f, 0.12f, 1f);
    [SerializeField] private float moodIntensity = 2f;
    [Tooltip("Blend toward mood per stack (1 = full mood on the first trigger)")]
    [SerializeField] private float perStackBlend = 1f;

    private Color originalColor;
    private float originalIntensity;
    private bool cached;

    private void Awake()
    {
        if (spotLight != null)
        {
            originalColor     = spotLight.color;
            originalIntensity = spotLight.intensity;
            cached = true;
            spotLight.enabled = false; // off until the anti-stall system triggers
        }
    }

    private void OnEnable()
    {
        AntiStallController.OnTriggered += Shift;
        BattleManager.OnBattleEnd       += Deactivate;
    }

    private void OnDisable()
    {
        AntiStallController.OnTriggered -= Shift;
        BattleManager.OnBattleEnd       -= Deactivate;
    }

    // Mood //

    private void Shift(int stack)
    {
        if (spotLight == null) return;
        spotLight.enabled = true; // enable on first (and every) trigger
        float t = Mathf.Clamp01((stack + 1) * perStackBlend); // stack 0 -> perStackBlend, escalates
        spotLight.color     = Color.Lerp(originalColor, moodColor, t);
        spotLight.intensity = Mathf.Lerp(originalIntensity, moodIntensity, t);
    }

    private void Deactivate(Team winner)
    {
        if (spotLight == null) return;
        spotLight.enabled = false; // off the moment the outcome is decided
        if (cached)
        {
            spotLight.color     = originalColor;
            spotLight.intensity = originalIntensity;
        }
    }
}
