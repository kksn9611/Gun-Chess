using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct LevelProbability
{
    public int[] tierWeights;
}

[CreateAssetMenu(fileName = "ProbabilityTable", menuName = "Scriptable Objects/ProbabilityTable")]
public class ProbabilityTable : ScriptableObject
{
    [Header("Table")]
    [SerializeField] private List<LevelProbability> probabilityByLevel;

    // Roll //

    /// <summary>Roll a cost tier (1-based) for the given player level, weighted by its row.</summary>
    public int RollCostTier(int level)
    {
        if (probabilityByLevel == null || probabilityByLevel.Count == 0) return 1;

        int row = Mathf.Clamp(level - 1, 0, probabilityByLevel.Count - 1);
        int[] weights = probabilityByLevel[row].tierWeights;
        if (weights == null || weights.Length == 0) return 1;

        int total = 0;
        foreach (int w in weights) total += w;
        if (total <= 0) return 1;

        int roll = Random.Range(0, total);
        for (int i = 0; i < weights.Length; i++)
        {
            roll -= weights[i];
            if (roll < 0) return i + 1; // tier index -> cost
        }
        return 1;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        // Return onn playing or executing game
        if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (probabilityByLevel == null) return;

        for (int i = 0; i < probabilityByLevel.Count; i++)
        {
            int[] weights = probabilityByLevel[i].tierWeights;

            if (weights == null || weights.Length == 0) continue;

            int sum = 0;
            bool hasNegative = false;

            for (int j = 0; j < weights.Length; j++)
            {
                if (weights[j] < 0) hasNegative = true;
                sum += weights[j];
            }

            // exception handle
            if (sum != 100 || hasNegative)
            {
                int difference = 100 - sum;
                weights[0] += difference;
            }

            if (weights[0] < 0 || hasNegative)
            {
                for (int j = 0; j < weights.Length; j++)
                {
                    weights[j] = 0;
                }
                weights[0] = 100;

                Debug.LogWarning($"[ProbabilityTable] Level {i + 1} Probability error. [100, 0, 0...] reset.", this);
            }
            else
            {
                Debug.LogWarning($"[ProbabilityTable] Level {i + 1} Probability error. Adjust 1 cost probability and set 100", this);
            }
        }
    }
#endif
}
