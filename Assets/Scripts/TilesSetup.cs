using System.Collections.Generic;
using Grid;
using Other;
using UnityEngine;

public class TilesSetup : MonoBehaviour
{
    [SerializeField] private Tile[] tiles;

    [SerializeField] private MyGrid grid;

    [SerializeField] private TileSprites sprites;

    public void Setup()
    {
        SetupTileSprites();

        const int maxAttempts = 100;
        var attempts = 0;

        do
        {
            InsertTilesIntoGridRandomly();
            attempts++;

            if (attempts < maxAttempts) continue;

            Debug.LogWarning(
                "TilesSetup: Reached maximum attempts to generate a non-winning configuration. Using current setup.");
            break;
        } while (GameManager.Instance.CheckIfWon());

        Debug.Log($"TilesSetup: Generated puzzle in {attempts} attempt(s)");
    }

    private void SetupTileSprites()
    {
        foreach (var tile in tiles) tile.ChangeSprite(sprites.GetSpriteFromType(tile.type));
    }

    private void InsertTilesIntoGridRandomly()
    {
        var availableIndexes = new List<int> { 0, 1, 2, 3, 4, 5, 6, 7, 8 };
        foreach (var tile in tiles)
        {
            var index = MathExt.RandomFrom(availableIndexes);

            availableIndexes.Remove(index);

            grid.gridSlots[index].UpdateTile(tile);
            tile.ResetPosition();

            var rotations = Random.Range(0, 3);
            for (var i = 0; i < rotations; i++) tile.Rotate();
        }
    }
}