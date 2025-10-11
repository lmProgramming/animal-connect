using UnityEngine;

public class TileSprites : MonoBehaviour
{
    public Sprite[] sprites = new Sprite[5];

    public Sprite GetSpriteFromType(Tile.TileType tileType)
    {
        switch (tileType)
        {
            case Tile.TileType.Curve:
                return sprites[0];
            case Tile.TileType.TwoCurves:
                return sprites[1];
            case Tile.TileType.Intersection:
                return sprites[2];
            case Tile.TileType.XIntersection:
                return sprites[3];
            case Tile.TileType.Bridge:
                return sprites[4];
            default:
                Debug.LogWarning("No sprite");
                return null;
        }
    }
}