using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Top-center timer bar. Depletes over AntiStallConfig.initialDelay during combat, then blinks red
/// once it empties (the anti-stall trigger point).
/// </summary>
public class AntiStallTimerBar : MonoBehaviour
{
    [SerializeField] private AntiStallConfig config;
    [SerializeField] private Image fill;         // Filled Horizontal
    [SerializeField] private CanvasGroup group;  // show/hide without disabling this component

    [Header("Colors")]
    [SerializeField] private Color countdownColor = new Color(0.3f, 0.8f, 1f, 1f);
    [SerializeField] private Color blinkColor = Color.red;
    [SerializeField] private float blinkSpeed = 4f;

    private float elapsed;
    private bool running;
    private bool blinking;

    private void Awake() => SetVisible(false);

    private void OnEnable()
    {
        BattleManager.OnBattleStart      += Begin;
        BattleManager.OnBattleEnd        += End;
        BattleManager.OnPreparationStart += ResetBar;
    }

    private void OnDisable()
    {
        BattleManager.OnBattleStart      -= Begin;
        BattleManager.OnBattleEnd        -= End;
        BattleManager.OnPreparationStart -= ResetBar;
    }

    // Lifecycle //

    private void Begin()
    {
        elapsed = 0f; running = true; blinking = false;
        if (fill != null) { fill.color = countdownColor; fill.fillAmount = 1f; }
        SetVisible(true);
    }

    private void End(Team _) => ResetBar();

    private void ResetBar()
    {
        running = false; blinking = false;
        SetVisible(false);
    }

    // Tick //

    private void Update()
    {
        if (running)
        {
            float delay = config != null ? config.initialDelay : 0f;
            elapsed += Time.deltaTime;
            float remaining = delay > 0f ? Mathf.Clamp01(1f - elapsed / delay) : 0f;
            if (fill != null) fill.fillAmount = remaining;

            if (remaining <= 0f)
            {
                running = false;
                blinking = true;
                if (fill != null) fill.fillAmount = 1f; // full bar flashes on empty
            }
        }
        else if (blinking && fill != null)
        {
            float t = Mathf.PingPong(Time.time * blinkSpeed, 1f);
            Color c = blinkColor;
            c.a = Mathf.Lerp(0.25f, 1f, t);
            fill.color = c;
        }
    }

    private void SetVisible(bool on)
    {
        if (group != null) group.alpha = on ? 1f : 0f;
    }
}
