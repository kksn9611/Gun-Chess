using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// Anti-stall scheduler. After a delay into combat it raises OnTriggered on a cadence so reactors
/// (buff, light, sfx) escalate the fight. One-shot when the config's stackInterval is &lt;= 0.
/// </summary>
public class AntiStallController : MonoBehaviour
{
    [SerializeField] private AntiStallConfig config;

    /// <summary>Raised on each anti-stall trigger; arg = stack index (0 = first, at initialDelay).</summary>
    public static event Action<int> OnTriggered;

    private CancellationTokenSource cts;

    private void OnEnable()
    {
        BattleManager.OnBattleStart += Begin;
        BattleManager.OnBattleEnd   += End;
    }

    private void OnDisable()
    {
        BattleManager.OnBattleStart -= Begin;
        BattleManager.OnBattleEnd   -= End;
        Cancel();
    }

    // Lifecycle //

    private void Begin()
    {
        if (config == null) return;
        Cancel();
        cts = new CancellationTokenSource();
        RunAsync(cts.Token).Forget();
    }

    private void End(Team _) => Cancel();

    private void Cancel()
    {
        cts?.Cancel();
        cts?.Dispose();
        cts = null;
    }

    // Schedule //

    private async UniTaskVoid RunAsync(CancellationToken ct)
    {
        try
        {
            await UniTask.WaitForSeconds(config.initialDelay, cancellationToken: ct);

            int stack = 0;
            while (true)
            {
                OnTriggered?.Invoke(stack);
                stack++;

                if (config.stackInterval <= 0f) break;                       // one-shot
                if (config.maxStacks > 0 && stack >= config.maxStacks) break; // capped
                await UniTask.WaitForSeconds(config.stackInterval, cancellationToken: ct);
            }
        }
        catch (OperationCanceledException) { } // battle ended / disabled
    }
}
