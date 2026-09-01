using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Catalog mapping SoundId -> clip + default playback params. Lets shared sounds (UI, BGM, level-up)
/// be referenced by id and re-authored without touching call sites.
/// </summary>
[CreateAssetMenu(fileName = "SoundLibrary", menuName = "Scriptable Objects/Audio/SoundLibrary")]
public sealed class SoundLibrary : ScriptableObject
{
    [System.Serializable]
    public struct Entry
    {
        public SoundId id;
        public AudioClip clip;
        [Range(0f, 1f)] public float volume;
        public SoundCategory category;
        [Tooltip("Low-pass cutoff Hz; 0 = no filtering")]
        public float lowPassCutoff;
        [Tooltip("Loop (BGM / ambience)")]
        public bool loop;
    }

    [SerializeField] private Entry[] entries;

    private Dictionary<SoundId, Entry> lookup;

    private void BuildLookup()
    {
        lookup = new Dictionary<SoundId, Entry>();
        if (entries == null) return;
        foreach (Entry e in entries)
            if (e.id != SoundId.None) lookup[e.id] = e;
    }

    /// <summary>Resolve an id to its entry. Rebuilds the lookup on first use / after domain reload.</summary>
    public bool TryGet(SoundId id, out Entry entry)
    {
        if (lookup == null) BuildLookup();
        return lookup.TryGetValue(id, out entry);
    }

    /// <summary>Build an SfxParams from a catalog entry (2D). Position/spatial handled by the caller.</summary>
    public static SfxParams ToParams(in Entry e) => new SfxParams
    {
        clip = e.clip,
        volume = e.volume <= 0f ? 1f : e.volume,
        pitch = 1f,
        spatialBlend = 0f,
        category = e.category,
        lowPassCutoff = e.lowPassCutoff,
        loop = e.loop
    };
}
