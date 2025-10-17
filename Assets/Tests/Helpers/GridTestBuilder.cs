using System.Collections.Generic;
using Grid;
using Solver;
using UnityEngine;

namespace Tests.Helpers
{
    /// <summary>
    ///     Fluent API builder for creating test grid configurations easily
    /// </summary>
    public class GridTestBuilder
    {
        private readonly GridVirtual _grid;
        private readonly Dictionary<int, TileVirtual> _tiles = new();

        public GridTestBuilder()
        {
            _grid = new GridVirtual();
        }

        /// <summary>
        ///     Place a tile at the specified grid position (0-8)
        /// </summary>
        public GridTestBuilder PlaceTile(int slotIndex, Tile.TileType type, int rotation = 0)
        {
            var tile = new TileVirtual(type, rotation);
            _tiles[slotIndex] = tile;
            _grid.GridSlots[slotIndex].UpdateTile(tile);
            return this;
        }

        /// <summary>
        ///     Place a tile at the specified x, y position (0-2, 0-2)
        /// </summary>
        public GridTestBuilder PlaceTile(int x, int y, Tile.TileType type, int rotation = 0)
        {
            var slotIndex = y * 3 + x;
            return PlaceTile(slotIndex, type, rotation);
        }

        /// <summary>
        ///     Place an entity at the specified path point index (0-23)
        /// </summary>
        public GridTestBuilder PlaceEntity(int entityIndex, int pathPointIndex)
        {
            _grid.PathPoints[pathPointIndex].entityIndex = entityIndex;
            _grid.PathPoints[pathPointIndex].Setup();
            return this;
        }

        /// <summary>
        ///     Recalculate all path connections
        /// </summary>
        public GridTestBuilder RecalculatePaths()
        {
            _grid.RecalculatePathConnections();
            return this;
        }

        /// <summary>
        ///     Build and return the configured grid
        /// </summary>
        public GridVirtual Build()
        {
            return _grid;
        }

        /// <summary>
        ///     Get the grid without finalizing (for intermediate operations)
        /// </summary>
        public GridVirtual GetGrid()
        {
            return _grid;
        }

        /// <summary>
        ///     Get a specific path point for verification
        /// </summary>
        public PathPoint GetPathPoint(int index)
        {
            return _grid.PathPoints[index];
        }

        /// <summary>
        ///     Print grid state for debugging
        /// </summary>
        public GridTestBuilder PrintState()
        {
            Debug.Log("=== Grid State ===");
            for (var i = 0; i < 9; i++)
            {
                var tile = _grid.GridSlots[i].Tile;
                if (tile != null) Debug.Log($"Slot {i}: {tile.TileType}, Rotation: {tile.Rotations}");
            }

            Debug.Log("=== Path Points ===");
            for (var i = 0; i < 24; i++)
            {
                var pp = _grid.PathPoints[i];
                Debug.Log($"PP {i}: PathNum={pp.pathNum}, EntityIdx={pp.entityIndex}");
            }

            return this;
        }
    }
}