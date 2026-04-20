using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))] // MeshFilter & MeshRenderer auto-added by Unity
public class HexRenderer : MonoBehaviour
{
    Mesh m_mesh;
    MeshFilter m_meshFilter;
    MeshRenderer m_meshRenderer;

    public Material material;
    public float innerSize;
    public float outerSize = 1;
    public float height;
    public bool isFlatTopped = false; // Hex orientation — always false!
    public bool IsFlatTopped => isFlatTopped;
    List<Face> m_faces;

    void GetComponentsIfNeeded()
    {
        // Lazy-init components
        if (m_meshFilter == null) m_meshFilter = GetComponent<MeshFilter>();
        if (m_meshRenderer == null) m_meshRenderer = GetComponent<MeshRenderer>();
        if (m_mesh == null)
        {
            m_mesh = new Mesh();
            m_mesh.name = "Hex Mesh";
            m_meshFilter.sharedMesh = m_mesh; // Must use sharedMesh in editor
        }
    }

    public void SetMaterial(Material mat)
    {
        GetComponentsIfNeeded();
        material = mat;
        m_meshRenderer.sharedMaterial = material;
    }

    public void DrawMesh()
    {
        GetComponentsIfNeeded();
        m_mesh.Clear(); // Clear existing mesh data

        DrawFaces(); // Calculate hex faces
        CombineFaces(); // Combine into single mesh
    }

    void DrawFaces() // Calculate hex faces
    {
        m_faces = new List<Face>();


        for (int point = 0; point < 6; point++) // Top cap
            m_faces.Add(CreateFace(innerSize, outerSize, height / 2f, height / 2f, point));

        for (int point = 0; point < 6; point++) // Bottom cap
            m_faces.Add(CreateFace(innerSize, outerSize, -height / 2f, -height / 2f, point, true));

        for (int point = 0; point < 6; point++) // Outer walls
            m_faces.Add(CreateFace(outerSize, outerSize, height / 2f, -height / 2f, point, true));

        for (int point = 0; point < 6; point++) // Inner walls
            m_faces.Add(CreateFace(innerSize, innerSize, height / 2f, -height / 2f, point, false));
    }

    void CombineFaces() // Merge faces into mesh
    {
        List<Vector3> vertices = new List<Vector3>();
        List<int> tris = new List<int>();
        List<Vector2> uvs = new List<Vector2>();

        for (int i = 0; i < m_faces.Count; i++)
        {
            vertices.AddRange(m_faces[i].vertices);
            uvs.AddRange(m_faces[i].uvs);

            int offset = (4 * i);
            foreach (int triangle in m_faces[i].triangles)
            {
                tris.Add(triangle + offset);
            }
        }

        if (m_mesh != null)
        {
            m_mesh.vertices = vertices.ToArray();
            m_mesh.triangles = tris.ToArray();
            m_mesh.uv = uvs.ToArray(); // List data to arrays
            m_mesh.RecalculateNormals(); // Recalculate normals for proper lighting
        }
    }

    protected Vector3 GetPoint(float size, float heightPos, int index) // Hex vertex calculation
    {
        float angle_deg = isFlatTopped ? 60 * index : 60 * index - 30;
        float angle_rad = Mathf.PI / 180f * angle_deg;
        return new Vector3(size * Mathf.Cos(angle_rad), heightPos, size * Mathf.Sin(angle_rad));
    }

    // Create a quad face
    Face CreateFace(float innerRad, float outerRad, float heightA, float heightB, int point, bool reverse = false)
    {
        Vector3 pointA = GetPoint(innerRad, heightB, point);
        Vector3 pointB = GetPoint(innerRad, heightB, (point < 5) ? point + 1 : 0);
        Vector3 pointC = GetPoint(outerRad, heightA, (point < 5) ? point + 1 : 0);
        Vector3 pointD = GetPoint(outerRad, heightA, point);

        List<Vector3> vertices = new List<Vector3>() { pointA, pointB, pointC, pointD };
        List<int> triangles = new List<int>() { 0, 1, 2, 2, 3, 0 };
        List<Vector2> uvs = new List<Vector2>() { new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1) };
        if (reverse) vertices.Reverse();

        return new Face(vertices, triangles, uvs);
    }
}

// Face struct (unchanged)
public struct Face
{
    public List<Vector3> vertices { get; private set; }
    public List<int> triangles { get; private set; }
    public List<Vector2> uvs { get; private set; }

    public Face(List<Vector3> v, List<int> t, List<Vector2> u)
    {
        vertices = v; triangles = t; uvs = u;
    }
}
