using System.Collections.Generic;
using Grid;
using UnityEngine;

namespace Solver
{
    public class TileVirtual
    {
        public readonly GridBlock GridBlock;

        public readonly Tile.TileType TileType;
        public Vector2 RestingPosition;
        public int Rotations;
        public GridSlot Slot;

        public TileVirtual(Tile.TileType type)
        {
            TileType = type;

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

        public int Rotate()
        {
            Rotations += 1;

            return Rotations;
        }

        //void OnMouseDown()
        //{
        //    if (slot != null)
        //    {
        //        slot.tile = null;
        //        slot = null;
        //        GameManager.Instance.ResetPathConnections();
        //    }
        //    TileDragger.Instance.GrabThisTile(this);
        //}
    }
}