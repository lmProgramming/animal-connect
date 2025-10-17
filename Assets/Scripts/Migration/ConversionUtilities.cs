using System;
using System.Collections.Generic;
using System.Linq;
using Core.Models;
using Grid;
using Solver;
using UnityEngine;

namespace Migration
{
    /// <summary>
    /// Utilities for converting between old and new system data structures.
    /// Provides bidirectional conversion to maintain compatibility during migration.
    /// </summary>
    public static class ConversionUtilities
    {
        /// <summary>
        /// Converts old system grid to new GridState.
        /// </summary>
        public static GridState ConvertToGridState(MyGrid oldGrid)
        {
            if (oldGrid == null)
            {
                throw new ArgumentNullException(nameof(oldGrid));
            }
            
            var gridState = new GridState();
            
            for (int i = 0; i < oldGrid.gridSlots.Length && i < 9; i++)
            {
                var slot = oldGrid.gridSlots[i];
                if (slot != null && slot.Tile != null)
                {
                    var tileData = ConvertToTileData(slot.Tile, i);
                    gridState = gridState.WithTile(i, tileData);
                }
            }
            
            return gridState;
        }
        
        /// <summary>
        /// Converts old Tile to new TileData.
        /// </summary>
        public static TileData ConvertToTileData(Tile oldTile, int gridPosition)
        {
            if (oldTile == null)
            {
                throw new ArgumentNullException(nameof(oldTile));
            }
            
            var tileType = ConvertTileType(oldTile);
            var rotation = GetTileRotation(oldTile);
            
            // Create TileData (only type and rotation, position is tracked separately in GridState)
            return new TileData(tileType, rotation);
        }
        
        /// <summary>
        /// Converts old tile type to new TileType enum.
        /// </summary>
        private static TileType ConvertTileType(Tile oldTile)
        {
            // Direct conversion from old Tile.TileType to new Core.Models.TileType
            return oldTile.type switch
            {
                Tile.TileType.Curve => TileType.Curve,
                Tile.TileType.TwoCurves => TileType.TwoCurves,
                Tile.TileType.Intersection => TileType.Intersection,
                Tile.TileType.XIntersection => TileType.XIntersection,
                Tile.TileType.Bridge => TileType.Bridge,
                _ => throw new ArgumentException($"Unknown tile type: {oldTile.type}")
            };
        }
        

        
        /// <summary>
        /// Gets the rotation value from an old tile (0-3).
        /// </summary>
        private static int GetTileRotation(Tile oldTile)
        {
            // Get rotation from transform
            float zRotation = oldTile.transform.rotation.eulerAngles.z;
            
            // Normalize to 0-3 range
            // Unity rotations are counter-clockwise, so we need to adjust
            int rotation = Mathf.RoundToInt(zRotation / 90f) % 4;
            
            // Ensure positive value
            if (rotation < 0) rotation += 4;
            
            return rotation;
        }
        
        /// <summary>
        /// Converts old PathPoint array to new PathNetworkState.
        /// </summary>
        public static PathNetworkState ConvertToPathNetworkState(PathPoint[] oldPathPoints)
        {
            if (oldPathPoints == null)
            {
                throw new ArgumentNullException(nameof(oldPathPoints));
            }
            
            var network = new PathNetworkState();
            
            // Build connection map from old path numbers
            var pathGroups = new Dictionary<int, List<int>>();
            
            for (int i = 0; i < oldPathPoints.Length; i++)
            {
                int pathNum = oldPathPoints[i].pathNum;
                
                if (pathNum >= 0)
                {
                    if (!pathGroups.ContainsKey(pathNum))
                    {
                        pathGroups[pathNum] = new List<int>();
                    }
                    pathGroups[pathNum].Add(i);
                }
            }
            
            // Connect all points in each path group
            foreach (var group in pathGroups.Values)
            {
                if (group.Count > 1)
                {
                    // Connect all pairs in this group
                    for (int i = 0; i < group.Count - 1; i++)
                    {
                        network.ConnectPoints(group[i], group[i + 1]);
                    }
                }
            }
            
            return network;
        }
        
        /// <summary>
        /// Converts new GridState back to old system format (for testing).
        /// NOTE: This modifies the old grid in place.
        /// </summary>
        public static void ApplyGridStateToOldSystem(GridState newGrid, MyGrid oldGrid)
        {
            if (newGrid == null)
            {
                throw new ArgumentNullException(nameof(newGrid));
            }
            if (oldGrid == null)
            {
                throw new ArgumentNullException(nameof(oldGrid));
            }
            
            // Update each slot
            for (int i = 0; i < 9; i++)
            {
                var tileData = newGrid.GetTile(i);
                var oldSlot = oldGrid.gridSlots[i];
                
                if (oldSlot == null) continue;
                
                if (tileData.HasValue)
                {
                    // If tile exists in new system but not old, we can't create it here
                    // This is for rotation updates only
                    if (oldSlot.Tile != null)
                    {
                        ApplyRotationToOldTile(oldSlot.Tile, tileData.Value.Rotation);
                    }
                }
            }
        }
        
        /// <summary>
        /// Applies rotation to an old tile.
        /// </summary>
        private static void ApplyRotationToOldTile(Tile oldTile, int rotation)
        {
            if (oldTile == null) return;
            
            // Convert rotation (0-3) to Unity Euler angles
            float zRotation = -rotation * 90f; // Negative because Unity rotates counter-clockwise
            oldTile.transform.rotation = Quaternion.Euler(0, 0, zRotation);
        }
        
        /// <summary>
        /// Extracts a Quest from the old GameManager format.
        /// </summary>
        public static Quest.Quest ConvertToQuest(GameManager oldGameManager)
        {
            if (oldGameManager == null)
            {
                throw new ArgumentNullException(nameof(oldGameManager));
            }
            
            // The old system already uses Quest.Quest, so just return it
            return oldGameManager.Quest;
        }
        
        /// <summary>
        /// Creates a complete GameState from old system components.
        /// </summary>
        public static GameState CreateGameStateFromOldSystem(MyGrid oldGrid, GameManager oldGameManager)
        {
            var gridState = ConvertToGridState(oldGrid);
            var pathState = ConvertToPathNetworkState(oldGrid.pathPoints);
            // TODO: Implement Quest to QuestData conversion when QuestData is defined
            // var questData = ConvertToQuestData(oldGameManager.Quest);
            
            // For now, create a placeholder QuestData with empty entity groups
            var questData = new QuestData(new List<EntityGroup>());
            
            return new GameState(gridState, pathState, questData);
        }
        
        /// <summary>
        /// Validates that conversion is correct by comparing key properties.
        /// </summary>
        public static bool ValidateConversion(MyGrid oldGrid, GridState newGrid)
        {
            // Check that tile counts match
            int oldTileCount = oldGrid.gridSlots.Count(slot => slot.Tile != null);
            int newTileCount = 0;
            for (int i = 0; i < GridState.TotalSlots; i++)
            {
                if (newGrid.GetTile(i) != null) newTileCount++;
            }
            
            if (oldTileCount != newTileCount)
            {
                Debug.LogError($"Tile count mismatch: old={oldTileCount}, new={newTileCount}");
                return false;
            }
            
            // Check that tile positions match
            for (int i = 0; i < 9; i++)
            {
                bool oldHasTile = oldGrid.gridSlots[i].Tile != null;
                bool newHasTile = newGrid.GetTile(i).HasValue;
                
                if (oldHasTile != newHasTile)
                {
                    Debug.LogError($"Tile presence mismatch at slot {i}: old={oldHasTile}, new={newHasTile}");
                    return false;
                }
            }
            
            return true;
        }
        
        /// <summary>
        /// Helper to get all tiles with their positions from old grid.
        /// </summary>
        public static IEnumerable<(int position, Tile tile)> GetOldGridTiles(MyGrid oldGrid)
        {
            for (int i = 0; i < oldGrid.gridSlots.Length && i < 9; i++)
            {
                var slot = oldGrid.gridSlots[i];
                if (slot?.Tile != null)
                {
                    yield return (i, slot.Tile);
                }
            }
        }
        
        /// <summary>
        /// Helper to compare path point states between systems.
        /// </summary>
        public static bool ComparePathStates(PathPoint[] oldPathPoints, PathNetworkState newPaths)
        {
            if (oldPathPoints == null || oldPathPoints.Length != 24)
            {
                return false;
            }
            
            // Compare connection counts
            for (int i = 0; i < 24; i++)
            {
                int oldCount = oldPathPoints[i].ConnectionsNumber;
                int newCount = newPaths.GetConnectionCount(i);
                
                if (oldCount != newCount)
                {
                    return false;
                }
            }
            
            // Compare connectivity
            for (int i = 0; i < 24; i++)
            {
                for (int j = i + 1; j < 24; j++)
                {
                    bool oldConnected = oldPathPoints[i].pathNum != -1 && 
                                       oldPathPoints[i].pathNum == oldPathPoints[j].pathNum;
                    bool newConnected = newPaths.AreConnected(i, j);
                    
                    if (oldConnected != newConnected)
                    {
                        return false;
                    }
                }
            }
            
            return true;
        }
    }
}
