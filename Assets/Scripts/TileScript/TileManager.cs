using System.Collections.Generic;
using UnityEngine;


public class TileManager
{
    private static TileManager instance;
    public static TileManager Instance => instance ??= new TileManager();

    private TileManager() { }

    private readonly Dictionary<Vector2Int, TileScript> tileMap = new Dictionary<Vector2Int, TileScript>();

    /// <summary>
    /// HexGridLayout.LayoutGrid()에서 타일 생성 시 1회 호출한다.
    /// </summary>
    public void RegisterTile(Vector2Int coord, TileScript tileScript)
    {
        tileMap[coord] = tileScript;
    }

    public TileScript GetTile(Vector2Int coord)
    {
        tileMap.TryGetValue(coord, out TileScript tileScript);
        return tileScript;
    }

    /// <summary>
    /// 모든 타일의 큐브 좌표와 이웃 타일을 계산한다.
    /// LayoutGrid() 완료 후 1회 호출한다.
    /// </summary>
    public void InitializeAllTiles()
    {
        foreach (TileScript tile in tileMap.Values)
            tile.Initialize();

        Debug.Log($"{tileMap.Count}개의 타일 연결");
    }

    public void ClearMap()
    {
        tileMap.Clear();
    }
}
