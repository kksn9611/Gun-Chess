using System.Collections.Generic;
using Priority_Queue;

public static class Pathfinder
{
    public static List<TileScript> FindPath(TileScript startTile, TileScript endTile)
    {
        if (startTile == null || endTile == null) return new List<TileScript>();
        if (startTile == endTile) return new List<TileScript>();

    // Priority search queue
    SimplePriorityQueue<TileScript, int> visitQueueList = new SimplePriorityQueue<TileScript, int>();
    // Path trace map
     Dictionary<TileScript, TileScript> parentMap = new Dictionary<TileScript, TileScript>();
    // Route cost map
     Dictionary<TileScript, int> costOfRoute = new Dictionary<TileScript, int>();
    // Enqueue start tile
    visitQueueList.Enqueue(startTile,0);
    // Initialize start tile cost
    costOfRoute[startTile] = 0;

        while (visitQueueList.Count > 0)
        {
            TileScript currentTile = visitQueueList.Dequeue();
            if(currentTile == endTile)
            {
                // Pathfinding complete — retrace and return route
               return RetraceRoute(parentMap, startTile, endTile);
            }

            foreach(TileScript neighbor in currentTile.Neighbors)
            {
                // Skip occupied tiles (unless it's the destination)
                if(neighbor.IsOccupied && neighbor != endTile) continue;
                // Tentative G score = accumulated route cost so far
                int tentativeGscore = costOfRoute[currentTile] + neighbor.MovementCost;
                // Unvisited tile or cheaper path found
                if(!costOfRoute.ContainsKey(neighbor) || tentativeGscore < costOfRoute[neighbor])
                {
                    costOfRoute[neighbor] = tentativeGscore;
                    // H score = remaining distance from neighbor to destination
                    int heuristicScore = HexCoordCal.GetCubeDistance(neighbor.CubeCoordinate, endTile.CubeCoordinate);
                    // F score = tentative G + H
                    int fScore = tentativeGscore + heuristicScore;
                    // Update priority if already in queue; lower F = explored first
                    if (visitQueueList.Contains(neighbor))
                    {
                        visitQueueList.UpdatePriority(neighbor, fScore);
                    }
                    // First visit — enqueue
                    else
                    {
                        visitQueueList.Enqueue(neighbor, fScore);
                    }

                    parentMap[neighbor] = currentTile;
                }
            }
        }
        // No path found — return empty list
        return new List<TileScript>();
    }

    private static List<TileScript> RetraceRoute(Dictionary<TileScript, TileScript> parentMap,TileScript startTile, TileScript endTile)
    {
        List<TileScript> route = new List<TileScript>();
        TileScript current = endTile;

        while (current != startTile)
        {
            route.Add(current);
            if (!parentMap.TryGetValue(current, out TileScript parent))
            {
                return new List<TileScript>();
            }
            current = parent;
        }
        route.Reverse();
        return route;
    }
}
