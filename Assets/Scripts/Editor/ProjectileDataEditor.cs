using UnityEditor;
using UnityEngine;

/// <summary>
/// Draws explosion radius wireframe in Scene view when ProjectileData SO is selected.
/// </summary>
[CustomEditor(typeof(ProjectileData))]
public class ProjectileDataEditor : Editor
{
    private static readonly Color wireColor = new Color(1f, 0.6f, 0.1f, 0.8f);
    private static readonly Color fillColor = new Color(1f, 0.6f, 0.1f, 0.15f);

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
        ProjectileData data = (ProjectileData)target;
        if (data == null || !data.useExplosion) return;

        Vector3 origin = Vector3.zero;

        Handles.color = wireColor;
        Handles.DrawWireDisc(origin, Vector3.up, data.explodeRadius);

        Handles.color = fillColor;
        Handles.DrawSolidDisc(origin, Vector3.up, data.explodeRadius);

        Handles.Label(origin + Vector3.up * 0.3f,
            $"{data.name} (Explosion r={data.explodeRadius})", EditorStyles.boldLabel);
    }
}
