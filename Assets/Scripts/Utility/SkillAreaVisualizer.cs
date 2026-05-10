#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
public class SkillAreaVisualizer : MonoBehaviour
{
        [Header("Skill Data to test")]
        public AoESkill testSkill;

        [Header("transform setting")]
        [Tooltip("startPos")]
        public Transform startTransform;

        [Tooltip("targetPos")]
        public Transform targetTransform;

        private void OnDrawGizmos()
        {
            if (testSkill == null || startTransform == null || targetTransform == null) return;

            AreaShapeData shape = testSkill.areaShape;
            Vector3 origin = startTransform.position;

            Vector3 forward = startTransform.forward;
            if (targetTransform != null)
            {
                forward = (targetTransform.position - startTransform.position).normalized;
                forward.y = 0f; // ignore Y (height)
                // if start == target
                if (forward == Vector3.zero) forward = startTransform.forward;
            }

            // calculate rotation
            Quaternion rotation = Quaternion.LookRotation(forward);

            // gizmo color set
            Gizmos.color = new Color(1f, 0f, 0f, 0.3f);

            switch (shape.shapeType)
            {
                case AreaShapeType.Circle:
                    // Circle centered on target (Y flattened to caster height)
                    Vector3 circleCenter = targetTransform.position;
                    circleCenter.y = origin.y;
                    Gizmos.DrawSphere(circleCenter, shape.radius);
                    break;

                case AreaShapeType.Cone:
#if UNITY_EDITOR
                Vector3 leftBoundary = Quaternion.Euler(0, -shape.angle / 2f, 0) * forward;
                Handles.DrawSolidArc(origin, Vector3.up, leftBoundary, shape.angle, shape.range);
#endif
                break;

                case AreaShapeType.Laser:
                    // Rectangle from caster forward (Y flattened)
                    Gizmos.matrix = Matrix4x4.TRS(origin, rotation, Vector3.one);
                    Vector3 center = new Vector3(0, 0, shape.length / 2f);
                    Vector3 size = new Vector3(shape.width, 0.1f, shape.length);
                    Gizmos.DrawCube(center, size);
                    break;
            }

            // draw target arrow code
            if (targetTransform != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(startTransform.position, targetTransform.position);
            }
        }
}

