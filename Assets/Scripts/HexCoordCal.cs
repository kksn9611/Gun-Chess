using UnityEngine;
using System.Collections.Generic;
// https://www.redblobgames.com/grids/hexagons/#coordinates
public class HexCoordCal
{
    public static int GetDistance(Vector2Int a, Vector2Int b)
    {
        Vector3Int cubeA = OffsetToCube(a);
        Vector3Int cubeB = OffsetToCube(b);
        // 세 개 축의 절대값 중 가장 큰 값 = 거리값
        int distance = 
        Mathf.Max(
            Mathf.Abs(cubeA.x - cubeB.x),
            Mathf.Abs(cubeA.y - cubeB.y),
            Mathf.Abs(cubeA.z - cubeB.z));

            return distance;
    }

    public static Vector3Int OffsetToCube(Vector2Int offsetCoord)
    {
        // HexGrid의 구조 odd-r 홀수 행 +1/2 밀어내기
        // 짝수 홀수 판별 & 1
        int q = offsetCoord.x - (offsetCoord.y - (offsetCoord.y & 1)) / 2;
        int r = offsetCoord.y;
        int s = -q -r;
        return new Vector3Int(q, r, s);
    }
    
    // 육각형 근접 타일 반환 함수
    public static List<TileScript> GetTileNeighbors(TileScript currentTile)
    {
        List<TileScript> neighbors = new List<TileScript>();
        Vector2Int pos = currentTile.GridCoordinate;

        Vector2Int[] directions = (pos.y % 2 != 0) ? OddRowDirections : EvenRowDirections;

        foreach (var direction in directions)
        {
            Vector2Int neighborPos = new Vector2Int(pos.x + direction.x, pos.y + direction.y);

            TileScript neighborTile = TileManager.Instance.GetTile(neighborPos);

            if (neighborTile != null)
            {
                neighbors.Add(neighborTile);
            }
        }
        return neighbors;
    }

    private static readonly Vector2Int[] EvenRowDirections = new Vector2Int[]
    {
        new Vector2Int(0, -1), new Vector2Int(1, 0), new Vector2Int(0, 1),
        new Vector2Int(-1, 1), new Vector2Int(-1, 0), new Vector2Int(-1, -1)
    };
    private static readonly Vector2Int[] OddRowDirections = new Vector2Int[]
        {
        new Vector2Int(1, -1), new Vector2Int(1, 0), new Vector2Int(1, 1),
        new Vector2Int(0, 1), new Vector2Int(-1, 0), new Vector2Int(0, -1)
        };
}


