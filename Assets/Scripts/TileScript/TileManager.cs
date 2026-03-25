using UnityEngine;
using System.Collections.Generic;
public class TileManager : MonoBehaviour
{
    // 싱글톤
    public static TileManager Instance { get; private set; }
    private Dictionary<Vector2Int, TileScript> tileMap = new Dictionary<Vector2Int, TileScript>();
 
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else Destroy(gameObject);
    }
    /// <summary>
    /// 게임 시작 시 HexGridLayout 스크립트에서 1회 실행해서 타일 맵에 등록
    /// </summary>
    /// <param name="coord"></param>
    /// <param name="tileScript"></param>
    public void RegisterTile(Vector2Int coord, TileScript tileScript)
        {
            tileMap[coord] = tileScript;
        }
    public TileScript GetTile(Vector2Int coord)
        {
        if (tileMap.TryGetValue(coord, out TileScript tileScript))
        {
            return tileScript;
        }
        
        return null;
    }
    /// <summary>
    /// 타일 큐브 좌표 계산, 이웃 타일 계산
    /// </summary>
    public void InitializeAllTiles()
    {
        foreach (TileScript tile in tileMap.Values)
        {
            tile.Initialize();
        }
        Debug.Log($"{tileMap.Count}개의 타일 연결");
    }


    public void ClearMap()
    {
        tileMap.Clear();
    }

}
