using UnityEngine;
using System.Collections.Generic;
[ExecuteAlways]
public class TileManager : MonoBehaviour
{
    public static TileManager Instance { get; private set; }
    private Dictionary<Vector2Int, TileScript> tileMap = new Dictionary<Vector2Int, TileScript>();

    private void Awake()
    {
        // 싱글톤 
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }
   
    public void RegisterTile(Vector2Int coord, TileScript tileScript)
        {
            tileMap[coord] = tileScript;        
        }
    public TileScript GetTile(Vector2Int coord)
        {
            if (tileMap.TryGetValue(coord, out TileScript tile)) return tile;
            return null;
        }

    public void ClearMap()
    {
        tileMap.Clear();
    }

}
