using UnityEditor;
using UnityEngine;

/// <summary>
/// Draws the healing zone radius circle in Scene view when a HealZoneSkill SO is selected.
/// Allows visual matching of VFX scale to the area size.
/// </summary>
[CustomEditor(typeof(HealZoneSkill))]
public class HealZoneSkillEditor : Editor
{
    private static readonly Color wireColor = new Color(0.3f, 1f, 0.4f, 0.8f);
    private static readonly Color fillColor = new Color(0.3f, 1f, 0.4f, 0.15f);

    private void OnEnable()
    {
        SceneView.duringSceneGui += OnSceneGUI;
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
    }

    private void OnSceneGUI(SceneView sceneView)
    {
        HealZoneSkill skill = (HealZoneSkill)target;
        if (skill == null) return;

        Vector3 origin = Vector3.zero;

        // Zone circle centered on the drop point (origin)
        Handles.color = wireColor;
        Handles.DrawWireDisc(origin, Vector3.up, skill.radius);
        Handles.color = fillColor;
        Handles.DrawSolidDisc(origin, Vector3.up, skill.radius);

        // Draw label
        Handles.Label(origin + Vector3.up * 0.3f,
            $"{skill.skillName} (Heal Zone)", EditorStyles.boldLabel);
    }
}
