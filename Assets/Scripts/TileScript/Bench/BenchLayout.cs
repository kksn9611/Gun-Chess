using UnityEngine;

public class BenchLayout : MonoBehaviour
{
    [Header("Slot Settings")]
    [SerializeField] private int slotCount; // 9
    [SerializeField] private float outerSize; // 1
    [SerializeField] private float innerSize; // 0.9
    [SerializeField] private float tileHeight; // 0.02
    [SerializeField] private float spacing; // 0.125

    [Header("Material")]
    [SerializeField] private Material tileMaterial;

    // ─────────────────────────────────────────────────────────────

    private void Start()
    {
        LayoutBench();
    }

    /// <summary>
    /// Destroy existing tiles and create bench tiles, left-aligned.
    /// </summary>
    public void LayoutBench()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
            DestroyImmediate(transform.GetChild(i).gameObject);

        if (slotCount <= 0) return;

        // Full width of one tile = outerSize * 2, step = full width + spacing
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

            // Draw tile mesh
            BenchTileRenderer tileRenderer = tile.GetComponent<BenchTileRenderer>();
            tileRenderer.outerSize = outerSize;
            tileRenderer.innerSize = innerSize;
            tileRenderer.height    = tileHeight;
            tileRenderer.SetMaterial(tileMaterial);
            tileRenderer.DrawMesh();

            // Register tile
            BenchTileScript benchTileScript = tile.GetComponent<BenchTileScript>();
            benchTileScript.Initialize(i);
            BenchManager.Instance.RegisterTile(i, benchTileScript);

            // BoxCollider for click detection
            // MeshCollider with thin mesh is unreliable for raycasts, so use BoxCollider instead
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
