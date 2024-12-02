using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TileVirtual
{
    public Vector2 restingPosition;
    public GridBlock gridBlock;
    public GridSlot slot;
    public int rotations;

    public Tile.TileType tileType;

    public TileVirtual(Tile.TileType type)
    {
        tileType = type;

        gridBlock = new GridBlock();

        if (type == Tile.TileType.Curve)
        {
            gridBlock.connections.Add(new List<int>() { 1, 2 });
        }
        else if (type == Tile.TileType.TwoCurves)
        {
            gridBlock.connections.Add(new List<int>() { 0, 3 });
            gridBlock.connections.Add(new List<int>() { 1, 2 });
        }
        else if (type == Tile.TileType.TIntersection)
        {
            gridBlock.connections.Add(new List<int>() { 0, 1, 2 });
        }
        else if (type == Tile.TileType.XIntersection)
        {
            gridBlock.connections.Add(new List<int>() { 0, 1, 2, 3 });
        }
        else if (type == Tile.TileType.Bridge)
        {
            gridBlock.connections.Add(new List<int>() { 0, 2 });
            gridBlock.connections.Add(new List<int>() { 1, 3 });
        }
    }

    public int Rotate()
    {
        rotations += 1;

        return rotations;
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
