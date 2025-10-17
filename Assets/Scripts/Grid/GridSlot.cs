using System.Collections.Generic;
using Solver;
using UnityEngine;

namespace Grid
{
    public class GridSlot : MonoBehaviour, IGridSlot
    {
        [SerializeField] private Tile tile;

        [SerializeField] private Vector2Int gridPosition;
        [SerializeField] private Vector2 position;

        private void Start()
        {
            gridPosition = new Vector2Int((int)transform.localPosition.x, (int)transform.localPosition.y);
            position = transform.position;
        }

        public IPathPoint[] PathPoints { get; set; }

        public ITile GetTile()
        {
            return tile;
        }

        public void UpdateTile(ITile newTile)
        {
            if (newTile is Tile concreteTile)
            {
                concreteTile.slot = this;
                tile = concreteTile;
            }
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

        // Legacy method for backward compatibility
        public Tile GetTileConcrete()
        {
            return tile;
        }

        // Legacy method for backward compatibility
        public void UpdateTileConcrete(Tile newTile)
        {
            newTile.slot = this;
            tile = newTile;
        }

        public void UpdateConnections(MyGrid myGrid)
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
                    var pathNum = PathPoints[connection[0]].PathNum;

                    // Debug.Log(connection[0]);

                    if (pathNum == -1)
                    {
                        pathNum = myGrid.GetNewPathNumber();
                        PathPoints[connection[0]].UpdatePathNum(pathNum);
                    }

                    PathPoints[connection[0]].RaiseConnectionsNumber();

                    for (var i = 1; i < connection.Count; i++)
                    {
                        // Debug.Log(connection[i]);

                        PathPoints[connection[i]].RaiseConnectionsNumber();

                        if (PathPoints[connection[i]].PathNum >= 0 && PathPoints[connection[i]].PathNum != pathNum)
                            myGrid.MergePaths(pathNum, PathPoints[connection[i]].PathNum);
                        else
                            PathPoints[connection[i]].UpdatePathNum(pathNum);
                    }
                }
            }
        }
    }
}