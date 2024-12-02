using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Grid : MonoBehaviour
{
    public GridSlot[] gridSlots = new GridSlot[9];
    public GridSlot[,] gridSlotsMap = new GridSlot[3, 3];

    public PathPoint[] pathPoints;
    public PathPoint[] entitiesPathPoints;

    int numberOfPaths = 0;

    private void Start()
    {
        SetupGridSlots();
    }

    private void SetupGridSlots()
    {
        int i = 0;
        for (int y = 0; y < 3; y++)
        {
            for (int x = 0; x < 3; x++)
            {
                gridSlotsMap[x, y] = gridSlots[i];
                i += 1;
            }
        }
    }

    private void SetupPathPoints()
    {
        int i = 0;
        for (int y = 0; y < 3; y++)
        {
            for (int x = 0; x < 3; x++)
            {
                gridSlotsMap[x, y] = gridSlots[i];
                i += 1;
            }
        }
    }

    public GridSlot GetGridSlot(Vector2Int gridPos)
    {
        return gridSlotsMap[gridPos.x, 2 - gridPos.y];
    }

    public Tile GetTile(Vector2Int gridPos)
    {
        return gridSlotsMap[gridPos.x, 2 - gridPos.y].GetTile();
    }

    public bool ChechIfValidPaths()
    {
        for (int i = 0; i < gridSlots.Length; i++)
        {
            if (gridSlots[i].GetTile() == null)
            {
                return false;
            }
        }
        for (int i = 0; i < pathPoints.Length; i++)
        {
            if (!pathPoints[i].CheckIfPathOnTopValid())
            {
                return false;
            }
        }
        return true;
    }

    public static Vector2Int GridPosition(Vector2 position)
    {
        return new Vector2Int((int)position.x + 1, (int)position.y + 1);
    }

    public void RecalculatePathConnections()
    {
        numberOfPaths = 0;

        for (int i = 0; i < pathPoints.Length; i++)
        {
            pathPoints[i].pathNum = -1;
            pathPoints[i].ResetConectionsNumber();
        }

        for (int i = 0; i < gridSlots.Length; i++)
        {
            gridSlots[i].UpdateConnections(this);
        }
    }

    public int GetNewPathNumber()
    {
        numberOfPaths++;
        return numberOfPaths;
    }

    public void MergePaths(int pathNum1, int pathNum2)
    {
        foreach (var pathPoint in pathPoints)
        {
            if (pathPoint.pathNum == pathNum2)
            {
                // Debug.Log("changed " + pathPoint.pathNum + " to " + pathNum1);
                pathPoint.UpdatePathNum(pathNum1);
            }
        }
    }

    public void PlaceTile(Tile tile, Vector2 newTilePostion, GridSlot gridSlot)
    {
        bool addToGrid = gridSlot != null;

        GridSlot formerGridSlot = tile.slot;

        if (addToGrid)
        {
            formerGridSlot.RemovedTile();

            Tile tileAtNewGridSlot = gridSlot.GetTile();

            if (tileAtNewGridSlot != null && tileAtNewGridSlot != tile)
            {
                SwapTiles(tile, formerGridSlot, tileAtNewGridSlot, gridSlot);
            }
            else
            {
                gridSlot.UpdateTile(tile);
            }
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

        if (formerGridSlot != null)
        {
            formerGridSlot.UpdateTile(tileToSwap);
        }

        tileToSwap.transform.position = formerGridSlot.GetPosition();
    }
}
