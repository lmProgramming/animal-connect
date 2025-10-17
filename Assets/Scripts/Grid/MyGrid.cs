using Solver;
using UnityEngine;

namespace Grid
{
    public class MyGrid : MonoBehaviour, IGrid
    {
        public GridSlot[] gridSlots = new GridSlot[9];

        public PathPoint[] PathPoints;
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

        public IGridSlot GetGridSlot(Vector2Int gridPos)
        {
            return GridSlotsMap[gridPos.x, 2 - gridPos.y];
        }

        public ITile GetTile(Vector2Int gridPos)
        {
            return GridSlotsMap[gridPos.x, 2 - gridPos.y].GetTile();
        }

        public bool CheckIfValidPaths()
        {
            for (var i = 0; i < gridSlots.Length; i++)
                if (!(gridSlots[i].GetTile() as Tile))
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

            for (var i = 0; i < gridSlots.Length; i++) gridSlots[i].UpdateConnections(this);
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
                    // Debug.Log("changed " + pathPoint.pathNum + " to " + pathNum1);
                    pathPoint.UpdatePathNum(pathNum1);
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
            PathPoints = new PathPoint[24];
            for (var i = 0; i < 24; i++)
                PathPoints[i] = new PathPoint(-1, 0);

            // Assign path points to grid slots
            // Grid slot 0 (top-left)
            gridSlots[0].PathPoints[0] = PathPoints[0];
            gridSlots[0].PathPoints[1] = PathPoints[13];
            gridSlots[0].PathPoints[2] = PathPoints[3];
            gridSlots[0].PathPoints[3] = PathPoints[12];

            // Grid slot 1 (top-center)
            gridSlots[1].PathPoints[0] = PathPoints[1];
            gridSlots[1].PathPoints[1] = PathPoints[14];
            gridSlots[1].PathPoints[2] = PathPoints[4];
            gridSlots[1].PathPoints[3] = PathPoints[13];

            // Grid slot 2 (top-right)
            gridSlots[2].PathPoints[0] = PathPoints[2];
            gridSlots[2].PathPoints[1] = PathPoints[15];
            gridSlots[2].PathPoints[2] = PathPoints[5];
            gridSlots[2].PathPoints[3] = PathPoints[14];

            // Grid slot 3 (middle-left)
            gridSlots[3].PathPoints[0] = PathPoints[3];
            gridSlots[3].PathPoints[1] = PathPoints[17];
            gridSlots[3].PathPoints[2] = PathPoints[6];
            gridSlots[3].PathPoints[3] = PathPoints[16];

            // Grid slot 4 (center)
            gridSlots[4].PathPoints[0] = PathPoints[4];
            gridSlots[4].PathPoints[1] = PathPoints[18];
            gridSlots[4].PathPoints[2] = PathPoints[7];
            gridSlots[4].PathPoints[3] = PathPoints[17];

            // Grid slot 5 (middle-right)
            gridSlots[5].PathPoints[0] = PathPoints[5];
            gridSlots[5].PathPoints[1] = PathPoints[19];
            gridSlots[5].PathPoints[2] = PathPoints[8];
            gridSlots[5].PathPoints[3] = PathPoints[18];

            // Grid slot 6 (bottom-left)
            gridSlots[6].PathPoints[0] = PathPoints[6];
            gridSlots[6].PathPoints[1] = PathPoints[21];
            gridSlots[6].PathPoints[2] = PathPoints[9];
            gridSlots[6].PathPoints[3] = PathPoints[20];

            // Grid slot 7 (bottom-center)
            gridSlots[7].PathPoints[0] = PathPoints[7];
            gridSlots[7].PathPoints[1] = PathPoints[22];
            gridSlots[7].PathPoints[2] = PathPoints[10];
            gridSlots[7].PathPoints[3] = PathPoints[21];

            // Grid slot 8 (bottom-right)
            gridSlots[8].PathPoints[0] = PathPoints[8];
            gridSlots[8].PathPoints[1] = PathPoints[23];
            gridSlots[8].PathPoints[2] = PathPoints[11];
            gridSlots[8].PathPoints[3] = PathPoints[22];
        }

        public static Vector2Int GridPosition(Vector2 position)
        {
            return new Vector2Int((int)position.x + 1, (int)position.y + 1);
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
                    SwapTiles(tile, formerGridSlot, tileAtNewGridSlot as Tile, gridSlot);
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

            if (formerGridSlot) formerGridSlot.UpdateTile(tileToSwap);

            tileToSwap.transform.position = formerGridSlot.GetPosition();
        }
    }
}