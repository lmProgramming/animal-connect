using System;
using System.Collections.Generic;
using System.Linq;

namespace Core.Models
{
    /// <summary>
    ///     Immutable representation of the 3x3 grid state.
    ///     Each slot can contain a tile or be empty.
    ///     No Unity dependencies - pure data structure.
    /// </summary>
    [Serializable]
    public class GridState
    {
        public const int GridSize = 3;
        public const int TotalSlots = 9;
        private readonly TileData?[] _tiles; // 9 slots, nullable for empty slots

        public GridState()
        {
            _tiles = new TileData?[TotalSlots];
        }

        private GridState(TileData?[] tiles)
        {
            _tiles = (TileData?[])tiles.Clone();
        }

        /// <summary>
        ///     Checks if all slots are filled.
        /// </summary>
        public bool IsFull => _tiles.All(t => t.HasValue);

        /// <summary>
        ///     Checks if the grid is empty.
        /// </summary>
        public bool IsEmpty => _tiles.All(t => !t.HasValue);

        /// <summary>
        ///     Gets the number of tiles on the grid.
        /// </summary>
        public int TileCount => _tiles.Count(t => t.HasValue);

        /// <summary>
        ///     Gets the tile at the specified position (0-8).
        ///     Returns null if the slot is empty.
        /// </summary>
        public TileData? GetTile(int position)
        {
            ValidatePosition(position);
            return _tiles[position];
        }

        /// <summary>
        ///     Gets the tile at the specified grid coordinates (x, y both 0-2).
        /// </summary>
        public TileData? GetTile(int x, int y)
        {
            return GetTile(CoordsToIndex(x, y));
        }

        /// <summary>
        ///     Creates a new GridState with the specified tile placed.
        ///     Immutable - returns a new instance.
        /// </summary>
        public GridState WithTile(int position, TileData tile)
        {
            ValidatePosition(position);
            var newTiles = (TileData?[])_tiles.Clone();
            newTiles[position] = tile;
            return new GridState(newTiles);
        }

        /// <summary>
        ///     Creates a new GridState with the specified tile removed.
        /// </summary>
        public GridState WithoutTile(int position)
        {
            ValidatePosition(position);
            var newTiles = (TileData?[])_tiles.Clone();
            newTiles[position] = null;
            return new GridState(newTiles);
        }

        /// <summary>
        ///     Creates a new GridState with the tile at the specified position rotated.
        /// </summary>
        public GridState WithRotation(int position, int newRotation)
        {
            ValidatePosition(position);
            var tile = _tiles[position];
            if (!tile.HasValue)
                throw new InvalidOperationException($"Cannot rotate empty slot at position {position}");

            return WithTile(position, tile.Value.WithRotation(newRotation));
        }

        /// <summary>
        ///     Creates a new GridState with two tiles swapped.
        /// </summary>
        public GridState WithSwap(int position1, int position2)
        {
            ValidatePosition(position1);
            ValidatePosition(position2);

            var newTiles = (TileData?[])_tiles.Clone();
            (newTiles[position1], newTiles[position2]) = (newTiles[position2], newTiles[position1]);
            return new GridState(newTiles);
        }

        /// <summary>
        ///     Gets all tiles currently on the grid with their positions.
        /// </summary>
        public IEnumerable<(int position, TileData tile)> GetOccupiedSlots()
        {
            for (var i = 0; i < TotalSlots; i++)
                if (_tiles[i].HasValue)
                    yield return (i, _tiles[i].Value);
        }

        /// <summary>
        ///     Gets all empty slot positions.
        /// </summary>
        public IEnumerable<int> GetEmptySlots()
        {
            for (var i = 0; i < TotalSlots; i++)
                if (!_tiles[i].HasValue)
                    yield return i;
        }

        /// <summary>
        ///     Converts grid coordinates to linear index.
        /// </summary>
        public static int CoordsToIndex(int x, int y)
        {
            if (x < 0 || x >= GridSize || y < 0 || y >= GridSize)
                throw new ArgumentOutOfRangeException($"Invalid coordinates: ({x}, {y})");
            return y * GridSize + x;
        }

        /// <summary>
        ///     Converts linear index to grid coordinates.
        /// </summary>
        public static (int x, int y) IndexToCoords(int index)
        {
            ValidatePosition(index);
            return (index % GridSize, index / GridSize);
        }

        private static void ValidatePosition(int position)
        {
            if (position < 0 || position >= TotalSlots)
                throw new ArgumentOutOfRangeException(nameof(position),
                    $"Position must be between 0 and {TotalSlots - 1}");
        }

        public override string ToString()
        {
            var rows = new string[GridSize];
            for (var y = 0; y < GridSize; y++)
            {
                var tiles = new string[GridSize];
                for (var x = 0; x < GridSize; x++)
                {
                    var tile = GetTile(x, y);
                    tiles[x] = tile.HasValue ? tile.Value.ToString() : "Empty";
                }

                rows[y] = string.Join(" | ", tiles);
            }

            return string.Join("\n", rows);
        }
    }
}