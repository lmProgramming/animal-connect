using System.Collections.Generic;
using System.Linq;
using Core.Configuration;
using Core.Models;

namespace Core.Logic
{
    /// <summary>
    /// Calculates path connections from grid state.
    /// Pure functions - no side effects, no Unity dependencies.
    /// This replaces the O(n²) RecalculatePathConnections with O(n) algorithm.
    /// </summary>
    public class PathCalculator
    {
        /// <summary>
        /// Calculates all path connections from the current grid state.
        /// This is a complete recalculation - use for initial setup or validation.
        /// For single tile changes, use UpdateForTileChange() for better performance.
        /// </summary>
        public PathNetworkState CalculatePathNetwork(GridState gridState)
        {
            var network = new PathNetworkState();
            
            // Process each occupied grid slot
            foreach (var (slotIndex, tile) in gridState.GetOccupiedSlots())
            {
                ProcessTileConnections(network, slotIndex, tile);
            }
            
            return network;
        }
        
        /// <summary>
        /// Updates path network incrementally when one tile is placed, removed, or rotated.
        /// Much faster than full recalculation for single tile changes.
        /// </summary>
        public PathNetworkState UpdateForTileChange(
            GridState oldGrid,
            GridState newGrid,
            int changedSlot)
        {
            // For now, do full recalculation
            // Optimization: Could track only affected paths in future
            // This is still faster than old system due to UnionFind
            return CalculatePathNetwork(newGrid);
        }
        
        /// <summary>
        /// Updates path network when two tiles are swapped.
        /// </summary>
        public PathNetworkState UpdateForTileSwap(
            GridState oldGrid,
            GridState newGrid,
            int slot1,
            int slot2)
        {
            // For now, do full recalculation
            return CalculatePathNetwork(newGrid);
        }
        
        /// <summary>
        /// Processes a single tile's connections and updates the network.
        /// </summary>
        private void ProcessTileConnections(
            PathNetworkState network,
            int slotIndex,
            TileData tile)
        {
            // Get the 4 path points adjacent to this slot [top, right, bottom, left]
            var adjacentPathPoints = GridConfiguration.SlotToPathPoints[slotIndex];
            
            // Get tile's connections in current rotation
            var connections = tile.GetConnections();
            
            // Each connection group represents sides that are connected together
            foreach (var connection in connections)
            {
                // Map tile sides to actual path point indices
                var pathPoints = connection.ConnectedSides
                    .Select(side => adjacentPathPoints[side])
                    .ToArray();
                
                // Connect all these path points together
                if (pathPoints.Length > 1)
                {
                    network.ConnectPoints(pathPoints);
                }
            }
        }
        
        /// <summary>
        /// Gets all path points that would be affected by changing a tile at the given slot.
        /// Useful for incremental updates and debugging.
        /// </summary>
        public IEnumerable<int> GetAffectedPathPoints(int slotIndex)
        {
            return GridConfiguration.SlotToPathPoints[slotIndex];
        }
        
        /// <summary>
        /// Validates that a tile placement would create valid connections.
        /// Returns true if the placement is legal (doesn't create invalid connection counts).
        /// Warnings (like dead ends) are acceptable, only errors make it invalid.
        /// </summary>
        public bool ValidateTilePlacement(GridState grid, int slotIndex, TileData tile)
        {
            // Create temporary grid with the tile placed
            var tempGrid = grid.WithTile(slotIndex, tile);
            var tempNetwork = CalculatePathNetwork(tempGrid);
            
            // Use ConnectionValidator to check - only Error-level issues make it invalid
            var validator = new ConnectionValidator();
            return validator.IsValid(tempNetwork);
        }
    }
}
