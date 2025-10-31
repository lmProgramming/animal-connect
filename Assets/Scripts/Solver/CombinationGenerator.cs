using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Core.Configuration;
using Core.Logic;
using Core.Models;
using UnityEngine;

namespace Solver
{
    public class CombinationGenerator : MonoBehaviour
    {
        [Header("Tile Set Configuration")]
        [SerializeField] private TileSetEntry[] tileSets;

        [Header("Output Configuration")]
        [SerializeField] private string outputFileName = "valid_combinations.csv";

        [SerializeField] private bool includeGridVisualization = true;

        public bool checkMaxIterations;
        public int maxIterations = 1000;

        private PathCalculator _pathCalculator;
        private DateTime _startTime;

        private int _totalCombinationsChecked;
        private ConnectionValidator _validator;
        private int _validCombinationsFound;

        private static TileData[] DefaultTileSet =>
            new[]
            {
                new TileData(TileType.Curve),
                new TileData(TileType.Curve),
                new TileData(TileType.Curve),
                new TileData(TileType.TwoCurves),
                new TileData(TileType.TwoCurves),
                new TileData(TileType.XIntersection),
                new TileData(TileType.Bridge),
                new TileData(TileType.Intersection),
                new TileData(TileType.Intersection)
            };

        [ContextMenu("Generate All Valid Combinations")]
        public void GenerateAllCombinations()
        {
            var tileSet = GetCurrentTileSet();
            if (tileSet == null || tileSet.Length == 0)
            {
                Debug.LogError("CombinationGenerator: No tiles configured!");
                return;
            }

            Debug.Log($"CombinationGenerator: Starting generation for {tileSet.Length} tiles...");
            _startTime = DateTime.Now;
            _totalCombinationsChecked = 0;
            _validCombinationsFound = 0;

            _pathCalculator = new PathCalculator();
            _validator = new ConnectionValidator();

            var validCombinations = new List<GridState>();
            var uniqueCombinations = new HashSet<int>();

            // Convert to tile counts for more efficient generation
            var tileCounts = GetTileCounts(tileSet);

            // Generate all valid combinations
            GenerateRecursive(new GridState(), tileCounts, 0, validCombinations, uniqueCombinations);

            var filePath = SaveToCsv(validCombinations);

            var duration = (DateTime.Now - _startTime).TotalSeconds;
            Debug.Log("CombinationGenerator: Complete!");
            Debug.Log($"Checked: {_totalCombinationsChecked:N0} combinations");
            Debug.Log($"Found: {_validCombinationsFound:N0} valid combinations");
            Debug.Log($"Duration: {duration:F2} seconds");
            Debug.Log($"File: {filePath}");
        }

        private TileData[] GetCurrentTileSet()
        {
            if (tileSets == null || tileSets.Length == 0)
                return DefaultTileSet;

            var tiles = new List<TileData>();
            foreach (var entry in tileSets)
                for (var i = 0; i < entry.count; i++)
                    tiles.Add(new TileData(entry.type));

            return tiles.ToArray();
        }

        private static Dictionary<TileType, int> GetTileCounts(TileData[] tiles)
        {
            var counts = new Dictionary<TileType, int>();
            foreach (var tile in tiles)
            {
                counts.TryAdd(tile.Type, 0);
                counts[tile.Type]++;
            }

            return counts;
        }

        private void GenerateRecursive(
            GridState currentGrid,
            Dictionary<TileType, int> tileCounts,
            int slot,
            List<GridState> validCombinations,
            HashSet<int> uniqueCombinations)
        {
            if (checkMaxIterations)
            {
                maxIterations -= 1;

                if (maxIterations <= 0) return;
            }

            // If we've placed all tiles, check if the configuration is valid
            if (slot >= GridState.TotalSlots)
            {
                _totalCombinationsChecked++;

                if (_totalCombinationsChecked % 10000 == 0)
                    Debug.Log(
                        $"   Progress: {_totalCombinationsChecked:N0} checked, {_validCombinationsFound:N0} valid found...");

                if (!IsValidGameState(currentGrid)) return;

                var hash = ComputeGridHash(currentGrid);
                if (!uniqueCombinations.Add(hash)) return;
                validCombinations.Add(currentGrid);
                _validCombinationsFound++;

                return;
            }

            foreach (var tileType in tileCounts.Keys.ToList())
            {
                if (tileCounts[tileType] == 0) continue;

                var maxRotations = tileType.GetMaxRotations();

                // Try all rotations for this tile type
                for (var rotation = 0; rotation < maxRotations; rotation++)
                {
                    var tile = new TileData(tileType, rotation);
                    var newGrid = currentGrid.WithTile(slot, tile);

                    if (!QuickValidate(newGrid, slot)) continue;

                    tileCounts[tileType]--;

                    GenerateRecursive(newGrid, tileCounts, slot + 1, validCombinations, uniqueCombinations);

                    // Return the tile for next iteration
                    tileCounts[tileType]++;
                }
            }
        }

        private bool QuickValidate(GridState grid, int placedSlot)
        {
            var pathNetwork = _pathCalculator.CalculatePathNetwork(grid);

            // Get the 4 path points for this slot
            var slotPathPoints = GridConfiguration.SlotToPathPoints[placedSlot];

            foreach (var pathPoint in slotPathPoints)
            {
                var connectionCount = pathNetwork.GetConnectionCount(pathPoint);

                if (connectionCount > 2)
                    return false;

                if (GridConfiguration.IsEntityPoint(pathPoint) && connectionCount > 1)
                    return false;
            }

            if (placedSlot >= GridState.TotalSlots - 1) return true; // Not the last tile
            // Check all already-placed slots for path points that have dead ends that can't be fixed
            for (var slot = 0; slot <= placedSlot; slot++)
            {
                var tile = grid.GetTile(slot);
                if (!tile.HasValue)
                    continue;

                var pointsInSlot = GridConfiguration.SlotToPathPoints[slot];

                for (var directionIndex = 0; directionIndex < 4; directionIndex++)
                {
                    var pathPoint = pointsInSlot[directionIndex];
                    var connectionCount = pathNetwork.GetConnectionCount(pathPoint);

                    switch (connectionCount)
                    {
                        case 0:
                            continue;
                        case 1 when !GridConfiguration.IsEntityPoint(pathPoint) &&
                                    !CanBeConnectedByFutureTiles(slot, directionIndex, placedSlot) &&
                                    !CanBeConnectedByFutureTiles(slot, directionIndex, placedSlot):
                        case > 2:
                            return false;
                    }

                    // Entity points with > 1 connection are invalid
                    if (GridConfiguration.IsEntityPoint(pathPoint) && connectionCount > 1)
                        return false;
                }
            }

            return true;
        }

        private static bool CanBeConnectedByFutureTiles(int slot, int directionIndex, int currentSlot)
        {
            var neighborSlot = GetNeighborSlot(slot, directionIndex);

            if (neighborSlot != -1) return neighborSlot > currentSlot;
            var pathPoint = GridConfiguration.SlotToPathPoints[slot][directionIndex];
            return GridConfiguration.IsEntityPoint(pathPoint);
        }

        private static int GetNeighborSlot(int slot, int directionIndex)
        {
            var row = slot / 3;
            var col = slot % 3;

            return directionIndex switch
            {
                0 => // top
                    row > 0 ? slot - 3 : -1,
                1 => // right
                    col < 2 ? slot + 1 : -1,
                2 => // bottom
                    row < 2 ? slot + 3 : -1,
                3 => // left
                    col > 0 ? slot - 1 : -1,
                _ => -1
            };
        }

        private bool IsValidGameState(GridState gridState)
        {
            if (!gridState.IsFull)
                return false;

            var pathNetwork = _pathCalculator.CalculatePathNetwork(gridState);

            return _validator.IsValid(pathNetwork);
        }

        private static int ComputeGridHash(GridState grid)
        {
            unchecked
            {
                var hash = 17;
                for (var i = 0; i < GridState.TotalSlots; i++)
                {
                    var tile = grid.GetTile(i);
                    if (tile.HasValue)
                    {
                        hash = hash * 31 + (int)tile.Value.Type;
                        hash = hash * 31 + tile.Value.Rotation;
                    }
                    else
                    {
                        hash = hash * 31 + -1;
                    }
                }

                return hash;
            }
        }

        private string SaveToCsv(List<GridState> combinations)
        {
            var filePath = Path.Combine(Application.dataPath, "..", "Output", outputFileName);
            var directory = Path.GetDirectoryName(filePath);

            if (directory == null) throw new DirectoryNotFoundException(filePath);

            if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);

            using var writer = new StreamWriter(filePath, false, Encoding.UTF8);

            var header = new List<string> { "ID" };
            for (var i = 0; i < GridState.TotalSlots; i++)
            {
                header.Add($"Slot{i}_Type");
                header.Add($"Slot{i}_Rotation");
            }

            if (includeGridVisualization) header.Add("Visualization");
            writer.WriteLine(string.Join(",", header));

            for (var id = 0; id < combinations.Count; id++)
            {
                var grid = combinations[id];
                var row = new List<string> { id.ToString() };

                for (var slot = 0; slot < GridState.TotalSlots; slot++)
                {
                    var tile = grid.GetTile(slot);
                    if (tile.HasValue)
                    {
                        row.Add(tile.Value.Type.ToString());
                        row.Add(tile.Value.Rotation.ToString());
                    }
                    else
                    {
                        row.Add("Empty");
                        row.Add("0");
                    }
                }

                if (includeGridVisualization)
                {
                    var visualization = grid.ToPrettyString();
                    row.Add($"\"{visualization}\""); // Quote for multiline CSV field
                }

                writer.WriteLine(string.Join(",", row));
            }

            return filePath;
        }

        [Serializable]
        public class TileSetEntry
        {
            public TileType type;
            public int count = 1;
        }
    }
}