using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using AnimalConnect.Managers;
using AnimalConnect.Views;
using Core.Models;
using Core.Logic;

public class TilesSetup : MonoBehaviour
{
    [Header("Tile Configuration")]
    [SerializeField] private TileTypeEntry[] _tileTypes;

    [Header("Sprites")]
    [SerializeField] private TileSprites _sprites;
    
    [Header("Grid View")]
    [SerializeField] private GridView _gridView;

    [System.Serializable]
    public class TileTypeEntry
    {
        public TileType type;
        public int count = 1;
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
        GridState validGrid = null;
        
        Debug.Log("TilesSetup: Generating puzzle configuration...");

        do
        {
            attempts++;
            validGrid = GenerateRandomGridState();
            
            // Check if this is a winning configuration
            if (!IsWinningConfiguration(validGrid, questData))
            {
                // Found a valid non-winning configuration
                break;
            }

            if (attempts >= maxAttempts)
            {
                Debug.LogWarning(
                    $"TilesSetup: Reached maximum attempts ({maxAttempts}). Using current setup.");
                break;
            }
        } while (true);

        Debug.Log($"TilesSetup: Generated puzzle in {attempts} attempt(s)");

        // Initialize the game state with the generated grid
        stateManager.Initialize(questData, validGrid);
        
        // Update the grid view
        if (_gridView != null)
        {
            _gridView.UpdateFromState(stateManager.CurrentState);
        }
    }

    private GridState GenerateRandomGridState()
    {
        var gridState = new GridState();
        
        // Create list of tiles based on configuration
        var tilesToPlace = new List<TileData>();
        
        foreach (var entry in _tileTypes)
        {
            for (int i = 0; i < entry.count; i++)
            {
                int maxRotations = GetMaxRotations(entry.type);
                int rotation = Random.Range(0, maxRotations);
                
                tilesToPlace.Add(new TileData(entry.type, rotation));
            }
        }
        
        // Shuffle tiles
        tilesToPlace = tilesToPlace.OrderBy(x => Random.value).ToList();
        
        // Assign to grid positions
        for (int i = 0; i < Mathf.Min(tilesToPlace.Count, 9); i++)
        {
            gridState = gridState.WithTile(i, tilesToPlace[i]);
        }
        
        return gridState;
    }

    private bool IsWinningConfiguration(GridState gridState, QuestData questData)
    {
        // Calculate path network
        var pathCalculator = new PathCalculator();
        var pathNetwork = pathCalculator.CalculatePathNetwork(gridState);
        
        // Check if all tiles are placed (full grid is a requirement for winning)
        bool allSlotsOccupied = true;
        for (int i = 0; i < 9; i++)
        {
            if (gridState.GetTile(i) == null)
            {
                allSlotsOccupied = false;
                break;
            }
        }
        
        if (!allSlotsOccupied)
        {
            return false; // Can't win with empty slots
        }
        
        // Validate connections
        var validator = new ConnectionValidator();
        var validationResult = validator.ValidateConnections(pathNetwork);
        
        if (!validationResult.IsValid)
        {
            return false; // Invalid paths, so not winning
        }
        
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

    private void OnValidate()
    {
        // Auto-find GridView if not assigned
        if (_gridView == null)
        {
            _gridView = FindFirstObjectByType<GridView>();
        }
        
        // Validate tile count
        if (_tileTypes != null)
        {
            int totalTiles = _tileTypes.Sum(t => t.count);
            if (totalTiles != 9)
            {
                Debug.LogWarning($"TilesSetup: Total tile count is {totalTiles}, should be 9 for a 3x3 grid!");
            }
        }
    }
}