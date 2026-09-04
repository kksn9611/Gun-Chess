using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// One-time intro: hides the shop slots on Awake, then reveals them one-by-one (staggered) when Reveal()
/// is called, playing a sound per slot. Intro-only — it doesn't touch the shop's normal content logic.
/// Wire Reveal() to BottomBarIntro.onComplete.
/// </summary>
public class ShopSlotIntro : MonoBehaviour
{
    [SerializeField] private GameObject[] slots;              // revealed in order
    [SerializeField] private float stagger = 0.12f;          // delay between slots
    [SerializeField] private SoundId revealSound = SoundId.UiReroll;

    private bool revealed;

    private void Awake()
    {
        if (slots != null)
            foreach (GameObject s in slots)
                if (s != null) s.SetActive(false);
    }

    /// <summary>Reveal the hidden slots sequentially (idempotent — runs once).</summary>
    public void Reveal()
    {
        if (revealed) return;
        revealed = true;
        RevealAsync(this.GetCancellationTokenOnDestroy()).Forget();
    }

    private async UniTaskVoid RevealAsync(CancellationToken ct)
    {
        if (slots == null) return;
        foreach (GameObject s in slots)
        {
            if (s == null) continue;
            s.SetActive(true);
            if (SoundManager.Instance != null) SoundManager.Instance.PlayUi(revealSound);

            try { await UniTask.WaitForSeconds(stagger, cancellationToken: ct); }
            catch (System.OperationCanceledException) { return; }
        }
    }
}
