using UnityEditor;
using UnityEngine;

/// <summary>
/// Draws the circle radius wireframe in Scene view when a SelfAoESkill SO is selected.
/// Allows visual matching of VFX scale to the area size.
/// </summary>
[CustomEditor(typeof(SelfAoESkill))]
public class SelfAoESkillEditor : Editor
{
    private static readonly Color wireColor = new Color(1f, 0.3f, 0.3f, 0.8f);
    private static readonly Color fillColor = new Color(1f, 0.3f, 0.3f, 0.15f);

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
        SelfAoESkill skill = (SelfAoESkill)target;
        if (skill == null) return;

        Vector3 origin = Vector3.zero;

        // Circle centered on the caster (origin)
        Handles.color = wireColor;
        Handles.DrawWireDisc(origin, Vector3.up, skill.radius);
        Handles.color = fillColor;
        Handles.DrawSolidDisc(origin, Vector3.up, skill.radius);

        // Draw label
        Handles.Label(origin + Vector3.up * 0.3f,
            $"{skill.skillName} (Self Circle)", EditorStyles.boldLabel);
    }
}
