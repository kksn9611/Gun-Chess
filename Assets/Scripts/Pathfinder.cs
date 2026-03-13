using UnityEngine;
using System.Collections.Generic;
using Priority_Queue;

public class Pathfinder : MonoBehaviour
{
    public void FindPath(Vector2Int start, Vector2Int end)
    {
        Vector3Int startCube = HexCoordCal.OffsetToCube(start);
        Vector3Int endCube = HexCoordCal.OffsetToCube(end);

        //우선 순위 큐를 사용해 탐색 속도 최적화
        SimplePriorityQueue<Vector3Int, int> queueSet = new SimplePriorityQueue<Vector3Int, int>();
        queueSet.Enqueue(startCube, 0);
        
        Dictionary<Vector3Int, int> costSoFar = new Dictionary<Vector3Int, int>();

    }
}
