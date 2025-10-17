using System;
using System.Collections.Generic;
using System.Linq;
using Core.Logic;
using Core.Models;
using Managers;
using UnityEngine;
using UnityEngine.Serialization;
using Views;
using Random = UnityEngine.Random;

public class TilesSetup : MonoBehaviour
{
    [FormerlySerializedAs("_tileTypes")]
    [Header("Tile Configuration")]
    [SerializeField] private TileTypeEntry[] tileTypes;

    [FormerlySerializedAs("_sprites")]
    [Header("Sprites")]
    [SerializeField] private TileSprites sprites;

    [FormerlySerializedAs("_gridView")]
    [Header("Grid View")]
    [SerializeField] private GridView gridView;

    private void OnValidate()
    {
        // Auto-find GridView if not assigned
        if (gridView == null) gridView = FindFirstObjectByType<GridView>();

        // Validate tile count
        if (tileTypes != null)
        {
            var totalTiles = tileTypes.Sum(t => t.count);
            if (totalTiles != 9)
                Debug.LogWarning($"TilesSetup: Total tile count is {totalTiles}, should be 9 for a 3x3 grid!");
        }
    }

    public void Setup(GameStateManager stateManager, QuestData questData)
    {
        if (stateManager == null)
        {
            Debug.LogError("TilesSetup: GameStateManager is null!");
            return;
        }

        const int maxAttempts = 100;
        var attempts = 0;
        GridState validGrid;

        Debug.Log("TilesSetup: Generating puzzle configuration...");

        do
        {
            attempts++;
            validGrid = GenerateRandomGridState();

            // Check if this is a winning configuration
            if (!IsWinningConfiguration(validGrid, questData))
                // Found a valid non-winning configuration
                break;

            if (attempts >= maxAttempts)
            {
                Debug.LogWarning(
                    $"TilesSetup: Reached maximum attempts ({maxAttempts}). Using current setup.");
                break;
            }
        } while (true);

        Debug.Log($"TilesSetup: Generated puzzle in {attempts} attempt(s)");

        Debug.Log("TilesSetup: About to call stateManager.Initialize...");
        // Initialize the game state with the generated grid
        stateManager.Initialize(questData, validGrid);
        Debug.Log("TilesSetup: stateManager.Initialize completed!");

        // Update the grid view
        if (gridView != null) gridView.UpdateFromState(stateManager.CurrentState);
    }

    private GridState GenerateRandomGridState()
    {
        var gridState = new GridState();

        // Create list of tiles based on configuration
        var tilesToPlace = new List<TileData>();

        foreach (var entry in tileTypes)
            for (var i = 0; i < entry.count; i++)
            {
                var maxRotations = GetMaxRotations(entry.type);
                var rotation = Random.Range(0, maxRotations);

                tilesToPlace.Add(new TileData(entry.type, rotation));
            }

        // Shuffle tiles
        tilesToPlace = tilesToPlace.OrderBy(_ => Random.value).ToList();

        // Assign to grid positions
        for (var i = 0; i < Mathf.Min(tilesToPlace.Count, 9); i++) gridState = gridState.WithTile(i, tilesToPlace[i]);

        return gridState;
    }

    private bool IsWinningConfiguration(GridState gridState, QuestData questData)
    {
        // Calculate path network
        var pathCalculator = new PathCalculator();
        var pathNetwork = pathCalculator.CalculatePathNetwork(gridState);

        // Check if all tiles are placed (full grid is a requirement for winning)
        var allSlotsOccupied = true;
        for (var i = 0; i < 9; i++)
            if (gridState.GetTile(i) == null)
            {
                allSlotsOccupied = false;
                break;
            }

        if (!allSlotsOccupied) return false; // Can't win with empty slots

        // Validate connections
        var validator = new ConnectionValidator();
        var validationResult = validator.ValidateConnections(pathNetwork);

        if (!validationResult.IsValid) return false; // Invalid paths, so not winning

        // Check quest completion
        var questEvaluator = new QuestEvaluator();
        var questResult = questEvaluator.EvaluateQuest(questData, pathNetwork);

        return questResult.IsComplete;
    }

    private int GetMaxRotations(TileType type)
    {
        return type switch
        {
            TileType.TwoCurves => 2,
            TileType.XIntersection => 1,
            TileType.Bridge => 1,
            _ => 4
        };
    }

    [Serializable]
    public class TileTypeEntry
    {
        public TileType type;
        public int count = 1;
    }
}