using UnityEngine;

public class HexGridLayout : MonoBehaviour
{
    [Header("Grid Settings")]
    public Vector2Int gridSize;

    [Header("Tile Settings")]
    public float outerSize = 1f;
    public float innerSize = 0f;
    public float height = 1f;
    public bool isFlatTopped;
    public Material material;

    private void Start()
    {
        LayoutGrid();
    }

    public void LayoutGrid()
    {
        // 기존 타일 찌꺼기 깔끔하게 청소
        TileManager.Instance.ClearMap();
        
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Destroy(transform.GetChild(i).gameObject);
        }

        // 새 타일 생성 및 배치
        for (int y = 0; y < gridSize.y; y++)
        {
            for (int x = 0; x < gridSize.x; x++)
            {

                //게임 오브젝트 생성 후 HexRenderer 붙이기 타일 관리용 TileScript 붙이기
                GameObject tile = new GameObject($"Tile_{x}_{y}", typeof(HexRenderer),typeof(TileScript));
                Vector2Int tileCoordinate = new Vector2Int(x,y); 
                tile.transform.SetParent(transform, false);
                tile.transform.localPosition = GetPositionForHexFromCoordinate(tileCoordinate);
                TileScript tileScript = tile.GetComponent<TileScript>();
                tileScript.GridCoordinate = tileCoordinate;
                
                TileManager.Instance.RegisterTile(tileCoordinate, tileScript);

                    // 타일 그리기
                HexRenderer hexRenderer = tile.GetComponent<HexRenderer>();
                hexRenderer.isFlatTopped = isFlatTopped;
                hexRenderer.outerSize = outerSize;
                hexRenderer.innerSize = innerSize;
                hexRenderer.height = height;
                hexRenderer.SetMaterial(material);
                hexRenderer.DrawMesh();

                // 레이캐스트(마우스 클릭) 대상이 되도록 MeshCollider 추가
                // innerSize > 0 이면 시각적 메시가 도넛 형태라 내부가 빈다.
                // 클릭 판정용으로 innerSize=0 인 솔리드 메시를 별도 생성해 콜라이더에 할당한다.
                MeshCollider col = tile.AddComponent<MeshCollider>();
                col.sharedMesh = CreateSolidHexMesh(outerSize, height, isFlatTopped);
            }
        }
        TileManager.Instance.InitializeAllTiles(); // 모든 타일 좌표 로딩
    }


    /// <summary>
    /// 클릭 판정 전용 솔리드 육각형 메시를 생성한다.
    /// innerSize=0 으로 고정해 중심까지 꽉 찬 뚜껑 면 6개로만 구성한다.
    /// </summary>
    private static Mesh CreateSolidHexMesh(float outer, float h, bool flatTopped)
    {
        var vertices  = new System.Collections.Generic.List<Vector3>();
        var triangles = new System.Collections.Generic.List<int>();

        float top = h / 2f;

        for (int i = 0; i < 6; i++)
        {
            // 각 섹터: 중심(0,top,0) + 외곽 두 꼭짓점으로 삼각형 생성
            int next = (i + 1) % 6;
            vertices.Add(new Vector3(0, top, 0));
            vertices.Add(GetHexPoint(outer, top, i,    flatTopped));
            vertices.Add(GetHexPoint(outer, top, next, flatTopped));

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
    /// CreateSolidHexMesh 전용 꼭짓점 계산.
    /// HexRenderer.GetPoint() 와 동일한 공식을 사용한다.
    /// </summary>
    private static Vector3 GetHexPoint(float size, float heightPos, int index, bool flatTopped)
    {
        float deg = flatTopped ? 60f * index : 60f * index - 30f;
        float rad = Mathf.PI / 180f * deg;
        return new Vector3(size * Mathf.Cos(rad), heightPos, size * Mathf.Sin(rad));
    }

    public Vector3 GetPositionForHexFromCoordinate(Vector2Int coordinate) // 육각형 타일 배치 구조 계산
    {
        int column = coordinate.x;
        int row = coordinate.y;
        float width, heightPos, xPosition, yPosition, horizontalDistance, verticalDistance, offset;
        float size = outerSize;
        bool shouldOffset;

        if (!isFlatTopped) // 위쪽이 뾰족한 육각형
        {
            shouldOffset = (row % 2) != 0; // 홀수번째 확인
            width = Mathf.Sqrt(3f) * size; // 너비
            heightPos = 2f * size; // 높이
            horizontalDistance = width; // 가로 이동거리
            verticalDistance = heightPos * 0.75f; // 세로 이동거리
            offset = (shouldOffset) ? width * 0.5f : 0; // 홀수 줄 처리 (너비의 0.5만큼 이동)
            xPosition = (column * horizontalDistance) + offset; 
            yPosition = (row * verticalDistance);
        }
        else // 위쪽 평평한 육각형
        {
            shouldOffset = (column % 2) != 0;
            width = 2f * size;
            heightPos = Mathf.Sqrt(3f) * size;
            horizontalDistance = width * 0.75f;
            verticalDistance = heightPos;
            offset = (shouldOffset) ? heightPos * 0.5f : 0;
            xPosition = (column * horizontalDistance);
            yPosition = (row * verticalDistance) - offset;
        }
        return new Vector3(xPosition, 0, -yPosition);
    }
}