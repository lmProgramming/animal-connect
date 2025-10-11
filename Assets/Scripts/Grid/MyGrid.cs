using Solver;
using UnityEngine;

namespace Grid
{
    public class MyGrid : MonoBehaviour
    {
        public GridSlot[] gridSlots = new GridSlot[9];

        public PathPoint[] pathPoints;
        public PathPoint[] entitiesPathPoints;
        public readonly GridSlot[,] GridSlotsMap = new GridSlot[3, 3];

        private int _numberOfPaths;

        private void Start()
        {
            SetupGridSlots();
        }

        private void SetupGridSlots()
        {
            var i = 0;
            for (var y = 0; y < 3; y++)
            for (var x = 0; x < 3; x++)
            {
                GridSlotsMap[x, y] = gridSlots[i];
                i += 1;
            }
        }

        private void SetupPathPoints()
        {
            var i = 0;
            for (var y = 0; y < 3; y++)
            for (var x = 0; x < 3; x++)
            {
                GridSlotsMap[x, y] = gridSlots[i];
                i += 1;
            }
        }

        public GridSlot GetGridSlot(Vector2Int gridPos)
        {
            return GridSlotsMap[gridPos.x, 2 - gridPos.y];
        }

        public Tile GetTile(Vector2Int gridPos)
        {
            return GridSlotsMap[gridPos.x, 2 - gridPos.y].GetTile();
        }

        public bool ChechIfValidPaths()
        {
            for (var i = 0; i < gridSlots.Length; i++)
                if (gridSlots[i].GetTile() == null)
                    return false;

            for (var i = 0; i < pathPoints.Length; i++)
                if (!pathPoints[i].CheckIfPathOnTopValid())
                    return false;

            return true;
        }

        public static Vector2Int GridPosition(Vector2 position)
        {
            return new Vector2Int((int)position.x + 1, (int)position.y + 1);
        }

        public void RecalculatePathConnections()
        {
            _numberOfPaths = 0;

            for (var i = 0; i < pathPoints.Length; i++)
            {
                pathPoints[i].pathNum = -1;
                pathPoints[i].ResetConectionsNumber();
            }

            for (var i = 0; i < gridSlots.Length; i++) gridSlots[i].UpdateConnections(this);
        }

        public int GetNewPathNumber()
        {
            _numberOfPaths++;
            return _numberOfPaths;
        }

        public void MergePaths(int pathNum1, int pathNum2)
        {
            foreach (var pathPoint in pathPoints)
                if (pathPoint.pathNum == pathNum2)
                    // Debug.Log("changed " + pathPoint.pathNum + " to " + pathNum1);
                    pathPoint.UpdatePathNum(pathNum1);
        }

        public void PlaceTile(Tile tile, Vector2 newTilePostion, GridSlot gridSlot)
        {
            var addToGrid = gridSlot != null;

            var formerGridSlot = tile.slot;

            if (addToGrid)
            {
                formerGridSlot.RemovedTile();

                var tileAtNewGridSlot = gridSlot.GetTile();

                if (tileAtNewGridSlot != null && tileAtNewGridSlot != tile)
                    SwapTiles(tile, formerGridSlot, tileAtNewGridSlot, gridSlot);
                else
                    gridSlot.UpdateTile(tile);
            }
            else
            {
                tile.transform.position = formerGridSlot.GetPosition();
            }

            GameManager.Instance.MoveMade();
        }

        public void SwapTiles(Tile tile, GridSlot formerGridSlot, Tile tileToSwap, GridSlot tileToSwapGridSlot)
        {
            tileToSwapGridSlot.UpdateTile(tile);

            if (formerGridSlot != null) formerGridSlot.UpdateTile(tileToSwap);

            tileToSwap.transform.position = formerGridSlot.GetPosition();
        }
    }
}