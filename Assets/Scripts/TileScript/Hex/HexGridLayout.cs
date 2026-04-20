using UnityEngine;

public class HexGridLayout : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private Vector2Int gridSize;
    [SerializeField] private float outerSize; // 1.5
    [SerializeField] private float innerSize; // 1.4
    [SerializeField] private float height; // 0.01

    [Header("Material")]
    public Material material;

    private void Start()
    {
        LayoutGrid();
    }

    public void LayoutGrid()
    {
        // Clean up existing tiles
        TileManager.Instance.ClearMap();

        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(transform.GetChild(i).gameObject);
        }

        // Create and place new tiles
        for (int y = 0; y < gridSize.y; y++)
        {
            for (int x = 0; x < gridSize.x; x++)
            {

                // Create GameObject with HexRenderer + TileScript
                GameObject tile = new GameObject($"Tile_{x}_{y}", typeof(HexRenderer),typeof(TileScript));
                Vector2Int tileCoordinate = new Vector2Int(x,y);
                tile.transform.SetParent(transform, false);
                tile.transform.localPosition = GetPositionForHexFromCoordinate(tileCoordinate);
                TileScript tileScript = tile.GetComponent<TileScript>();
                tileScript.GridCoordinate = tileCoordinate;

                // Draw tile mesh
                HexRenderer hexRenderer = tile.GetComponent<HexRenderer>();
                hexRenderer.outerSize = outerSize;
                hexRenderer.innerSize = innerSize;
                hexRenderer.height = height;
                hexRenderer.SetMaterial(material);
                hexRenderer.DrawMesh();

                // Register tile
                TileManager.Instance.RegisterTile(tileCoordinate, tileScript);

                // Add MeshCollider for raycast (mouse click) detection
                MeshCollider col = tile.AddComponent<MeshCollider>();
                col.sharedMesh = CreateSolidHexMesh(outerSize, height);
            }
        }
        TileManager.Instance.InitializeAllTiles(); // Initialize all tile coordinates
    }


    /// <summary>
    /// Create a solid hex mesh for click detection.
    /// </summary>
    private static Mesh CreateSolidHexMesh(float outer, float h)
    {
        var vertices  = new System.Collections.Generic.List<Vector3>();
        var triangles = new System.Collections.Generic.List<int>();

        float top = h / 2f;

        for (int i = 0; i < 6; i++)
        {
            // Each sector: center(0,top,0) + two outer vertices form a triangle
            int next = (i + 1) % 6;
            vertices.Add(new Vector3(0, top, 0));
            vertices.Add(GetHexPoint(outer, top, i ));
            vertices.Add(GetHexPoint(outer, top, next));

            int baseIdx = i * 3;
            triangles.Add(baseIdx);
            triangles.Add(baseIdx + 1);
            triangles.Add(baseIdx + 2);
        }

        Mesh mesh = new Mesh();
        mesh.name       = "HexColliderMesh";
        mesh.vertices   = vertices.ToArray();
        mesh.triangles  = triangles.ToArray();
        mesh.RecalculateNormals();
        return mesh;
    }

    /// <summary>
    /// Vertex calculation for CreateSolidHexMesh.
    /// Uses the same formula as HexRenderer.GetPoint().
    /// </summary>
    private static Vector3 GetHexPoint(float size, float heightPos, int index)
    {
        float deg = 60f * index - 30f;
        float rad = Mathf.PI / 180f * deg;
        return new Vector3(size * Mathf.Cos(rad), heightPos, size * Mathf.Sin(rad));
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
        LayoutGrid();
    }
#endif

    public Vector3 GetPositionForHexFromCoordinate(Vector2Int coordinate) // Calculate hex tile placement
    {
        int column = coordinate.x;
        int row = coordinate.y;
        float width, heightPos, xPosition, yPosition, horizontalDistance, verticalDistance, offset;
        float size = outerSize;
        bool shouldOffset;

            shouldOffset = (row % 2) != 0; // Check odd row
            width = Mathf.Sqrt(3f) * size; // Width
            heightPos = 2f * size; // Height
            horizontalDistance = width; // Horizontal spacing
            verticalDistance = heightPos * 0.75f; // Vertical spacing
            offset = (shouldOffset) ? width * 0.5f : 0; // Odd row offset (shift by half width)
            xPosition = (column * horizontalDistance) + offset;
            yPosition = (row * verticalDistance);

        return new Vector3(xPosition, 0, -yPosition);
    }
}
