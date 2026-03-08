using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways] // 이 스크립트를 에디터 상에서도 실행
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))] // MeshFilter MeshRenderer 유니티가 자동으로 추가
public class HexRenderer : MonoBehaviour
{
    Mesh m_mesh;
    MeshFilter m_meshFilter;
    MeshRenderer m_meshRenderer;

    public Material material;
    public float innerSize;
    public float outerSize = 1;
    public float height;
    public bool isFlatTopped;

    List<Face> m_faces;

    void GetComponentsIfNeeded()
    {
        // 컴포넌트가 비어있다면 GetComponent
        if (m_meshFilter == null) m_meshFilter = GetComponent<MeshFilter>();
        if (m_meshRenderer == null) m_meshRenderer = GetComponent<MeshRenderer>();
        if (m_mesh == null)
        {
            m_mesh = new Mesh();
            m_mesh.name = "Hex Mesh";
            m_meshFilter.sharedMesh = m_mesh; // 에디터에서는 sharedMesh 사용 필수
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
        m_mesh.Clear(); // 기존 메쉬 데이터 삭제

        DrawFaces(); // 육각형 계산
        CombineFaces(); // 계산 후 합치기
    }

    void DrawFaces() // 육각형 면 계산 함수
    {
        m_faces = new List<Face>();

        
        for (int point = 0; point < 6; point++) // 육각형 뚜껑
            m_faces.Add(CreateFace(innerSize, outerSize, height / 2f, height / 2f, point));

        for (int point = 0; point < 6; point++) // 육각형 바닥
            m_faces.Add(CreateFace(innerSize, outerSize, -height / 2f, -height / 2f, point, true));

        for (int point = 0; point < 6; point++) // 바깥쪽 벽면
            m_faces.Add(CreateFace(outerSize, outerSize, height / 2f, -height / 2f, point, true));

        for (int point = 0; point < 6; point++) // 안쪽 벽면
            m_faces.Add(CreateFace(innerSize, innerSize, height / 2f, -height / 2f, point, false));
    }

    void CombineFaces() // 면 합치기
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
            m_mesh.uv = uvs.ToArray(); //리스트에 담긴 데이터를 배열로
            m_mesh.RecalculateNormals(); // 표면의 빛 반사 방향을 재계산 빛을 제대로 받기 위해 필수
        }
    }

    protected Vector3 GetPoint(float size, float heightPos, int index) // 육각형 점 찍기
    {
        float angle_deg = isFlatTopped ? 60 * index : 60 * index - 30;
        float angle_rad = Mathf.PI / 180f * angle_deg;
        return new Vector3(size * Mathf.Cos(angle_rad), heightPos, size * Mathf.Sin(angle_rad));
    }

    // 사각형 면 만들기
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

// Face 구조체 (기존과 동일)
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