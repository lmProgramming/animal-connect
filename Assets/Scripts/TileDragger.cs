using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public sealed class TileDragger : MonoBehaviour
{
    public static TileDragger Instance;
    public float timeDraggedATile;
    public Tile draggedTile;

    [SerializeField]
    float hoverBias = 0.1f;

    [SerializeField]
    Grid grid;

    private void Awake()
    {
        Instance = this;
    }

    // Update is called once per frame
    void Update()
    {
        if (draggedTile != null)
        {
            if (timeDraggedATile > 0.05f)
            {
                draggedTile.transform.position = GameInput.WorldPointerPosition;
            }

            Vector2 tilePosition = draggedTile.transform.position;

            GridSlot gridSlot = FindClosestSlot(tilePosition);

            tilePosition = gridSlot.GetPosition();
            draggedTile.transform.position = GetTileHoveringPostion(GameInput.WorldPointerPosition, gridSlot);

            SwapTilesVisually(gridSlot, draggedTile);

            if (Input.GetMouseButtonUp(0))
            {
                if (timeDraggedATile < 0.2f)
                {
                    draggedTile.Rotate();
                }

                grid.PlaceTile(draggedTile, tilePosition, gridSlot);

                LetGoOfTile();
            }

            timeDraggedATile += Time.deltaTime;
        }
    }

    Vector2 GetTileHoveringPostion(Vector2 dragPosition, GridSlot gridSlot)
    {
        Vector2 gridSlotPositon = gridSlot.GetPosition();

        return gridSlotPositon + (dragPosition - gridSlotPositon) * 0.1f + new Vector2(0, hoverBias);
    }

    void SwapTilesVisually(GridSlot newGridSlot, Tile draggedTile)
    {
        Tile otherTile = newGridSlot.GetTile();

        if (otherTile == draggedTile)
        {
            return;
        }

        GridSlot oldGridSlot = draggedTile.slot;

        otherTile.transform.position = oldGridSlot.GetPosition() + new Vector2(0, hoverBias);
        otherTile.ResetPositionAfterXFrames(1);
    }
    
    public GridSlot FindClosestSlot(Vector2 position)
    {
        GridSlot[,] slotsMap = grid.gridSlotsMap;

        float closestDistance = float.PositiveInfinity;
        GridSlot closestSlot = null;

        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;

                    Vector2Int gridPosition = new Vector2Int(x + 1, 1 - y);
                    closestSlot = slotsMap[gridPosition.x, gridPosition.y];
                }
            }
        }

        return closestSlot;
    }

    public void GrabThisTile(Tile tileGrabbed)
    {
        draggedTile = tileGrabbed;

        timeDraggedATile = 0;

        //draggedTile.GetComponent<SpriteRenderer>().sortingOrder = 10000;

        draggedTile.Lighten();
    }

    public void LetGoOfTile()
    {
        draggedTile.transform.position = draggedTile.slot.GetPosition();

        //draggedTile.GetComponent<SpriteRenderer>().sortingOrder = 0;

        draggedTile.ResetColor();

        draggedTile = null;
    }
}
