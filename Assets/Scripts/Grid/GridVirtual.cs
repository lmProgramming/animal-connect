using System.Collections.Generic;
using Solver;
using UnityEngine;

namespace Grid
{
    /// <summary>
    ///     Virtual grid implementation for testing without MonoBehaviour dependencies
    /// </summary>
    public class GridVirtual : IGrid
    {
        public readonly GridSlotVirtual[,] GridSlotsMap = new GridSlotVirtual[3, 3];

        private int _numberOfPaths;
        public GridSlotVirtual[] GridSlots = new GridSlotVirtual[9];
        public PathPoint[] PathPoints;

        public GridVirtual()
        {
            Initialize();
        }

        public void Initialize()
        {
            // Initialize grid slots
            for (var i = 0; i < 9; i++)
            {
                var x = i % 3;
                var y = i / 3;
                GridSlots[i] = new GridSlotVirtual(x, y);
            }

            SetupGridSlots();
            SetupPathPoints();
        }

        public bool CheckIfValidPaths()
        {
            for (var i = 0; i < GridSlots.Length; i++)
                if (GridSlots[i].GetTile() == null)
                    return false;

            for (var i = 0; i < PathPoints.Length; i++)
                if (!PathPoints[i].CheckIfPathOnTopValid())
                    return false;

            return true;
        }

        public void RecalculatePathConnections()
        {
            _numberOfPaths = 0;

            for (var i = 0; i < PathPoints.Length; i++)
            {
                PathPoints[i].pathNum = -1;
                PathPoints[i].ResetConectionsNumber();
            }

            for (var i = 0; i < GridSlots.Length; i++)
                UpdateConnectionsForSlot(GridSlots[i]);
        }

        public int GetNewPathNumber()
        {
            _numberOfPaths++;
            return _numberOfPaths;
        }

        public void MergePaths(int pathNum1, int pathNum2)
        {
            foreach (var pathPoint in PathPoints)
                if (pathPoint.pathNum == pathNum2)
                    pathPoint.UpdatePathNum(pathNum1);
        }

        public IGridSlot GetGridSlot(Vector2Int gridPos)
        {
            return GridSlotsMap[gridPos.x, 2 - gridPos.y];
        }

        public ITile GetTile(Vector2Int gridPos)
        {
            return GridSlotsMap[gridPos.x, 2 - gridPos.y].Tile;
        }

        private void SetupGridSlots()
        {
            var i = 0;
            for (var y = 0; y < 3; y++)
            for (var x = 0; x < 3; x++)
            {
                GridSlotsMap[x, y] = GridSlots[i];
                i += 1;
            }
        }

        private void SetupPathPoints()
        {
            // Initialize 24 path points
            PathPoints = new PathPoint[24];
            for (var i = 0; i < 24; i++)
                PathPoints[i] = new PathPoint(-1, 0);

            // Assign path points to grid slots (same layout as MyGrid)
            GridSlots[0].PathPoints[0] = PathPoints[0];
            GridSlots[0].PathPoints[1] = PathPoints[13];
            GridSlots[0].PathPoints[2] = PathPoints[3];
            GridSlots[0].PathPoints[3] = PathPoints[12];

            GridSlots[1].PathPoints[0] = PathPoints[1];
            GridSlots[1].PathPoints[1] = PathPoints[14];
            GridSlots[1].PathPoints[2] = PathPoints[4];
            GridSlots[1].PathPoints[3] = PathPoints[13];

            GridSlots[2].PathPoints[0] = PathPoints[2];
            GridSlots[2].PathPoints[1] = PathPoints[15];
            GridSlots[2].PathPoints[2] = PathPoints[5];
            GridSlots[2].PathPoints[3] = PathPoints[14];

            GridSlots[3].PathPoints[0] = PathPoints[3];
            GridSlots[3].PathPoints[1] = PathPoints[17];
            GridSlots[3].PathPoints[2] = PathPoints[6];
            GridSlots[3].PathPoints[3] = PathPoints[16];

            GridSlots[4].PathPoints[0] = PathPoints[4];
            GridSlots[4].PathPoints[1] = PathPoints[18];
            GridSlots[4].PathPoints[2] = PathPoints[7];
            GridSlots[4].PathPoints[3] = PathPoints[17];

            GridSlots[5].PathPoints[0] = PathPoints[5];
            GridSlots[5].PathPoints[1] = PathPoints[19];
            GridSlots[5].PathPoints[2] = PathPoints[8];
            GridSlots[5].PathPoints[3] = PathPoints[18];

            GridSlots[6].PathPoints[0] = PathPoints[6];
            GridSlots[6].PathPoints[1] = PathPoints[21];
            GridSlots[6].PathPoints[2] = PathPoints[9];
            GridSlots[6].PathPoints[3] = PathPoints[20];

            GridSlots[7].PathPoints[0] = PathPoints[7];
            GridSlots[7].PathPoints[1] = PathPoints[22];
            GridSlots[7].PathPoints[2] = PathPoints[10];
            GridSlots[7].PathPoints[3] = PathPoints[21];

            GridSlots[8].PathPoints[0] = PathPoints[8];
            GridSlots[8].PathPoints[1] = PathPoints[23];
            GridSlots[8].PathPoints[2] = PathPoints[11];
            GridSlots[8].PathPoints[3] = PathPoints[22];
        }

        private void UpdateConnectionsForSlot(GridSlotVirtual gridSlot)
        {
            var tile = gridSlot.Tile;
            if (tile != null)
            {
                var connectionsCopy = new List<List<int>>();
                for (var i = 0; i < tile.GetGridBlock().Connections.Count; i++)
                {
                    connectionsCopy.Add(new List<int>());
                    for (var j = 0; j < tile.GetGridBlock().Connections[i].Count; j++)
                        connectionsCopy[i].Add((tile.GetGridBlock().Connections[i][j] + tile.Rotations) % 4);
                }

                foreach (var connection in connectionsCopy)
                {
                    var pathNum = gridSlot.PathPoints[connection[0]].PathNum;

                    if (pathNum == -1)
                    {
                        pathNum = GetNewPathNumber();
                        gridSlot.PathPoints[connection[0]].UpdatePathNum(pathNum);
                    }

                    gridSlot.PathPoints[connection[0]].RaiseConnectionsNumber();

                    for (var i = 1; i < connection.Count; i++)
                    {
                        gridSlot.PathPoints[connection[i]].RaiseConnectionsNumber();

                        if (gridSlot.PathPoints[connection[i]].PathNum >= 0 &&
                            gridSlot.PathPoints[connection[i]].PathNum != pathNum)
                            MergePaths(pathNum, gridSlot.PathPoints[connection[i]].PathNum);
                        else
                            gridSlot.PathPoints[connection[i]].UpdatePathNum(pathNum);
                    }
                }
            }
        }
    }
}