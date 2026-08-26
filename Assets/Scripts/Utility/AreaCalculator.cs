using System.Collections.Generic;
using UnityEngine;

public static class AreaTargetingUtility
{
    // Single Target Check //

    /// <summary>
    /// Check if targetPos is within the area shape.
    /// All positions must be Y-flattened before calling.
    /// </summary>
    public static bool IsTargetInShape(AreaShapeData shape, Vector3 origin, Vector3 forward, Vector3 targetPos, Vector3 pivot)
    {
        switch (shape.shapeType)
        {
            case AreaShapeType.Circle:
                Vector3 toPivot = targetPos - pivot;
                return toPivot.sqrMagnitude <= (shape.radius * shape.radius);

            case AreaShapeType.Cone:
                Vector3 toCone = targetPos - origin;
                if (toCone.sqrMagnitude > (shape.range * shape.range)) return false;
                float targetAngle = Vector3.Angle(forward, toCone);
                return targetAngle <= (shape.angle / 2f);

            case AreaShapeType.Laser:
                Quaternion rot = Quaternion.LookRotation(forward, Vector3.up);
                Vector3 localPos = Quaternion.Inverse(rot) * (targetPos - origin);
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
    /// Origin = caster FirePoint (Y ignored), hit detection = enemy HitBox (Y ignored).
    /// </summary>
    public static List<UnitController> GetTargetsInArea(AreaShapeData shape, UnitController caster, Vector3 pivot)
    {
        List<UnitController> hits = new List<UnitController>();
        IReadOnlyList<UnitController> enemies = UnitManager.Instance.GetEnemiesOf(caster.CurrentTeam);

        Vector3 origin = caster.Visuals.FirePoint.position;
        origin.y = 0f;

        Vector3 forward = pivot - origin;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.001f)
        {
            forward = caster.transform.forward;
            forward.y = 0f;
        }
        forward.Normalize();

        pivot.y = 0f;

        foreach (UnitController enemy in enemies)
        {
            if (enemy == null || enemy.AI.CurrentState == UnitState.Dead) continue;

            Vector3 targetPos = enemy.Visuals.HitBox.position;
            targetPos.y = 0f;

            if (IsTargetInShape(shape, origin, forward, targetPos, pivot))
                hits.Add(enemy);
        }
        return hits;
    }

    /// <summary>
    /// Collect all enemies within a circle at a world position. Team-based, no caster needed.
    /// </summary>
    public static List<UnitController> GetTargetsInCircle(Vector3 center, float radius, Team attackerTeam)
    {
        List<UnitController> hits = new List<UnitController>();
        IReadOnlyList<UnitController> enemies = UnitManager.Instance.GetEnemiesOf(attackerTeam);
        center.y = 0f;
        float sqrRadius = radius * radius;

        foreach (UnitController enemy in enemies)
        {
            if (enemy == null || enemy.AI.CurrentState == UnitState.Dead) continue;

            Vector3 targetPos = enemy.Visuals.HitBox.position;
            targetPos.y = 0f;

            if ((targetPos - center).sqrMagnitude <= sqrRadius)
                hits.Add(enemy);
        }
        return hits;
    }

    /// <summary>
    /// Collect all living allies within a circle at a world position. Team-based, no caster needed.
    /// </summary>
    public static List<UnitController> GetAlliesInCircle(Vector3 center, float radius, Team team)
    {
        List<UnitController> hits = new List<UnitController>();
        IReadOnlyList<UnitController> allies = UnitManager.Instance.GetAlliesOf(team);
        center.y = 0f;
        float sqrRadius = radius * radius;

        foreach (UnitController ally in allies)
        {
            if (ally == null || ally.AI.CurrentState == UnitState.Dead) continue;

            Vector3 targetPos = ally.Visuals.HitBox.position;
            targetPos.y = 0f;

            if ((targetPos - center).sqrMagnitude <= sqrRadius)
                hits.Add(ally);
        }
        return hits;
    }

    // Heal Targeting //

    /// <summary>
    /// Return up to count living allies sorted by lowest HP%. By default skips full-HP allies
    /// (heal targeting) and falls back to the caster if none are damaged. Pass includeFullHp=true
    /// for shield targeting, which picks the lowest-HP allies regardless of full HP.
    /// </summary>
    public static List<UnitController> FindLowestHpAllies(UnitController caster, int count, bool includeFullHp = false)
    {
        IReadOnlyList<UnitController> allies = UnitManager.Instance.GetAlliesOf(caster.CurrentTeam);
        List<UnitController> candidates = new List<UnitController>();

        foreach (UnitController ally in allies)
        {
            if (ally == null || ally.AI.CurrentState == UnitState.Dead) continue;
            if (!includeFullHp && ally.Stats.CurrentHp >= ally.Stats.CurrentMaxHp) continue; // heal: skip full HP
            candidates.Add(ally);
        }

        // Sort by HP% ascending
        candidates.Sort((a, b) =>
        {
            float pctA = a.Stats.CurrentHp / a.Stats.CurrentMaxHp;
            float pctB = b.Stats.CurrentHp / b.Stats.CurrentMaxHp;
            return pctA.CompareTo(pctB);
        });

        // Fallback to self if there are no candidates (e.g. heal mode with no one damaged)
        if (candidates.Count == 0)
        {
            candidates.Add(caster);
            return candidates;
        }

        if (candidates.Count > count)
            candidates.RemoveRange(count, candidates.Count - count);

        return candidates;
    }

    /// <summary>
    /// Collect all enemies within an area shape at a world position. Direction-based, no caster needed.
    /// </summary>
    public static List<UnitController> GetTargetsInArea(AreaShapeData shape, Vector3 origin, Vector3 forward, Team attackerTeam)
    {
        List<UnitController> hits = new List<UnitController>();
        IReadOnlyList<UnitController> enemies = UnitManager.Instance.GetEnemiesOf(attackerTeam);

        origin.y = 0f;
        forward.y = 0f;
        forward.Normalize();

        foreach (UnitController enemy in enemies)
        {
            if (enemy == null || enemy.AI.CurrentState == UnitState.Dead) continue;

            Vector3 targetPos = enemy.Visuals.HitBox.position;
            targetPos.y = 0f;

            if (IsTargetInShape(shape, origin, forward, targetPos, origin))
                hits.Add(enemy);
        }
        return hits;
    }
}
