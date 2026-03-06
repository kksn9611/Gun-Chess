using UnityEngine;
using UnityEngine.Tilemaps;

[ExecuteAlways] // 항상 작동
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


#if UNITY_EDITOR
    private void OnValidate()
    {
        // 인스펙터의 수치를 조절할 때마다 이 부분이 호출됩니다.
        // delayCall을 사용해 유니티 에러를 우회하여 안전하게 재생성합니다.
        UnityEditor.EditorApplication.delayCall += () =>
        {
            // 이 스크립트가 파괴된 상태가 아닐 때만 실행
            if (this != null)
            {
                LayoutGrid();
            }
        };
    }
#endif

    void Start()
    {
        if (Application.isPlaying && transform.childCount == 0)
        {
            LayoutGrid();
        }
    }

    [ContextMenu("Generate Grid (에디터에서 생성)")]
    public void LayoutGrid()
    {
        // 기존 타일 찌꺼기 깔끔하게 청소
        if (TileManager.Instance != null) 
        {
            TileManager.Instance.ClearMap();
        }
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            if (Application.isPlaying) Destroy(transform.GetChild(i).gameObject);
            else DestroyImmediate(transform.GetChild(i).gameObject);
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

                HexRenderer hexRenderer = tile.GetComponent<HexRenderer>();
                hexRenderer.isFlatTopped = isFlatTopped;
                hexRenderer.outerSize = outerSize;
                hexRenderer.innerSize = innerSize;
                hexRenderer.height = height;
                hexRenderer.SetMaterial(material);

                hexRenderer.DrawMesh();
            }
        }
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