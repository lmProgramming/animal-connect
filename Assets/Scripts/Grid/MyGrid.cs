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
            Initialize();
        }

        public void Initialize()
        {
            SetupGridSlots();
            SetupPathPoints();
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
            // Initialize 24 path points
            pathPoints = new PathPoint[24];
            for (var i = 0; i < 24; i++) 
                pathPoints[i] = new PathPoint(-1, 0);

            // Assign path points to grid slots
            // Grid slot 0 (top-left)
            gridSlots[0].pathPoints[0] = pathPoints[0];
            gridSlots[0].pathPoints[1] = pathPoints[13];
            gridSlots[0].pathPoints[2] = pathPoints[3];
            gridSlots[0].pathPoints[3] = pathPoints[12];

            // Grid slot 1 (top-center)
            gridSlots[1].pathPoints[0] = pathPoints[1];
            gridSlots[1].pathPoints[1] = pathPoints[14];
            gridSlots[1].pathPoints[2] = pathPoints[4];
            gridSlots[1].pathPoints[3] = pathPoints[13];

            // Grid slot 2 (top-right)
            gridSlots[2].pathPoints[0] = pathPoints[2];
            gridSlots[2].pathPoints[1] = pathPoints[15];
            gridSlots[2].pathPoints[2] = pathPoints[5];
            gridSlots[2].pathPoints[3] = pathPoints[14];

            // Grid slot 3 (middle-left)
            gridSlots[3].pathPoints[0] = pathPoints[3];
            gridSlots[3].pathPoints[1] = pathPoints[17];
            gridSlots[3].pathPoints[2] = pathPoints[6];
            gridSlots[3].pathPoints[3] = pathPoints[16];

            // Grid slot 4 (center)
            gridSlots[4].pathPoints[0] = pathPoints[4];
            gridSlots[4].pathPoints[1] = pathPoints[18];
            gridSlots[4].pathPoints[2] = pathPoints[7];
            gridSlots[4].pathPoints[3] = pathPoints[17];

            // Grid slot 5 (middle-right)
            gridSlots[5].pathPoints[0] = pathPoints[5];
            gridSlots[5].pathPoints[1] = pathPoints[19];
            gridSlots[5].pathPoints[2] = pathPoints[8];
            gridSlots[5].pathPoints[3] = pathPoints[18];

            // Grid slot 6 (bottom-left)
            gridSlots[6].pathPoints[0] = pathPoints[6];
            gridSlots[6].pathPoints[1] = pathPoints[21];
            gridSlots[6].pathPoints[2] = pathPoints[9];
            gridSlots[6].pathPoints[3] = pathPoints[20];

            // Grid slot 7 (bottom-center)
            gridSlots[7].pathPoints[0] = pathPoints[7];
            gridSlots[7].pathPoints[1] = pathPoints[22];
            gridSlots[7].pathPoints[2] = pathPoints[10];
            gridSlots[7].pathPoints[3] = pathPoints[21];

            // Grid slot 8 (bottom-right)
            gridSlots[8].pathPoints[0] = pathPoints[8];
            gridSlots[8].pathPoints[1] = pathPoints[23];
            gridSlots[8].pathPoints[2] = pathPoints[11];
            gridSlots[8].pathPoints[3] = pathPoints[22];
        }

        public GridSlot GetGridSlot(Vector2Int gridPos)
        {
            return GridSlotsMap[gridPos.x, 2 - gridPos.y];
        }

        public Tile GetTile(Vector2Int gridPos)
        {
            return GridSlotsMap[gridPos.x, 2 - gridPos.y].GetTile();
        }

        public bool CheckIfValidPaths()
        {
            for (var i = 0; i < gridSlots.Length; i++)
                if (!gridSlots[i].GetTile())
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

        public static void PlaceTile(Tile tile, Vector2 newTilePostion, GridSlot gridSlot)
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

        private static void SwapTiles(Tile tile, GridSlot formerGridSlot, Tile tileToSwap, GridSlot tileToSwapGridSlot)
        {
            tileToSwapGridSlot.UpdateTile(tile);

            if (formerGridSlot != null) formerGridSlot.UpdateTile(tileToSwap);

            tileToSwap.transform.position = formerGridSlot.GetPosition();
        }
    }
}