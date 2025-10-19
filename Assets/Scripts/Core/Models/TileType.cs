namespace Core.Models
{
    /// <summary>
    ///     Tile types matching the original game.
    /// </summary>
    public enum TileType
    {
        Curve, // 90-degree turn
        TwoCurves, // S-shape (two separate curves)
        Intersection, // T-junction (connects 3 sides)
        XIntersection, // Cross (connects all 4 sides)
        Bridge, // Two straight paths crossing (two separate connections)
        Empty // no connections
    }
}