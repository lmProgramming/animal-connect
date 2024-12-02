using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridSlot : MonoBehaviour
{
    [SerializeField]
    Tile tile;
    public PathPoint[] pathPoints = new PathPoint[4];

    public Tile GetTile() { return tile; }

    public void UpdateTile(Tile newTile)
    {
        newTile.slot = this;
        tile = newTile;
    }

    public void RemovedTile()
    {
        tile.slot = null;
        tile = null;
    }

    [SerializeField]
    Vector2Int gridPosition;
    [SerializeField]
    Vector2 position;

    private void Start()
    {
        gridPosition = new Vector2Int((int)transform.position.x, (int)transform.position.y);
        position = transform.position;
    }

    public Vector2Int GetGridPosition()
    {
        return gridPosition;
    }

    public Vector2 GetPosition()
    {
        return position;
    }

    public void UpdateConnections(Grid grid)
    {
        if (tile != null)
        {
            List<List<int>> connectionsCopy = new List<List<int>>();
            for (int i = 0; i < tile.GetGridBlock().connections.Count; i++)
            {
                connectionsCopy.Add(new List<int>());
                for (int j = 0; j < tile.GetGridBlock().connections[i].Count; j++)
                {
                    connectionsCopy[i].Add((tile.GetGridBlock().connections[i][j] + tile.rotations) % 4);
                }
            }

            foreach (var connection in connectionsCopy)
            {
                int pathNum = pathPoints[connection[0]].pathNum;

                // Debug.Log(connection[0]);

                if (pathNum == -1)
                {
                    pathNum = grid.GetNewPathNumber();
                    pathPoints[connection[0]].UpdatePathNum(pathNum);
                }

                pathPoints[connection[0]].RaiseConnectionsNumber();

                for (int i = 1; i < connection.Count; i++)
                {
                    // Debug.Log(connection[i]);

                    pathPoints[connection[i]].RaiseConnectionsNumber();

                    if (pathPoints[connection[i]].pathNum >= 0 && pathPoints[connection[i]].pathNum != pathNum)
                    {
                        grid.MergePaths(pathNum, pathPoints[connection[i]].pathNum);
                    }
                    else
                    {
                        pathPoints[connection[i]].UpdatePathNum(pathNum);
                    }
                }
            }
        }
    }
}
