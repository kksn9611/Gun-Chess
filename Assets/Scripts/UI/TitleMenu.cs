using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Title screen actions: start the game (load the main scene) or quit.</summary>
public class TitleMenu : MonoBehaviour
{
    [Tooltip("Scene loaded by Game Start (must be in Build Settings)")]
    [SerializeField] private string mainSceneName = "Main";

    /// <summary>Load the main gameplay scene.</summary>
    public void LoadMainScene() => SceneManager.LoadScene(mainSceneName);

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
