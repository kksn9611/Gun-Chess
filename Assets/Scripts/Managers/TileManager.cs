using System.Collections.Generic;
using UnityEngine;


public class TileManager
{
    private static TileManager instance;
    public static TileManager Instance => instance ??= new TileManager();

    private TileManager() { }

    private readonly Dictionary<Vector2Int, TileScript> tileMap = new Dictionary<Vector2Int, TileScript>();

    /// <summary>All registered field tiles (used for overlay/placement sweeps).</summary>
    public IEnumerable<TileScript> AllTiles => tileMap.Values;

    /// <summary>
    /// Called once per tile during HexGridLayout.LayoutGrid().
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
    /// Compute cube coordinates and neighbor lists for all tiles.
    /// Called once after LayoutGrid() completes.
    /// </summary>
    public void InitializeAllTiles()
    {
        foreach (TileScript tile in tileMap.Values)
            tile.Initialize();

        Debug.Log($"{tileMap.Count} tiles connected");
    }

    /// <summary>
    /// Reset IsOccupied to false for all tiles. Called on round transition.
    /// </summary>
    public void ClearAllOccupied()
    {
        foreach (TileScript tile in tileMap.Values)
            tile.IsOccupied = false;
    }

    public void ClearMap()
    {
        tileMap.Clear();
    }
}
