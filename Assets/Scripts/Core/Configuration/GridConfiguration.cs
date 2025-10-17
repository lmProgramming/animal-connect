namespace Core.Configuration
{
    /// <summary>
    ///     Centralizes all grid topology and path point configuration.
    ///     This matches the exact layout from the original MyGrid.cs SetupPathPoints() method.
    ///     Grid Layout (slots numbered 0-8):
    ///     0 | 1 | 2
    ///     ---------
    ///     3 | 4 | 5
    ///     ---------
    ///     6 | 7 | 8
    ///     Path Points (numbered 0-23, arranged around grid perimeter):
    ///     Top edge: 0, 1, 2
    ///     Right edge: 15, 19, 23
    ///     Bottom edge: 11, 10, 9
    ///     Left edge: 12, 16, 20
    ///     Interior connections: 13, 14, 17, 18, 21, 22, 3, 4, 5, 6, 7, 8
    /// </summary>
    public static class GridConfiguration
    {
        public const int GridSize = 3;
        public const int TotalSlots = 9;
        public const int TotalPathPoints = 24;
        public const int TotalEntities = 12;

        // Maps each grid slot to its 4 adjacent path points [top, right, bottom, left]
        public static readonly int[][] SlotToPathPoints =
        {
            // Slot 0 (top-left)
            new[] { 0, 13, 3, 12 },

            // Slot 1 (top-center)
            new[] { 1, 14, 4, 13 },

            // Slot 2 (top-right)
            new[] { 2, 15, 5, 14 },

            // Slot 3 (middle-left)
            new[] { 3, 17, 6, 16 },

            // Slot 4 (center)
            new[] { 4, 18, 7, 17 },

            // Slot 5 (middle-right)
            new[] { 5, 19, 8, 18 },

            // Slot 6 (bottom-left)
            new[] { 6, 21, 9, 20 },

            // Slot 7 (bottom-center)
            new[] { 7, 22, 10, 21 },

            // Slot 8 (bottom-right)
            new[] { 8, 23, 11, 22 }
        };

        // Maps path point index to entity index (-1 if no entity at that point)
        // Based on GameManager.SetupEntities() method
        private static readonly int[] PathPointToEntity =
        {
            0, // Point 0  -> Entity 0
            1, // Point 1  -> Entity 1
            2, // Point 2  -> Entity 2
            -1, // Point 3  -> No entity
            -1, // Point 4  -> No entity
            -1, // Point 5  -> No entity
            -1, // Point 6  -> No entity
            -1, // Point 7  -> No entity
            -1, // Point 8  -> No entity
            8, // Point 9  -> Entity 8
            7, // Point 10 -> Entity 7
            6, // Point 11 -> Entity 6
            11, // Point 12 -> Entity 11
            -1, // Point 13 -> No entity
            -1, // Point 14 -> No entity
            3, // Point 15 -> Entity 3
            10, // Point 16 -> Entity 10
            -1, // Point 17 -> No entity
            -1, // Point 18 -> No entity
            4, // Point 19 -> Entity 4
            9, // Point 20 -> Entity 9
            -1, // Point 21 -> No entity
            -1, // Point 22 -> No entity
            5 // Point 23 -> Entity 5
        };

        // Reverse mapping: entity index to path point index
        public static readonly int[] EntityToPathPoint =
        {
            0, // Entity 0  -> Point 0
            1, // Entity 1  -> Point 1
            2, // Entity 2  -> Point 2
            15, // Entity 3  -> Point 15
            19, // Entity 4  -> Point 19
            23, // Entity 5  -> Point 23
            11, // Entity 6  -> Point 11
            10, // Entity 7  -> Point 10
            9, // Entity 8  -> Point 9
            20, // Entity 9  -> Point 20
            16, // Entity 10 -> Point 16
            12 // Entity 11 -> Point 12
        };

        /// <summary>
        ///     Checks if a path point has an entity.
        /// </summary>
        public static bool IsEntityPoint(int pathPoint)
        {
            return pathPoint >= 0 && pathPoint < TotalPathPoints &&
                   PathPointToEntity[pathPoint] != -1;
        }

        /// <summary>
        ///     Gets the entity index at a path point, or -1 if none.
        /// </summary>
        public static int GetEntityAtPoint(int pathPoint)
        {
            if (pathPoint < 0 || pathPoint >= TotalPathPoints)
                return -1;
            return PathPointToEntity[pathPoint];
        }

        /// <summary>
        ///     Gets the path point for an entity.
        /// </summary>
        public static int GetPathPointForEntity(int entityIndex)
        {
            if (entityIndex < 0 || entityIndex >= TotalEntities)
                return -1;
            return EntityToPathPoint[entityIndex];
        }

        /// <summary>
        ///     Validates that path point connections are legal.
        ///     Entity points can have 0 or 1 connection (0 = unconnected, 1 = connected to path).
        ///     Non-entity points must have 0 or 2 connections (no dead ends or branch points).
        /// </summary>
        public static bool IsValidConnectionCount(int pathPoint, int connectionCount)
        {
            var isEntity = IsEntityPoint(pathPoint);

            if (isEntity) return connectionCount == 0 || connectionCount == 1;

            return connectionCount == 0 || connectionCount == 2;
        }
    }
}