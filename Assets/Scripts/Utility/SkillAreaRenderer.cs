using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// Runtime AoE area indicator. Spawns a flat quad with the SkillArea shader.
/// </summary>
public class SkillAreaRenderer : MonoBehaviour
{
    private Material material;

    private static Shader cachedShader;
    private static Mesh cachedQuad;

    private const float GROUND_OFFSET = 0.05f;

    // Factory //

    /// <summary>
    /// Create and show an area indicator for the given shape.
    /// </summary>
    public static SkillAreaRenderer Create(AreaShapeData shape, Vector3 casterPos,
        Vector3 targetPos, Color? color = null)
    {
        GameObject go = new GameObject("SkillArea_Runtime");
        SkillAreaRenderer renderer = go.AddComponent<SkillAreaRenderer>();
        renderer.Init();
        renderer.Show(shape, casterPos, targetPos, color);
        return renderer;
    }

    // Init //

    private void Init()
    {
        if (cachedQuad == null) cachedQuad = BuildQuad();
        if (cachedShader == null) cachedShader = Shader.Find("Custom/SkillArea");

        MeshFilter filter = gameObject.AddComponent<MeshFilter>();
        filter.sharedMesh = cachedQuad;

        MeshRenderer meshRenderer = gameObject.AddComponent<MeshRenderer>();
        material = new Material(cachedShader);
        meshRenderer.material = material;
        meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        meshRenderer.receiveShadows = false;
    }

    // Show / Hide //

    /// <summary>
    /// Configure and display the area for the given shape.
    /// Y-axis is ignored; the indicator always snaps to ground level.
    /// </summary>
    public void Show(AreaShapeData shape, Vector3 casterPos, Vector3 targetPos,
        Color? color = null)
    {
        material.SetColor("_MainColor", color ?? new Color(1f, 0f, 0f, 0.4f));

        // Flatten to XZ plane
        Vector3 forward = new Vector3(targetPos.x - casterPos.x, 0f, targetPos.z - casterPos.z);
        if (forward.sqrMagnitude < 0.001f) forward = Vector3.forward;
        forward.Normalize();

        Quaternion lookRot = Quaternion.LookRotation(forward, Vector3.up);
        Quaternion flatRot = Quaternion.Euler(90f, 0f, 0f);

        switch (shape.shapeType)
        {
            case AreaShapeType.Circle:
                float d = shape.radius * 2f;
                transform.position   = new Vector3(targetPos.x, GROUND_OFFSET, targetPos.z);
                transform.rotation   = flatRot;
                transform.localScale = new Vector3(d, d, 1f);
                material.SetFloat("_ShapeType", 0f);
                break;

            case AreaShapeType.Cone:
                float r2 = shape.range * 2f;
                transform.position   = new Vector3(casterPos.x, GROUND_OFFSET, casterPos.z);
                transform.rotation   = lookRot * flatRot;
                transform.localScale = new Vector3(r2, r2, 1f);
                material.SetFloat("_ShapeType", 1f);
                material.SetFloat("_Angle", shape.angle);
                break;

            case AreaShapeType.Laser:
                Vector3 center = new Vector3(casterPos.x, 0f, casterPos.z) + forward * (shape.length * 0.5f);
                center.y = GROUND_OFFSET;
                transform.position   = center;
                transform.rotation   = lookRot * flatRot;
                transform.localScale = new Vector3(shape.width, shape.length, 1f);
                material.SetFloat("_ShapeType", 2f);
                break;
        }

        gameObject.SetActive(true);
    }

    /// <summary>
    /// Destroy the indicator immediately.
    /// </summary>
    public void Hide()
    {
        Destroy(gameObject);
    }

    /// <summary>
    /// Show for a duration then auto-destroy.
    /// </summary>
    public async UniTask ShowForDuration(float duration, CancellationToken ct = default)
    {
        try
        {
            await UniTask.WaitForSeconds(duration, cancellationToken: ct);
        }
        finally
        {
            if (this != null) Hide();
        }
    }

    // Mesh //

    private static Mesh BuildQuad()
    {
        Mesh mesh = new Mesh { name = "SkillAreaQuad" };
        mesh.vertices = new[]
        {
            new Vector3(-0.5f, -0.5f, 0f),
            new Vector3( 0.5f, -0.5f, 0f),
            new Vector3(-0.5f,  0.5f, 0f),
            new Vector3( 0.5f,  0.5f, 0f)
        };
        mesh.uv = new[]
        {
            new Vector2(0f, 0f),
            new Vector2(1f, 0f),
            new Vector2(0f, 1f),
            new Vector2(1f, 1f)
        };
        mesh.triangles = new[] { 0, 2, 1, 2, 3, 1 };
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    // Cleanup //

    private void OnDestroy()
    {
        if (material != null)
        {
            Destroy(material);
            material = null;
        }
    }
}
