using System.Collections.Generic;
using Priority_Queue;

public static class Pathfinder
{   
    public static List<TileScript> FindPath(TileScript startTile, TileScript endTile)
    {
        if (startTile == null || endTile == null) return new List<TileScript>();
        if (startTile == endTile) return new List<TileScript>();

            // 우선순위 탐색 큐
     SimplePriorityQueue<TileScript, int> visitQueueList = new SimplePriorityQueue<TileScript, int>();
    // 이동 경로 저장
     Dictionary<TileScript, TileScript> parentMap = new Dictionary<TileScript, TileScript>();
    // 이동 경로 비용
     Dictionary<TileScript, int> costOfRoute = new Dictionary<TileScript, int>();
    // 우선순위 탐색 큐에 시작 타일
    visitQueueList.Enqueue(startTile,0);
    // costOfRoute 이동 경로 비용 딕셔너리 시작 타일 
    costOfRoute[startTile] = 0;

        while (visitQueueList.Count > 0)
        {
            TileScript currentTile = visitQueueList.Dequeue();
            if(currentTile == endTile)
            {
                // 길찾기를 완료했을경우 경로를 되돌아가서 루트 반환
               return RetraceRoute(parentMap, startTile, endTile); 
            }

            foreach(TileScript neighbor in currentTile.Neighbors)
            {
                // 타일에 유닛이 배치되어있다면 그 타일은 무시하기
                if(neighbor.IsOccupied && neighbor != endTile) continue;
                // 임시 루트 비용 계산 (현재까지 누적된 루트 비용) G Score
                int tentativeGscore = costOfRoute[currentTile] + neighbor.MovementCost;
                // 가지 않았던 길 or 더 빠른 길
                if(!costOfRoute.ContainsKey(neighbor) || tentativeGscore < costOfRoute[neighbor])
                {
                    costOfRoute[neighbor] = tentativeGscore;
                    // 이웃 타일에서 목적지까지 남은 거리 계산 (H Score)
                    int heuristicScore = HexCoordCal.GetCubeDistance(neighbor.CubeCoordinate, endTile.CubeCoordinate);
                    // 총 예상 비용 (임시 G Score + H Score)
                    int fScore = tentativeGscore + heuristicScore;
                    // fScore가 낮은(비용이 낮은) 타일부터 탐색하기 위해 우선순위 큐로 넘겨줌
                    visitQueueList.Enqueue(neighbor,fScore);
                    
                    parentMap[neighbor] = currentTile;
                }
            }
        }
        // 길 없으면 빈 타일 리스트 반환
        return new List<TileScript>();
    }

    private static List<TileScript> RetraceRoute(Dictionary<TileScript, TileScript> parentMap,TileScript startTile, TileScript endTile)
    {
        List<TileScript> route = new List<TileScript>();
        TileScript current = endTile;

        while (current != startTile)
        {
            route.Add(current);
            current = parentMap[current];
        }
        route.Reverse();
        return route;   
    }
}
