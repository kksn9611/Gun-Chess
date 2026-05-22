using UnityEditor;
using UnityEngine;

/// <summary>
/// Draws area shape wireframe in Scene view when AoESkill SO is selected.
/// Allows visual matching of VFX scale to indicator size.
/// </summary>
[CustomEditor(typeof(AoESkill))]
public class AoESkillEditor : Editor
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
        AoESkill skill = (AoESkill)target;
        if (skill == null) return;

        AreaShapeData shape = skill.areaShape;
        Vector3 origin = Vector3.zero;
        Vector3 forward = Vector3.forward;

        Handles.color = wireColor;

        switch (shape.shapeType)
        {
            case AreaShapeType.Circle:
                DrawCircle(origin, shape.radius);
                break;
            case AreaShapeType.Cone:
                DrawCone(origin, forward, shape.range, shape.angle);
                break;
            case AreaShapeType.Laser:
                DrawLaser(origin, forward, shape.width, shape.length);
                break;
        }

        // Draw label
        Handles.Label(origin + Vector3.up * 0.3f,
            $"{skill.skillName} ({shape.shapeType})", EditorStyles.boldLabel);
    }

    // Shape Drawing //

    private void DrawCircle(Vector3 center, float radius)
    {
        Handles.DrawWireDisc(center, Vector3.up, radius);
        Handles.color = fillColor;
        Handles.DrawSolidDisc(center, Vector3.up, radius);
    }

    private void DrawCone(Vector3 origin, Vector3 forward, float range, float angle)
    {
        float halfRad = angle * 0.5f * Mathf.Deg2Rad;
        Quaternion leftRot = Quaternion.AngleAxis(-angle * 0.5f, Vector3.up);
        Quaternion rightRot = Quaternion.AngleAxis(angle * 0.5f, Vector3.up);

        Vector3 leftDir = leftRot * forward;
        Vector3 rightDir = rightRot * forward;
        Vector3 leftEnd = origin + leftDir * range;
        Vector3 rightEnd = origin + rightDir * range;

        // Wire edges
        Handles.DrawLine(origin, leftEnd);
        Handles.DrawLine(origin, rightEnd);
        Handles.DrawWireArc(origin, Vector3.up, leftDir, angle, range);

        // Solid fill
        Handles.color = fillColor;
        Handles.DrawSolidArc(origin, Vector3.up, leftDir, angle, range);
    }

    private void DrawLaser(Vector3 origin, Vector3 forward, float width, float length)
    {
        Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
        float halfW = width * 0.5f;

        Vector3 bl = origin - right * halfW;
        Vector3 br = origin + right * halfW;
        Vector3 tl = origin - right * halfW + forward * length;
        Vector3 tr = origin + right * halfW + forward * length;

        // Wire outline
        Handles.DrawLine(bl, br);
        Handles.DrawLine(br, tr);
        Handles.DrawLine(tr, tl);
        Handles.DrawLine(tl, bl);

        // Solid fill
        Handles.color = fillColor;
        Handles.DrawAAConvexPolygon(bl, br, tr, tl);
    }
}
