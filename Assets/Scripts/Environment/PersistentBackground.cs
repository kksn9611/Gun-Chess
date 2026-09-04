using DG.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Background skybox that persists across scene loads (DontDestroyOnLoad) and owns the whole rotation
/// hand-off, so the sky is a single continuous object through the Title -> Main swap:
///   Title (on Game Start):  accelerate rotation 1 -> -50   (ease-in).
///   Main  (on scene load):  instant flip -50 -> +50, then decelerate +50 -> 1   (ease-out).
/// Both scenes author a Background for editing; at runtime the FIRST one to load persists, and any later
/// duplicate deactivates + destroys itself. Uses a material instance so the shared asset is never touched.
/// </summary>
[DisallowMultipleComponent]
public class PersistentBackground : MonoBehaviour
{
    public static PersistentBackground Instance { get; private set; }

    [SerializeField] private Renderer skyboxRenderer;               // defaults to this object's renderer
    [SerializeField] private string speedProperty = "_RotationSpeed";
    [SerializeField] private string mainSceneName = "Main";

    [Header("Title -> accelerate out (1 -> -30)")]
    [SerializeField] private float titleFromSpeed = 1f;             // idle speed
    [SerializeField] private float titleToSpeed = -30f;             // wind up to fast reverse
    [SerializeField] private float titleDuration = 3f;
    [SerializeField] private Ease titleEase = Ease.InQuart;

    [Header("Main -> flip + decelerate in (-30 -> 30 -> 1)")]
    [SerializeField] private float mainFlipSpeed = 30f;            // instant flip target
    [SerializeField] private float mainToSpeed = 1f;               // settle to rest
    [SerializeField] private float mainDuration = 3f;
    [SerializeField] private Ease mainEase = Ease.InQuart;        

    private Material sourceMaterial; // pristine shared asset the instance is rebuilt from
    private Material mat;
    private Tween speedTween;
    private bool mainPlayed;

    public float TitleDuration => titleDuration;

    private void Awake()
    {
        // Duplicate reconciliation: a persistent Background already exists -> this copy is redundant.
        if (Instance != null && Instance != this)
        {
            gameObject.SetActive(false);                            // never render even one frame
            Destroy(gameObject);
            return;
        }

        Instance = this;
        transform.SetParent(null);                                 // DontDestroyOnLoad requires a root
        DontDestroyOnLoad(gameObject);

        if (skyboxRenderer == null) skyboxRenderer = GetComponent<Renderer>();
        if (skyboxRenderer != null)
        {
            sourceMaterial = skyboxRenderer.sharedMaterial;        // pristine source (before instancing)
            mat = skyboxRenderer.material;                         // instance (asset untouched)
        }

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void Start()
    {
        // Direct Main launch (no Title first): sceneLoaded doesn't fire for the initial scene, so kick
        // the Main half here instead.
        if (SceneManager.GetActiveScene().name == mainSceneName) PlayMainIntro();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            Instance = null;
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == mainSceneName)
        {
            PlayMainIntro();                                       // Title -> Main hand-off
        }
        else
        {
            mainPlayed = false;                                    // back at Title -> re-arm the Main intro
            RecreateMaterial();                                    // fresh skybox instance for the Title
        }
    }

    /// <summary>Rebuild the skybox material instance from the source asset — a clean state on return to
    /// Title (no drifted rotation speed / properties carried over from Main).</summary>
    private void RecreateMaterial()
    {
        if (skyboxRenderer == null || sourceMaterial == null) return;
        speedTween?.Kill();                                        // old tween targets the instance we're replacing
        speedTween = null;
        skyboxRenderer.sharedMaterial = sourceMaterial;           // detach the current instance from the renderer
        if (mat != null) Destroy(mat);                            // free the old instance (no leak on repeated returns)
        mat = skyboxRenderer.material;                            // fresh instance of the pristine source
    }

    /// <summary>Title half: accelerate rotation from idle (1) to -50. Call on Game Start.</summary>
    public void PlayTitleIntro() => EaseSpeed(titleFromSpeed, titleToSpeed, titleDuration, titleEase);

    /// <summary>Main half: instant flip to +50, then decelerate to 1. Runs once.</summary>
    public void PlayMainIntro()
    {
        if (mainPlayed) return;
        mainPlayed = true;
        EaseSpeed(mainFlipSpeed, mainToSpeed, mainDuration, mainEase);
    }

    // Snap to 'from' (the instant flip), then tween to 'to'. Continues running across a scene swap.
    private void EaseSpeed(float from, float to, float duration, Ease ease)
    {
        if (mat == null || !mat.HasProperty(speedProperty)) return;
        speedTween?.Kill();
        mat.SetFloat(speedProperty, from);
        speedTween = DOTween.To(() => mat.GetFloat(speedProperty),
                                v => mat.SetFloat(speedProperty, v),
                                to, duration)
                            .SetEase(ease)
                            .SetLink(gameObject);
    }
}
