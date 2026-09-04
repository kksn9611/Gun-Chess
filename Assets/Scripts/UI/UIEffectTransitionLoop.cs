using System.Collections;
using Coffee.UIEffects;
using DG.Tweening;
using UnityEngine;

/// <summary>
/// Loops a UIEffect's transition sweep: waits a random interval, then animates Transition Rate 0 -> 1
/// over a fixed duration. Repeats while enabled (e.g. the periodic shine on augment cards).
/// </summary>
[RequireComponent(typeof(UIEffect))]
public class UIEffectTransitionLoop : MonoBehaviour
{
    [SerializeField] private UIEffect effect;          // target effect (defaults to this object's UIEffect)
    [Header("Timing")]
    [SerializeField] private float minInterval = 2f;   // shortest wait between sweeps
    [SerializeField] private float maxInterval = 3f;   // longest wait between sweeps
    [SerializeField] private float sweepDuration = 1f; // 0 -> 1 animation length
    [SerializeField] private Ease ease = Ease.OutSine;

    private Tween sweep;

    private void Awake()
    {
        if (effect == null) effect = GetComponent<UIEffect>();
    }

    // Loop //

    private void OnEnable() => StartCoroutine(LoopRoutine());

    private void OnDisable()
    {
        StopAllCoroutines();
        sweep?.Kill();
        sweep = null;
    }

    private IEnumerator LoopRoutine()
    {
        if (effect == null) yield break;

        while (true)
        {
            effect.transitionRate = 0f; // reset (invisible: shine sits before the element)
            yield return new WaitForSeconds(Random.Range(minInterval, maxInterval));

            sweep = DOTween.To(() => effect.transitionRate, v => effect.transitionRate = v, 1f, sweepDuration)
                           .SetEase(ease);
            yield return sweep.WaitForCompletion();
        }
    }
}
