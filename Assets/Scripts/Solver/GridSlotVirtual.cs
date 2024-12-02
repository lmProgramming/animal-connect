using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridSlotVirtual
{
    public TileVirtual tile;
    public PathPoint[] pathPoints = new PathPoint[4];

    public void UpdateTile(TileVirtual newTile)
    {
        tile = newTile;

        // UpdateConnections();
    }

    public GridSlotVirtual()
    {

    }

    //public void UpdateConnections()
    //{
    //    if (tile != null)
    //    {
    //        GridBlock gridBlock = tile.gridBlock;

    //        List<List<int>> connectionsCopy = new List<List<int>>();
    //        for (int i = 0; i < tile.gridBlock.connections.Count; i++)
    //        {
    //            connectionsCopy.Add(new List<int>());
    //            for (int j = 0; j < tile.gridBlock.connections[i].Count; j++)
    //            {
    //                connectionsCopy[i].Add((tile.gridBlock.connections[i][j] + tile.rotations) % 4);
    //            }
    //        }

    //        foreach (var connection in connectionsCopy)
    //        {
    //            int pathNum = pathPoints[connection[0]].pathNum;

    //            // Debug.Log(connection[0]);

    //            if (pathNum == -1)
    //            {
    //                pathNum = GameManager.Instance.GetNewPathNumber();
    //                pathPoints[connection[0]].UpdatePathNum(pathNum);
    //            }

    //            pathPoints[connection[0]].RaiseConnectionsNumber();

    //            for (int i = 1; i < connection.Count; i++)
    //            {
    //                // Debug.Log(connection[i]);

    //                pathPoints[connection[i]].RaiseConnectionsNumber();

    //                if (pathPoints[connection[i]].pathNum >= 0 && pathPoints[connection[i]].pathNum != pathNum)
    //                {
    //                    GameManager.Instance.MergePaths(pathNum, pathPoints[connection[i]].pathNum);
    //                }
    //                else
    //                {
    //                    pathPoints[connection[i]].UpdatePathNum(pathNum);
    //                }
    //            }
    //        }
    //    }
    //}
}
