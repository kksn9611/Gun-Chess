using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// Centralized audio service. Pools AudioSources for SFX/UI one-shots, drives BGM on a dedicated
/// crossfading pair, and resolves mixer groups once. Callers reference shared sounds by SoundId.
/// </summary>
public sealed class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [Header("Mixer")]
    [SerializeField] private AudioMixer mixer;              // Resources/Sound/Mixer
    [SerializeField] private string sfxGroup = "SFX";
    [SerializeField] private string uiGroup = "UI";        // falls back to SFX if the group is absent
    [SerializeField] private string musicGroup = "Music";  // falls back to Master if the group is absent

    [Header("Catalog")]
    [SerializeField] private SoundLibrary library;          // SoundId -> clip + params

    [Header("Voice Pool")]
    [SerializeField] private int initialVoices = 16;        // pre-instantiated idle voices
    [SerializeField] private int maxVoices = 64;            // hard cap; requests past it are dropped

    [Header("Music")]
    [SerializeField] private float defaultMusicFade = 1f;   // crossfade seconds

    // Voice //
    private const float LowPassOff = 22000f; // AudioLowPassFilter max cutoff = effectively bypassed

    private sealed class Voice
    {
        public AudioSource source;
        public AudioLowPassFilter lowPass; // per-voice filter; cutoff set per play
        public int generation;    // bumped on each reuse; validates SoundHandle
        public float releaseTime;  // Time.time when a one-shot frees (loops: never)
        public bool loop;
        public bool busy;
    }

    private readonly List<Voice> voices = new List<Voice>();
    private readonly Queue<int> idle = new Queue<int>();
    private readonly Dictionary<SoundCategory, AudioMixerGroup> groups = new Dictionary<SoundCategory, AudioMixerGroup>();

    // Music (dedicated crossfade pair, not pooled) //
    private AudioSource musicA;
    private AudioSource musicB;
    private AudioSource activeMusic;      // currently audible source
    private SoundId currentMusic = SoundId.None;
    private CancellationTokenSource musicCts;

    // Lifecycle //

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        ResolveGroups();
        for (int i = 0; i < initialVoices; i++) CreateVoice();
        CreateMusicSources();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        musicCts?.Cancel();
        musicCts?.Dispose();
    }

    private void Update()
    {
        // Reclaim finished one-shots into the idle pool.
        float now = Time.time;
        for (int i = 0; i < voices.Count; i++)
        {
            Voice v = voices[i];
            if (v.busy && !v.loop && now >= v.releaseTime) Release(i);
        }
    }

    // Public API //

    /// <summary>Play a fully specified request. Returns a handle usable to Stop() loops.</summary>
    public SoundHandle Play(in SfxParams p)
    {
        if (p.clip == null) return SoundHandle.None;

        int idx = AcquireVoice();
        if (idx < 0) return SoundHandle.None; // pool exhausted, request dropped

        Voice v = voices[idx];
        AudioSource s = v.source;
        s.clip = p.clip;
        s.volume = p.volume;
        s.pitch = p.pitch == 0f ? 1f : p.pitch;
        s.spatialBlend = p.spatialBlend;
        s.loop = p.loop;
        s.outputAudioMixerGroup = ResolveGroup(p.category);
        v.lowPass.cutoffFrequency = p.lowPassCutoff <= 0f ? LowPassOff : p.lowPassCutoff;
        if (p.spatialBlend > 0f) s.transform.position = p.position;

        if (p.delay > 0f) s.PlayDelayed(p.delay);
        else s.Play();

        v.loop = p.loop;
        v.releaseTime = p.loop ? float.MaxValue : Time.time + p.delay + p.clip.length / s.pitch;
        return new SoundHandle(idx, v.generation);
    }

    /// <summary>Play a 2D SFX (no distance falloff).</summary>
    public SoundHandle PlaySfx(AudioClip clip, float volume = 1f, float delay = 0f)
        => Play(SfxParams.Sfx2D(clip, volume, delay));

    /// <summary>Play a 3D SFX at a world position.</summary>
    public SoundHandle PlaySfxAt(AudioClip clip, Vector3 position, float volume = 1f, float delay = 0f)
        => Play(SfxParams.Sfx3D(clip, position, volume, delay));

    /// <summary>Stop a voice (mainly for loops) and return it to the pool. No-op on stale handles.</summary>
    public void Stop(SoundHandle handle)
    {
        if (!handle.IsValid || handle.index < 0 || handle.index >= voices.Count) return;
        Voice v = voices[handle.index];
        if (v.generation != handle.generation || !v.busy) return; // recycled or already free
        Release(handle.index);
    }

    // Catalog API //

    /// <summary>Play a catalogued sound by id (2D one-shot). Music ids are ignored here — use PlayMusic.</summary>
    public SoundHandle Play(SoundId id)
    {
        if (library == null || !library.TryGet(id, out SoundLibrary.Entry e)) return SoundHandle.None;
        return Play(SoundLibrary.ToParams(e));
    }

    /// <summary>Play a catalogued sound by id at a world position (3D).</summary>
    public SoundHandle PlayAt(SoundId id, Vector3 position)
    {
        if (library == null || !library.TryGet(id, out SoundLibrary.Entry e)) return SoundHandle.None;
        SfxParams p = SoundLibrary.ToParams(e);
        p.spatialBlend = 1f; p.position = position;
        return Play(p);
    }

    /// <summary>Play a catalogued UI sound (2D, UI mixer group).</summary>
    public SoundHandle PlayUi(SoundId id)
    {
        if (library == null || !library.TryGet(id, out SoundLibrary.Entry e)) return SoundHandle.None;
        SfxParams p = SoundLibrary.ToParams(e);
        p.category = SoundCategory.Ui;
        return Play(p);
    }

    // Music API //

    /// <summary>Crossfade BGM to a catalogued track. No-op if that track is already playing.</summary>
    public void PlayMusic(SoundId id, float fade = -1f)
    {
        if (id == currentMusic) return;
        if (library == null || !library.TryGet(id, out SoundLibrary.Entry e) || e.clip == null) return;
        currentMusic = id;
        PlayMusic(e.clip, fade < 0f ? defaultMusicFade : fade, e.volume <= 0f ? 1f : e.volume);
    }

    /// <summary>Crossfade BGM to a raw clip (loops by default).</summary>
    public void PlayMusic(AudioClip clip, float fade, float volume = 1f, bool loop = true)
    {
        if (clip == null) return;

        musicCts?.Cancel();
        musicCts = new CancellationTokenSource();

        AudioSource from = activeMusic;
        AudioSource to = (activeMusic == musicA) ? musicB : musicA;
        to.clip = clip;
        to.loop = loop;
        to.volume = 0f;
        to.Play();
        activeMusic = to;

        CrossfadeAsync(from, to, volume, fade, musicCts.Token).Forget();
    }

    /// <summary>Fade the current BGM out and stop.</summary>
    public void StopMusic(float fade = -1f)
    {
        currentMusic = SoundId.None;
        musicCts?.Cancel();
        musicCts = new CancellationTokenSource();
        if (activeMusic != null)
            CrossfadeAsync(activeMusic, null, 0f, fade < 0f ? defaultMusicFade : fade, musicCts.Token).Forget();
        activeMusic = null;
    }

    private async UniTaskVoid CrossfadeAsync(AudioSource from, AudioSource to, float targetVol, float duration, CancellationToken ct)
    {
        float fromStart = from != null ? from.volume : 0f;
        float t = 0f;
        while (t < duration)
        {
            if (ct.IsCancellationRequested) return;
            t += Time.unscaledDeltaTime;
            float k = duration > 0f ? Mathf.Clamp01(t / duration) : 1f;
            if (from != null) from.volume = Mathf.Lerp(fromStart, 0f, k);
            if (to != null) to.volume = Mathf.Lerp(0f, targetVol, k);
            await UniTask.Yield(ct);
        }
        if (from != null) { from.Stop(); from.volume = 0f; }
        if (to != null) to.volume = targetVol;
    }

    // Pool //

    private int AcquireVoice()
    {
        if (idle.Count > 0)
        {
            int idx = idle.Dequeue();
            voices[idx].busy = true;
            return idx;
        }
        if (voices.Count < maxVoices)
        {
            int idx = CreateVoice(startIdle: false);
            voices[idx].busy = true;
            return idx;
        }
        return -1;
    }

    private int CreateVoice(bool startIdle = true)
    {
        var go = new GameObject($"Voice_{voices.Count}");
        go.transform.SetParent(transform, false);
        var s = go.AddComponent<AudioSource>();
        s.playOnAwake = false;
        var lp = go.AddComponent<AudioLowPassFilter>();
        lp.cutoffFrequency = LowPassOff; // default bypassed

        var v = new Voice { source = s, lowPass = lp, generation = 1, busy = false };
        int idx = voices.Count;
        voices.Add(v);
        if (startIdle) idle.Enqueue(idx);
        return idx;
    }

    private void Release(int idx)
    {
        Voice v = voices[idx];
        v.source.Stop();
        v.source.clip = null;
        v.busy = false;
        v.loop = false;
        v.generation++; // invalidate outstanding handles
        idle.Enqueue(idx);
    }

    // Music sources //

    private void CreateMusicSources()
    {
        AudioMixerGroup g = ResolveGroup(SoundCategory.Music);
        musicA = CreateMusicSource("Music_A", g);
        musicB = CreateMusicSource("Music_B", g);
        activeMusic = musicA;
    }

    private AudioSource CreateMusicSource(string name, AudioMixerGroup group)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform, false);
        var s = go.AddComponent<AudioSource>();
        s.playOnAwake = false;
        s.spatialBlend = 0f;
        s.loop = true;
        s.volume = 0f;
        s.outputAudioMixerGroup = group;
        return s;
    }

    // Mixer Routing //

    private void ResolveGroups()
    {
        groups[SoundCategory.Sfx]   = FindGroup(sfxGroup);
        groups[SoundCategory.Ui]    = FindGroup(uiGroup) ?? FindGroup(sfxGroup); // UI falls back to SFX
        groups[SoundCategory.Music] = FindGroup(musicGroup);                     // null -> Master
    }

    private AudioMixerGroup ResolveGroup(SoundCategory category)
        => groups.TryGetValue(category, out AudioMixerGroup g) ? g : null;

    private AudioMixerGroup FindGroup(string groupName)
    {
        if (mixer == null || string.IsNullOrEmpty(groupName)) return null;
        AudioMixerGroup[] found = mixer.FindMatchingGroups(groupName);
        return found.Length > 0 ? found[0] : null;
    }
}
