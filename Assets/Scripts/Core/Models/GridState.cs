using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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

        public string PrettyString => ToPrettyString();

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

        public GridState FillEmptyWith(TileData tileData)
        {
            var newTiles = (TileData?[])_tiles.Clone();
            for (var i = 0; i < TotalSlots; i++) newTiles[i] ??= tileData;

            return new GridState(newTiles);
        }

        /// <summary>
        ///     Returns a pretty-printed graphical representation of the grid.
        ///     Each tile is displayed as a 3x3 character block showing its connections and rotation.
        /// </summary>
        public string ToPrettyString()
        {
            var result = new StringBuilder();
            var horizontalSeparator = "+" + string.Join("+", Enumerable.Repeat("-----", GridSize)) + "+\n";

            result.Append(horizontalSeparator);

            for (var row = 0; row < GridSize; row++)
            {
                // Each tile is represented in 3 lines
                var line1 = new string[GridSize];
                var line2 = new string[GridSize];
                var line3 = new string[GridSize];

                for (var col = 0; col < GridSize; col++)
                {
                    var tile = GetTile(col, row);
                    var (l1, l2, l3) = GetTileGraphic(tile);
                    line1[col] = l1;
                    line2[col] = l2;
                    line3[col] = l3;
                }

                result.Append("│" + string.Join("│", line1) + "│\n");
                result.Append("│" + string.Join("│", line2) + "│\n");
                result.Append("│" + string.Join("│", line3) + "│\n");
                result.Append(horizontalSeparator);
            }

            return result.ToString();
        }

        /// <summary>
        ///     Returns a 3-line graphical representation of a tile showing its connections.
        /// </summary>
        private static (string line1, string line2, string line3) GetTileGraphic(TileData? tile)
        {
            if (!tile.HasValue)
                return ("     ", "     ", "     ");

            var t = tile.Value;
            var rotation = t.Rotation;

            return t.Type switch
            {
                TileType.Curve => GetCurveGraphic(rotation),
                TileType.TwoCurves => GetTwoCurvesGraphic(rotation),
                TileType.Intersection => GetIntersectionGraphic(rotation),
                TileType.XIntersection => GetXIntersectionGraphic(),
                TileType.Bridge => GetBridgeGraphic(),
                TileType.Empty => ("     ", "     ", "     "),
                _ => ("  ?  ", " ??? ", "  ?  ")
            };
        }

        private static (string, string, string) GetCurveGraphic(int rotation)
        {
            return (rotation % 4) switch
            {
                0 => ("     ", "  ╔══", "  ║  "), // Right to Bottom
                1 => ("     ", "══╗  ", "  ║  "),
                2 => ("  ║  ", "══╝  ", "     "),
                3 => ("  ║  ", "  ╚══", "     "),
                _ => throw new InvalidProgramException()
            };
        }

        private static (string, string, string) GetTwoCurvesGraphic(int rotation)
        {
            return (rotation % 2) switch
            {
                0 => ("  ║  ", "══╝╔═", "  ╱  "), // Top-Left and Right-Bottom curves ╔═ ╚═
                1 => ("  ╲  ", "═╗╚══", "  ║  "), // Left-Top and Right-Bottom curves (rotated 90)
                _ => throw new InvalidProgramException()
            };
        }

        private static (string, string, string) GetIntersectionGraphic(int rotation)
        {
            return (rotation % 4) switch
            {
                0 => ("  ║  ", "  ╠══", "  ║  "), // T-junction: Top, Right, Bottom
                1 => ("     ", "══╬══", "  ║  "), // T-junction: Left, Right, Bottom
                2 => ("  ║  ", "══╣  ", "  ║  "), // T-junction: Top, Left, Bottom
                3 => ("  ║  ", "══╬══", "     "), // T-junction: Top, Left, Right
                _ => throw new InvalidProgramException()
            };
        }

        private static (string, string, string) GetXIntersectionGraphic()
        {
            return ("  ║  ", "══╬══", "  ║  ");
        }

        private static (string, string, string) GetBridgeGraphic()
        {
            return ("  ║  ", "══)══", "  ║  ");
        }
    }
}