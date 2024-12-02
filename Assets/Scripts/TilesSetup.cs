using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TilesSetup : MonoBehaviour
{
    [SerializeField]
    Tile[] tiles;

    [SerializeField]
    Grid grid;

    [SerializeField]
    TileSprites sprites;

    public void Setup()
    {
        SetupTileSprites();

        do
        {
            InsertTilesIntoGridRandomly();

        } while (GameManager.Instance.CheckIfWon());
    }

    void SetupTileSprites()
    {
        foreach (Tile tile in tiles)
        {
            tile.ChangeSprite(sprites.GetSpriteFromType(tile.type));
        }
    }

    void InsertTilesIntoGridRandomly()
    {
        List<int> availableIndexes = new List<int>() { 0, 1, 2, 3, 4, 5, 6, 7, 8 } ;
        foreach (var tile in tiles)
        {
            int index = MathExt.RandomFrom(availableIndexes);

            availableIndexes.Remove(index);

            grid.gridSlots[index].UpdateTile(tile);
            tile.ResetPosition();

            int rotations = Random.Range(0, 3);
            for (int i = 0; i < rotations; i++)
            {
                tile.Rotate();
            }
        }
    }
}
