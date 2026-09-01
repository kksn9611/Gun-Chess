using UnityEngine;

/// <summary>Mixer routing category for a played sound.</summary>
public enum SoundCategory { Sfx, Ui, Music }

/// <summary>
/// A single play request. Every runtime AudioSource setting is an explicit field, so no
/// existing SFX config is lost when routing through SoundManager.
/// </summary>
public struct SfxParams
{
    public AudioClip clip;
    public float volume;          // linear 0..1
    public float pitch;           // playback pitch multiplier
    public float spatialBlend;    // 0 = 2D, 1 = 3D
    public Vector3 position;      // world position (used when spatialBlend > 0)
    public float delay;           // seconds before playback starts
    public SoundCategory category;// mixer routing
    public bool loop;             // looped voices play until Stop()
    public float lowPassCutoff;   // low-pass cutoff Hz; <= 0 means no filtering

    // Factories (bake today's defaults) //

    /// <summary>2D SFX with no distance falloff.</summary>
    public static SfxParams Sfx2D(AudioClip clip, float volume = 1f, float delay = 0f) => new SfxParams
    {
        clip = clip, volume = volume, pitch = 1f, spatialBlend = 0f,
        delay = delay, category = SoundCategory.Sfx
    };

    /// <summary>3D SFX positioned in the world.</summary>
    public static SfxParams Sfx3D(AudioClip clip, Vector3 position, float volume = 1f, float delay = 0f) => new SfxParams
    {
        clip = clip, volume = volume, pitch = 1f, spatialBlend = 1f,
        position = position, delay = delay, category = SoundCategory.Sfx
    };
}

/// <summary>
/// Lightweight reference to a playing voice. Generation guards against pool reuse, so a stale
/// handle can never stop a voice that has since been recycled.
/// </summary>
public readonly struct SoundHandle
{
    public readonly int index;
    public readonly int generation;

    public SoundHandle(int index, int generation)
    {
        this.index = index;
        this.generation = generation;
    }

    public bool IsValid => generation > 0;
    public static SoundHandle None => new SoundHandle(-1, 0);
}
