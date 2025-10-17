using System.Collections.Generic;
using Grid;
using UnityEngine;

namespace Solver
{
    /// <summary>
    /// Virtual tile implementation for testing without Unity dependencies
    /// </summary>
    public class TileVirtual : ITile
    {
        public readonly GridBlock GridBlock;

        public readonly Tile.TileType TileType;
        public Vector2 RestingPosition;
        private int _rotations;
        public GridSlot Slot;

        public int Rotations => _rotations;

        public TileVirtual(Tile.TileType type, int rotations = 0)
        {
            TileType = type;
            _rotations = rotations;

            GridBlock = new GridBlock();

            if (type == Tile.TileType.Curve)
            {
                GridBlock.Connections.Add(new List<int> { 1, 2 });
            }
            else if (type == Tile.TileType.TwoCurves)
            {
                GridBlock.Connections.Add(new List<int> { 0, 3 });
                GridBlock.Connections.Add(new List<int> { 1, 2 });
            }
            else if (type == Tile.TileType.Intersection)
            {
                GridBlock.Connections.Add(new List<int> { 0, 1, 2 });
            }
            else if (type == Tile.TileType.XIntersection)
            {
                GridBlock.Connections.Add(new List<int> { 0, 1, 2, 3 });
            }
            else if (type == Tile.TileType.Bridge)
            {
                GridBlock.Connections.Add(new List<int> { 0, 2 });
                GridBlock.Connections.Add(new List<int> { 1, 3 });
            }
        }

        public IGridBlock GetGridBlock()
        {
            return GridBlock;
        }

        public int Rotate()
        {
            _rotations += 1;

            // Apply rotation limits based on tile type
            var rotationsLimit = TileType switch
            {
                Tile.TileType.TwoCurves => 2,
                Tile.TileType.XIntersection => 1,
                Tile.TileType.Bridge => 1,
                _ => 4
            };

            if (_rotations == rotationsLimit) _rotations = 0;

            return _rotations;
        }
    }
}