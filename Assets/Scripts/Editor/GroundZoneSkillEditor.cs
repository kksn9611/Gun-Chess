using UnityEditor;
using UnityEngine;

/// <summary>
/// Draws the pool radius circle in Scene view when a GroundZoneSkill SO is selected.
/// Allows visual matching of VFX scale to the area size.
/// </summary>
[CustomEditor(typeof(GroundZoneSkill))]
public class GroundZoneSkillEditor : Editor
{
    private static readonly Color wireColor = new Color(0.7f, 0.3f, 1f, 0.8f);
    private static readonly Color fillColor = new Color(0.7f, 0.3f, 1f, 0.15f);

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
        GroundZoneSkill skill = (GroundZoneSkill)target;
        if (skill == null) return;

        Vector3 origin = Vector3.zero;

        // Pool circle centered on the landing point (origin)
        Handles.color = wireColor;
        Handles.DrawWireDisc(origin, Vector3.up, skill.radius);
        Handles.color = fillColor;
        Handles.DrawSolidDisc(origin, Vector3.up, skill.radius);

        // Draw label
        Handles.Label(origin + Vector3.up * 0.3f,
            $"{skill.skillName} (Pool)", EditorStyles.boldLabel);
    }
}
