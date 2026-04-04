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

            // 클릭 판정용 콜라이더 — BoxCollider 사용
            // MeshCollider 단면 메시는 레이캐스트가 불안정하므로 BoxCollider 를 사용한다.
            BoxCollider col = tile.AddComponent<BoxCollider>();
            col.size   = new Vector3(outerSize * 2f, Mathf.Max(tileHeight, 0.05f), outerSize * 2f);
            col.center = Vector3.zero;
        }
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
