using System.Collections.Generic;
using UnityEngine;

public static class AreaTargetingUtility
{
    // Single Target Check //

    /// <summary>
    /// Check if target is within the area shape.
    /// Circle uses pivot as center. Cone/Laser use caster as origin.
    /// All checks ignore Y axis.
    /// </summary>
    public static bool IsTargetInShape(AreaShapeData shape, Transform caster, Transform target, Vector3 pivot)
    {
        if (caster == null || target == null) return false;

        Vector3 forward = caster.forward;
        forward.y = 0f;

        switch (shape.shapeType)
        {
            case AreaShapeType.Circle:
                Vector3 toPivot = target.position - pivot;
                toPivot.y = 0f;
                return toPivot.sqrMagnitude <= (shape.radius * shape.radius);

            case AreaShapeType.Cone:
                Vector3 toCone = target.position - caster.position;
                toCone.y = 0f;
                if (toCone.sqrMagnitude > (shape.range * shape.range)) return false;

                float targetAngle = Vector3.Angle(forward, toCone);
                return targetAngle <= (shape.angle / 2f);

            case AreaShapeType.Laser:
                Vector3 localPos = caster.InverseTransformPoint(target.position);
                localPos.y = 0f;
                bool isWithinLength = localPos.z >= 0f && localPos.z <= shape.length;
                bool isWithinWidth = Mathf.Abs(localPos.x) <= (shape.width / 2f);
                return isWithinLength && isWithinWidth;

            default:
                return false;
        }
    }

    // Multi-Target Collection //

    /// <summary>
    /// Collect all enemies within the AoE shape.
    /// Pivot is the center for Circle (typically primary target position).
    /// </summary>
    public static List<UnitController> GetTargetsInArea(AreaShapeData shape, UnitController caster, Vector3 pivot)
    {
        List<UnitController> hits = new List<UnitController>();
        IReadOnlyList<UnitController> enemies = UnitManager.Instance.GetEnemiesOf(caster.CurrentTeam);

        foreach (UnitController enemy in enemies)
        {
            if (enemy == null || enemy.AI.CurrentState == UnitState.Dead) continue;
            if (IsTargetInShape(shape, caster.transform, enemy.transform, pivot))
                hits.Add(enemy);
        }
        return hits;
    }
}