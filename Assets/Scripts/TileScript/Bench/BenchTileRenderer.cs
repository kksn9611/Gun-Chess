using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class BenchTileRenderer : MonoBehaviour
{
    public float    innerSize = 0f;
    public float    outerSize = 1f;
    public float    height    = 0.05f;
    public Material material;

    private Mesh         m_mesh;
    private MeshFilter   m_meshFilter;
    private MeshRenderer m_meshRenderer;
    List<Face> m_faces;

    // ─────────────────────────────────────────────────────────────

    private void GetComponentsIfNeeded()
    {
        if (m_meshFilter   == null) m_meshFilter   = GetComponent<MeshFilter>();
        if (m_meshRenderer == null) m_meshRenderer = GetComponent<MeshRenderer>();
        if (m_mesh == null)
        {
            m_mesh      = new Mesh();
            m_mesh.name = "Bench Tile Mesh";
            m_meshFilter.sharedMesh = m_mesh;
        }
    }

    public void SetMaterial(Material mat)
    {
        GetComponentsIfNeeded();
        material = mat;
        m_meshRenderer.sharedMaterial = mat;
    }

    public void DrawMesh()
    {
        GetComponentsIfNeeded();
        m_mesh.Clear();

        DrawFaces();
        CombineFaces();
    }

    // Face generation — same structure as HexRenderer.DrawFaces()
    void DrawFaces()
    {
        m_faces = new List<Face>();

        for (int side = 0; side < 4; side++) // Top frame
            m_faces.Add(CreateFace(innerSize, outerSize,  height / 2f,  height / 2f, side));

        for (int side = 0; side < 4; side++) // Bottom frame
            m_faces.Add(CreateFace(innerSize, outerSize, -height / 2f, -height / 2f, side, true));

        for (int side = 0; side < 4; side++) // Outer walls
            m_faces.Add(CreateFace(outerSize, outerSize,  height / 2f, -height / 2f, side, true));

        for (int side = 0; side < 4; side++) // Inner walls
            m_faces.Add(CreateFace(innerSize, innerSize,  height / 2f, -height / 2f, side));
    }

    void CombineFaces()
    {
        var vertices  = new List<Vector3>();
        var tris      = new List<int>();
        var uvs       = new List<Vector2>();

        for (int i = 0; i < m_faces.Count; i++)
        {
            vertices.AddRange(m_faces[i].vertices);
            uvs.AddRange(m_faces[i].uvs);

            int offset = 4 * i;
            foreach (int t in m_faces[i].triangles)
                tris.Add(t + offset);
        }

        m_mesh.vertices  = vertices.ToArray();
        m_mesh.triangles = tris.ToArray();
        m_mesh.uv        = uvs.ToArray();
        m_mesh.RecalculateNormals();
    }

    // Same structure as HexRenderer.CreateFace() / GetPoint()
    Face CreateFace(float innerRad, float outerRad,
                    float heightA,  float heightB,
                    int   side,     bool  reverse = false)
    {
        int next = (side + 1) % 4;

        Vector3 pointA = GetPoint(innerRad, heightB, side);
        Vector3 pointB = GetPoint(innerRad, heightB, next);
        Vector3 pointC = GetPoint(outerRad, heightA, next);
        Vector3 pointD = GetPoint(outerRad, heightA, side);

        var verts = new List<Vector3>() { pointA, pointB, pointC, pointD };
        var tris  = new List<int>()     { 0, 1, 2, 2, 3, 0 };
        var uvs   = new List<Vector2>() {
            new Vector2(0, 0), new Vector2(1, 0),
            new Vector2(1, 1), new Vector2(0, 1)
        };

        if (reverse) verts.Reverse();

        return new Face(verts, tris, uvs);
    }

    /// <summary>
    /// Return vertex position for a square tile.
    /// </summary>
    Vector3 GetPoint(float size, float heightPos, int index)
    {
        switch (index % 4)
        {
            case 0:  return new Vector3(-size, heightPos, -size);
            case 1:  return new Vector3( size, heightPos, -size);
            case 2:  return new Vector3( size, heightPos,  size);
            default: return new Vector3(-size, heightPos,  size);
        }
    }
}
