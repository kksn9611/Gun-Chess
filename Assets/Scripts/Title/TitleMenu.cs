using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Title screen actions. Game Start pre-loads the main scene in the background (held in standby) while
/// the skybox intro and a loading bar play, then activates it once the load is ready AND a guaranteed
/// minimum standby time has elapsed — a smooth, hitch-free transition. Exit quits.
/// </summary>
public class TitleMenu : MonoBehaviour
{
    [Header("Transition")]
    [Tooltip("Scene loaded after the intro (must be in Build Settings)")]
    [SerializeField] private string mainSceneName = "Main";
    [Tooltip("Guaranteed minimum standby: the transition never ends before this many seconds (also always waits for the load and the intro)")]
    [SerializeField] private float minStandbyDuration = 0f;

    [Header("Loading Bar")]
    [Tooltip("Root shown during the transition (hidden otherwise)")]
    [SerializeField] private GameObject loadingBarRoot;
    [Tooltip("Filled image (fillAmount 0..1) that visualizes progress")]
    [SerializeField] private Image loadingBarFill;
    [Tooltip("How fast the bar catches up to the true progress")]
    [SerializeField] private float barFillSpeed = 3f;

    private bool starting;

    private void Awake()
    {

        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 120;

        if (loadingBarRoot != null) loadingBarRoot.SetActive(false);
    }

    /// <summary>Game Start button: pre-load Main, play the intro + bar, then activate Main.</summary>
    public void StartGame()
    {
        if (starting) return;
        starting = true;
        StartSequenceAsync().Forget();
    }

    private async UniTaskVoid StartSequenceAsync()
    {
        CancellationToken ct = this.GetCancellationTokenOnDestroy();

        // Begin loading Main in the background, but hold it in standby: no activation, so the scene's
        // objects don't wake (no Awake/Start) until we flip the flag below.
        AsyncOperation load = SceneManager.LoadSceneAsync(mainSceneName);
        if (load == null) return;                       // scene not in Build Settings
        load.allowSceneActivation = false;

        // Show the loading bar and start the Title half of the rotation on the persistent background
        // (accelerate 1 -> -50). The background survives the swap; Main's half runs on scene load.
        if (loadingBarRoot != null) loadingBarRoot.SetActive(true);
        if (loadingBarFill != null) loadingBarFill.fillAmount = 0f;
        PersistentBackground bg = PersistentBackground.Instance;
        if (bg != null) bg.PlayTitleIntro();

        // Standby ends only when the load is ready (progress 0.9) AND the guaranteed minimum time has
        // elapsed — the longer of the intro duration and minStandbyDuration.
        float introDuration = bg != null ? bg.TitleDuration : 0f;
        float minStandby = Mathf.Max(introDuration, minStandbyDuration);

        float elapsed = 0f;
        float shown = 0f;
        while (true)
        {
            elapsed += Time.deltaTime;
            float loadN = Mathf.Clamp01(load.progress / 0.9f);     // scene load 0..0.9 -> 0..1
            float timeN = minStandby > 0f ? Mathf.Clamp01(elapsed / minStandby) : 1f;
            float target = Mathf.Min(loadN, timeN);                 // 1 only when both are satisfied

            shown = Mathf.MoveTowards(shown, target, barFillSpeed * Time.deltaTime);
            if (loadingBarFill != null) loadingBarFill.fillAmount = shown;

            if (target >= 1f && shown >= 0.999f) break;             // load ready + min standby + bar full
            await UniTask.Yield(PlayerLoopTiming.Update, ct);
        }

        if (loadingBarFill != null) loadingBarFill.fillAmount = 1f;

        // Activate the already-loaded scene — instant swap, no loading freeze.
        load.allowSceneActivation = true;
    }

    /// <summary>Quit the application (stops Play mode in the editor).</summary>
    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
