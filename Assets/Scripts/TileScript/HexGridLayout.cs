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
        if (TileManager.Instance != null)
        {
            TileManager.Instance.ClearMap();
        }
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
                
                if (TileManager.Instance != null)
                TileManager.Instance.RegisterTile(tileCoordinate, tileScript);
                else
                {
                    Debug.Log("등록실패");
                }

                    // 타일 그리기
                HexRenderer hexRenderer = tile.GetComponent<HexRenderer>();
                hexRenderer.isFlatTopped = isFlatTopped;
                hexRenderer.outerSize = outerSize;
                hexRenderer.innerSize = innerSize;
                hexRenderer.height = height;
                hexRenderer.SetMaterial(material);
                hexRenderer.DrawMesh();
            }
        }
        TileManager.Instance.InitializeAllTiles(); // 모든 타일 좌표 로딩
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