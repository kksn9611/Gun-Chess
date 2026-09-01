using UnityEngine;

/// <summary>
/// Anti-stall reaction: shifts the battlefield spot light toward a mood color/intensity on each trigger
/// (deeper per stack), and restores the original look when the next preparation phase begins.
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
        }
    }

    private void OnEnable()
    {
        AntiStallController.OnTriggered  += Shift;
        BattleManager.OnPreparationStart += Restore;
    }

    private void OnDisable()
    {
        AntiStallController.OnTriggered  -= Shift;
        BattleManager.OnPreparationStart -= Restore;
    }

    // Mood //

    private void Shift(int stack)
    {
        if (spotLight == null) return;
        float t = Mathf.Clamp01((stack + 1) * perStackBlend); // stack 0 -> perStackBlend, escalates
        spotLight.color     = Color.Lerp(originalColor, moodColor, t);
        spotLight.intensity = Mathf.Lerp(originalIntensity, moodIntensity, t);
    }

    private void Restore()
    {
        if (spotLight == null || !cached) return;
        spotLight.color     = originalColor;
        spotLight.intensity = originalIntensity;
    }
}
