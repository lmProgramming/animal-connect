using Grid;
using UnityEngine;

namespace Solver
{
    /// <summary>
    ///     Virtual grid slot implementation for testing without Unity dependencies
    /// </summary>
    public class GridSlotVirtual : IGridSlot
    {
        private readonly Vector2Int _gridPosition;
        private readonly Vector2 _position;
        public TileVirtual Tile;

        public GridSlotVirtual(int x, int y)
        {
            _gridPosition = new Vector2Int(x, y);
            _position = new Vector2(x, y);
        }

        public IPathPoint[] PathPoints { get; set; } = new IPathPoint[4];

        public ITile GetTile()
        {
            return Tile;
        }

        public void UpdateTile(ITile newTile)
        {
            if (newTile is TileVirtual virtualTile) Tile = virtualTile;
        }

        public void RemovedTile()
        {
            Tile = null;
        }

        public Vector2Int GetGridPosition()
        {
            return _gridPosition;
        }

        public Vector2 GetPosition()
        {
            return _position;
        }
    }
}