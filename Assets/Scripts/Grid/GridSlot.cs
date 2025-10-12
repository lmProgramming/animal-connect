using System.Collections.Generic;
using Solver;
using UnityEngine;

namespace Grid
{
    public class GridSlot : MonoBehaviour
    {
        [SerializeField] private Tile tile;
        public PathPoint[] pathPoints = new PathPoint[4];

        [SerializeField] private Vector2Int gridPosition;
        [SerializeField] private Vector2 position;

        private void Start()
        {
            gridPosition = new Vector2Int((int)transform.localPosition.x, (int)transform.localPosition.y);
            position = transform.position;
        }

        public Tile GetTile()
        {
            return tile;
        }

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

        public Vector2Int GetGridPosition()
        {
            return gridPosition;
        }

        public Vector2 GetPosition()
        {
            return position;
        }

        public void UpdateConnections(MyGrid grid)
        {
            if (tile != null)
            {
                var connectionsCopy = new List<List<int>>();
                for (var i = 0; i < tile.GetGridBlock().Connections.Count; i++)
                {
                    connectionsCopy.Add(new List<int>());
                    for (var j = 0; j < tile.GetGridBlock().Connections[i].Count; j++)
                        connectionsCopy[i].Add((tile.GetGridBlock().Connections[i][j] + tile.rotations) % 4);
                }

                foreach (var connection in connectionsCopy)
                {
                    var pathNum = pathPoints[connection[0]].pathNum;

                    // Debug.Log(connection[0]);

                    if (pathNum == -1)
                    {
                        pathNum = grid.GetNewPathNumber();
                        pathPoints[connection[0]].UpdatePathNum(pathNum);
                    }

                    pathPoints[connection[0]].RaiseConnectionsNumber();

                    for (var i = 1; i < connection.Count; i++)
                    {
                        // Debug.Log(connection[i]);

                        pathPoints[connection[i]].RaiseConnectionsNumber();

                        if (pathPoints[connection[i]].pathNum >= 0 && pathPoints[connection[i]].pathNum != pathNum)
                            grid.MergePaths(pathNum, pathPoints[connection[i]].pathNum);
                        else
                            pathPoints[connection[i]].UpdatePathNum(pathNum);
                    }
                }
            }
        }
    }
}