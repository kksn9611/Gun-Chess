using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Shown after the final stage is cleared: dims the screen (like the augment select) and offers
/// "Return to Title" / "Quit". Hidden via CanvasGroup so the component stays active until Show().
/// </summary>
public class GameClearUI : MonoBehaviour
{
    [SerializeField] private CanvasGroup group;               // panel show/hide + screen dim
    [SerializeField] private string titleSceneName = "Title"; // scene loaded by ReturnToTitle

    private void Awake() => SetVisible(false);

    /// <summary>Reveal the game-clear panel (dim + buttons).</summary>
    public void Show() => SetVisible(true);

    /// <summary>Return-to-Title button: load the Title scene.</summary>
    public void ReturnToTitle() => SceneManager.LoadScene(titleSceneName);

    /// <summary>Quit button: quit the application (stops Play mode in the editor).</summary>
    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void SetVisible(bool on)
    {
        if (group == null) return;
        group.alpha = on ? 1f : 0f;
        group.interactable = on;
        group.blocksRaycasts = on;
    }
}
