using UnityEngine;

public class BenchLayout : MonoBehaviour
{
    [Header("슬롯 설정")]
    [SerializeField] private int slotCount; // 9
    [SerializeField] private float outerSize; // 1
    [SerializeField] private float innerSize; // 0.9
    [SerializeField] private float tileHeight; // 0.02
    [SerializeField] private float spacing; // 0.125

    [Header("재질")]
    [SerializeField] private Material tileMaterial;

    // ─────────────────────────────────────────────────────────────

    private void Start()
    {
        LayoutBench();
    }

    /// <summary>
    /// 기존 타일을 제거하고 벤치 타일을 생성, 왼쪽 정렬
    /// </summary>
    public void LayoutBench()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
            DestroyImmediate(transform.GetChild(i).gameObject);

        if (slotCount <= 0) return;

        // 타일 한 칸의 전체 너비 = outerSize * 2, 슬롯 간 이동 거리 = 전체 너비 + 여백
        float tileFullWidth = outerSize * 2f;
        float step          = tileFullWidth + spacing;

        for (int i = 0; i < slotCount; i++)
        {
            GameObject tile = new GameObject(
                $"BenchSlot_{i}",
                typeof(BenchTileRenderer),
                typeof(BenchTileScript));

            tile.transform.SetParent(transform, false);
            tile.transform.localPosition = new Vector3(i * step, 0f, 0f);
            
            //타일 그리기
            BenchTileRenderer tileRenderer = tile.GetComponent<BenchTileRenderer>();
            tileRenderer.outerSize = outerSize;
            tileRenderer.innerSize = innerSize;
            tileRenderer.height    = tileHeight;
            tileRenderer.SetMaterial(tileMaterial);
            tileRenderer.DrawMesh();
            
            // 타일 등록
            BenchTileScript benchTileScript = tile.GetComponent<BenchTileScript>();
            benchTileScript.Initialize(i);
            BenchManager.Instance.RegisterTile(i, benchTileScript);

            // 클릭 판정용 콜라이더
            // innerSize > 0 이면 메시를 별도 생성한다.
            MeshCollider col = tile.AddComponent<MeshCollider>();
            col.sharedMesh = innerSize > 0f
                ? CreateSolidColliderMesh(outerSize, tileHeight)
                : tile.GetComponent<MeshFilter>().sharedMesh;
        }
    }

    /// <summary>
    /// innerSize > 0 일 때 클릭 판정 전용 솔리드 메시 생성
    /// HexGridLayout.CreateSolidHexMesh() 와 동일한 역할.
    /// </summary>
    private static Mesh CreateSolidColliderMesh(float outer, float h)
    {
        float top = h / 2f;

        // 상단 면만으로 구성된 꽉 찬 사각형 (두 개의 삼각형)
        var vertices = new[]
        {
            new Vector3(-outer, top, -outer),
            new Vector3( outer, top, -outer),
            new Vector3( outer, top,  outer),
            new Vector3(-outer, top,  outer),
        };
        var triangles = new[] { 0, 1, 2, 2, 3, 0 };

        Mesh mesh       = new Mesh();
        mesh.name       = "BenchColliderMesh";
        mesh.vertices   = vertices;
        mesh.triangles  = triangles;
        mesh.RecalculateNormals();
        return mesh;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        UnityEditor.EditorApplication.delayCall -= OnValidateDelayed;
        UnityEditor.EditorApplication.delayCall += OnValidateDelayed;
    }

    private void OnValidateDelayed()
    {
        UnityEditor.EditorApplication.delayCall -= OnValidateDelayed;
        if (this == null) return;
        LayoutBench();
    }
#endif
}
