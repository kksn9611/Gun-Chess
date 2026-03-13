using System;
using UnityEngine;
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
}

