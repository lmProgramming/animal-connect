using Grid;
using Other;
using UnityEngine;

public sealed class TileDragger : MonoBehaviour
{
    public static TileDragger Instance;
    public float timeDraggedATile;
    public Tile draggedTile;

    [SerializeField] private float hoverBias = 0.1f;

    [SerializeField] private MyGrid grid;

    private void Awake()
    {
        Instance = this;
    }

    // Update is called once per frame
    private void Update()
    {
        if (draggedTile != null)
        {
            if (timeDraggedATile > 0.05f) draggedTile.transform.position = GameInput.WorldPointerPosition;

            Vector2 tilePosition = draggedTile.transform.position;

            var gridSlot = FindClosestSlot(tilePosition);

            tilePosition = gridSlot.GetPosition();
            draggedTile.transform.position = GetTileHoveringPosition(GameInput.WorldPointerPosition, gridSlot);

            SwapTilesVisually(gridSlot, draggedTile);

            if (Input.GetMouseButtonUp(0))
            {
                if (timeDraggedATile < 0.2f) draggedTile.Rotate();

                MyGrid.PlaceTile(draggedTile, tilePosition, gridSlot);

                LetGoOfTile();
            }

            timeDraggedATile += Time.deltaTime;
        }
    }

    private Vector2 GetTileHoveringPosition(Vector2 dragPosition, GridSlot gridSlot)
    {
        var gridSlotPositon = gridSlot.GetPosition();

        return gridSlotPositon + (dragPosition - gridSlotPositon) * 0.1f + new Vector2(0, hoverBias);
    }

    private void SwapTilesVisually(GridSlot newGridSlot, Tile draggedTile)
    {
        var otherTile = newGridSlot.GetTile();

        if (otherTile == draggedTile) return;

        var oldGridSlot = draggedTile.slot;

        otherTile.transform.position = oldGridSlot.GetPosition() + new Vector2(0, hoverBias);
        otherTile.ResetPositionAfterXFrames(1);
    }

    public GridSlot FindClosestSlot(Vector2 position)
    {
        var slotsMap = grid.GridSlotsMap;

        var closestDistance = float.PositiveInfinity;
        GridSlot closestSlot = null;

        for (var x = -1; x <= 1; x++)
        for (var y = -1; y <= 1; y++)
        {
            var distance = Vector2.Distance(new Vector2(x, y), position);
            if (distance < closestDistance)
            {
                closestDistance = distance;

                var gridPosition = new Vector2Int(x + 1, 1 - y);
                closestSlot = slotsMap[gridPosition.x, gridPosition.y];
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