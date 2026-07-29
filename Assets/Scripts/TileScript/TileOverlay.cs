using System.Collections;
using UnityEngine;

/// <summary>
/// Glowing overlay mesh shown on a tile while a unit is being placed.
/// Created procedurally by the tile layout; toggled and recolored by UnitPlacer.
/// </summary>
[RequireComponent(typeof(MeshRenderer))]
public class TileOverlay : MonoBehaviour
{
    private MeshRenderer meshRenderer;
    private MaterialPropertyBlock mpb;
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

    private Color currentColor;         // last applied tint (lerp start)
    private Coroutine colorRoutine;     // running color transition

    private void Awake()
    {
        meshRenderer = GetComponent<MeshRenderer>();
        mpb          = new MaterialPropertyBlock();
    }

    private void OnDisable() => colorRoutine = null; // coroutines auto-stop on deactivate

    // Show / Hide //

    public void Show() => gameObject.SetActive(true);
    public void Hide() => gameObject.SetActive(false);

    // Color //

    /// <summary>Set the tint instantly (cancels any running transition).</summary>
    public void SetColor(Color color)
    {
        if (colorRoutine != null) { StopCoroutine(colorRoutine); colorRoutine = null; }
        ApplyColor(color);
    }

    /// <summary>Lerp from the current tint to <paramref name="target"/> over <paramref name="duration"/> seconds.</summary>
    public void AnimateColor(Color target, float duration)
    {
        // No time to lerp, or inactive (can't run a coroutine) -> snap
        if (duration <= 0f || !gameObject.activeInHierarchy) { SetColor(target); return; }

        if (colorRoutine != null) StopCoroutine(colorRoutine);
        colorRoutine = StartCoroutine(LerpColor(target, duration));
    }

    private IEnumerator LerpColor(Color target, float duration)
    {
        Color start = currentColor;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            ApplyColor(Color.Lerp(start, target, t / duration));
            yield return null;
        }
        ApplyColor(target);
        colorRoutine = null;
    }

    // Push a color through the property block (per-tile, shared material) //
    private void ApplyColor(Color color)
    {
        currentColor = color;
        if (meshRenderer == null) meshRenderer = GetComponent<MeshRenderer>();
        if (mpb == null)          mpb          = new MaterialPropertyBlock();

        meshRenderer.GetPropertyBlock(mpb);
        mpb.SetColor(BaseColorId, color);
        meshRenderer.SetPropertyBlock(mpb);
    }
}
