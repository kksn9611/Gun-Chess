using UnityEngine;

/// <summary>
/// Timing for the anti-stall trigger. One-shot when stackInterval &lt;= 0, else repeats on that cadence.
/// </summary>
[CreateAssetMenu(fileName = "AntiStallConfig", menuName = "Scriptable Objects/Combat/AntiStallConfig")]
public class AntiStallConfig : ScriptableObject
{
    [Tooltip("Seconds into combat before the first trigger")]
    public float initialDelay = 15f;

    [Tooltip("Seconds between repeat triggers; <= 0 = single trigger")]
    public float stackInterval = 0f;

    [Tooltip("Max triggers; 0 = unlimited until battle ends")]
    public int maxStacks = 0;
}
