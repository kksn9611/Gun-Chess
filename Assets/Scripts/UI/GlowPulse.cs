using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Breathing glow: oscillates a Graphic's alpha and the transform scale on a sine wave.
/// Runs on unscaled time so it animates during the (paused) shop phase.
/// </summary>
public class GlowPulse : MonoBehaviour
{
    [SerializeField] private Graphic target; // the glow image (defaults to this object's Graphic)
    [SerializeField] private float minAlpha = 0.35f;
    [SerializeField] private float maxAlpha = 0.85f;
    [SerializeField] private float minScale = 1.0f;
    [SerializeField] private float maxScale = 1.12f;
    [SerializeField] private float speed    = 2.5f;

    private void Reset()    => target = GetComponent<Graphic>();
    private void OnEnable() { if (target == null) target = GetComponent<Graphic>(); }

    private void Update()
    {
        float t = (Mathf.Sin(Time.unscaledTime * speed) + 1f) * 0.5f;

        if (target != null)
        {
            Color c = target.color;
            c.a = Mathf.Lerp(minAlpha, maxAlpha, t);
            target.color = c;
        }
        float s = Mathf.Lerp(minScale, maxScale, t);
        transform.localScale = new Vector3(s, s, 1f);
    }
}
