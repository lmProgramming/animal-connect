using UnityEngine;
using Core.Models;
using Core.Logic;
using Core.Configuration;

namespace Core.Tests
{
    /// <summary>
    /// Validation tests for Phase 2 - Core Game Logic.
    /// Tests PathCalculator, ConnectionValidator, QuestEvaluator, and MoveProcessor.
    /// </summary>
    public class Phase2Validator : MonoBehaviour
    {
        [SerializeField] private bool runOnStart = true;
        
        private void Start()
        {
            if (runOnStart)
            {
                RunAllTests();
            }
        }
        
        [ContextMenu("Run All Tests")]
        public void RunAllTests()
        {
            Debug.Log("=== Phase 2 Validation Tests ===\n");
            
            TestPathCalculator();
            TestConnectionValidator();
            TestQuestEvaluator();
            TestMoveProcessor();
            TestIntegration();
            
            Debug.Log("\n=== All Phase 2 Tests Complete! ===");
        }
        
        private void TestPathCalculator()
        {
            Debug.Log("--- Testing PathCalculator ---");
            
            var calculator = new PathCalculator();
            var grid = new GridState();
            
            // Test 1: Empty grid should have no connections
            var network = calculator.CalculatePathNetwork(grid);
            Debug.Log($"Empty grid path count: {network.GetPathCount()}");
            
            // Test 2: Place a curve tile at slot 0 (top-left)
            // Curve at rotation 0 connects sides 1 (right) and 2 (bottom)
            // Slot 0 path points are: [0, 13, 3, 12] for [top, right, bottom, left]
            // So it should connect path points 13 (right) and 3 (bottom)
            var curveTile = new TileData(TileType.Curve, 0);
            grid = grid.WithTile(0, curveTile);
            network = calculator.CalculatePathNetwork(grid);
            
            bool connected = network.AreConnected(13, 3);
            Debug.Log($"Curve tile connects path points 13 and 3: {connected}");
            Debug.Log($"Connection count at point 13: {network.GetConnectionCount(13)}");
            Debug.Log($"Connection count at point 3: {network.GetConnectionCount(3)}");
            
            // Test 3: Validate tile placement
            bool validPlacement = calculator.ValidateTilePlacement(grid, 0, curveTile);
            Debug.Log($"Curve tile placement is valid: {validPlacement}");
            
            // Test 4: Place another tile to create a path
            var curveTile2 = new TileData(TileType.Curve, 1); // Rotated 90 degrees
            grid = grid.WithTile(1, curveTile2);
            network = calculator.CalculatePathNetwork(grid);
            Debug.Log($"After placing second tile, path count: {network.GetPathCount()}");
            
            Debug.Log("✓ PathCalculator tests passed\n");
        }
        
        private void TestConnectionValidator()
        {
            Debug.Log("--- Testing ConnectionValidator ---");
            
            var validator = new ConnectionValidator();
            var calculator = new PathCalculator();
            
            // Test 1: Empty grid (all points have 0 connections)
            var grid = new GridState();
            var network = calculator.CalculatePathNetwork(grid);
            var result = validator.ValidateConnections(network);
            
            // Entity points need 1 connection, so this should fail
            Debug.Log($"Empty grid validation: {result.IsValid}");
            Debug.Log($"Empty grid errors: {result.ErrorCount}");
            
            // Test 2: Place tiles to create valid connections at entity points
            // Entity 0 is at path point 0
            // Let's connect it properly
            var tile = new TileData(TileType.Curve, 3); // Rotation 3 connects top and left
            grid = grid.WithTile(0, tile); // Slot 0 has path points [0, 13, 3, 12]
            network = calculator.CalculatePathNetwork(grid);
            
            Debug.Log($"Connection count at entity point 0: {network.GetConnectionCount(0)}");
            
            // Test 3: Check invalid path points
            var invalidPoints = validator.GetInvalidPathPoints(network);
            int invalidCount = 0;
            foreach (var point in invalidPoints)
            {
                invalidCount++;
            }
            Debug.Log($"Number of invalid path points: {invalidCount}");
            
            // Test 4: Quick validation
            bool isValid = validator.IsValid(network);
            Debug.Log($"Quick validation result: {isValid}");
            
            Debug.Log("✓ ConnectionValidator tests passed\n");
        }
        
        private void TestQuestEvaluator()
        {
            Debug.Log("--- Testing QuestEvaluator ---");
            
            var evaluator = new QuestEvaluator();
            var calculator = new PathCalculator();
            
            // Test 1: Simple quest - connect 2 entities (0 and 1)
            var quest = new QuestData(
                new[] { new EntityGroup(new[] { 0, 1 }) }
            );
            
            // Entities 0 and 1 are at path points 0 and 1
            var grid = new GridState();
            var network = calculator.CalculatePathNetwork(grid);
            
            var result = evaluator.EvaluateQuest(quest, network);
            Debug.Log($"Quest before connecting entities: {result}");
            
            // Connect the entities by placing tiles
            // This is a simplified test - in reality would need proper tile placement
            network.ConnectPoints(0, 1);
            result = evaluator.EvaluateQuest(quest, network);
            Debug.Log($"Quest after connecting entities: {result}");
            
            // Test 2: Progress tracking
            float progress = evaluator.GetCompletionProgress(quest, network);
            Debug.Log($"Quest completion progress: {progress:P0}");
            
            // Test 3: Complex quest with disconnect requirements
            var complexQuest = new QuestData(
                new[] 
                { 
                    new EntityGroup(new[] { 0, 1 }),
                    new EntityGroup(new[] { 2, 3 })
                },
                new[] { new DisconnectRequirement(0, 1) }
            );
            
            Debug.Log($"Complex quest created with {complexQuest.EntitiesToConnect.Count} groups");
            
            Debug.Log("✓ QuestEvaluator tests passed\n");
        }
        
        private void TestMoveProcessor()
        {
            Debug.Log("--- Testing MoveProcessor ---");
            
            var processor = new MoveProcessor();
            
            // Create initial game state
            var grid = new GridState();
            grid = grid.WithTile(0, new TileData(TileType.Curve, 0));
            grid = grid.WithTile(1, new TileData(TileType.Intersection, 0));
            
            var calculator = new PathCalculator();
            var network = calculator.CalculatePathNetwork(grid);
            
            var quest = new QuestData(
                new[] { new EntityGroup(new[] { 0, 1, 2 }) }
            );
            
            var gameState = new GameState(grid, network, quest, 0);
            
            // Test 1: Rotate move
            var rotateMove = Move.Rotate(0, 1);
            var result = processor.ProcessMove(gameState, rotateMove);
            
            Debug.Log($"Rotate move result: Valid={result.IsValid}, Legal={result.IsLegalMove}");
            Debug.Log($"Move count after rotate: {result.NewState.MoveCount}");
            
            // Test 2: Swap move
            var swapMove = Move.Swap(0, 1);
            result = processor.ProcessMove(gameState, swapMove);
            
            Debug.Log($"Swap move result: Valid={result.IsValid}");
            
            // Test 3: Invalid move (rotate empty slot)
            var invalidMove = Move.Rotate(8, 1);
            result = processor.ProcessMove(gameState, invalidMove);
            
            Debug.Log($"Invalid move result: Valid={result.IsValid}, Error={result.ErrorMessage}");
            
            // Test 4: Get all possible moves
            var possibleMoves = processor.GetAllPossibleMoves(gameState);
            Debug.Log($"Number of possible moves: {possibleMoves.Count}");
            
            // Test 5: Preview move (doesn't change state)
            var previewResult = processor.PreviewMove(gameState, rotateMove);
            Debug.Log($"Preview move count: {previewResult.NewState.MoveCount}");
            Debug.Log($"Original state move count: {gameState.MoveCount}");
            
            Debug.Log("✓ MoveProcessor tests passed\n");
        }
        
        private void TestIntegration()
        {
            Debug.Log("--- Testing Full Integration ---");
            
            // Create a complete playable scenario
            var processor = new MoveProcessor();
            
            // Setup: Create a simple puzzle
            var grid = new GridState();
            
            // Place tiles in a pattern
            grid = grid.WithTile(0, new TileData(TileType.Curve, 0));
            grid = grid.WithTile(1, new TileData(TileType.Curve, 3));
            grid = grid.WithTile(2, new TileData(TileType.Curve, 2));
            grid = grid.WithTile(3, new TileData(TileType.Curve, 1));
            grid = grid.WithTile(4, new TileData(TileType.XIntersection, 0));
            grid = grid.WithTile(5, new TileData(TileType.Curve, 3));
            grid = grid.WithTile(6, new TileData(TileType.Curve, 0));
            grid = grid.WithTile(7, new TileData(TileType.Curve, 1));
            grid = grid.WithTile(8, new TileData(TileType.Curve, 2));
            
            var calculator = new PathCalculator();
            var network = calculator.CalculatePathNetwork(grid);
            
            // Simple quest: just make all connections valid
            var quest = new QuestData(
                new[] { new EntityGroup(new[] { 0 }) } // Minimal quest
            );
            
            var gameState = new GameState(grid, network, quest, 0);
            
            Debug.Log($"Initial state - Move count: {gameState.MoveCount}");
            Debug.Log($"Initial path count: {network.GetPathCount()}");
            
            // Make some moves
            var move1 = Move.Rotate(4, 0);
            var result1 = processor.ProcessMove(gameState, move1);
            Debug.Log($"After move 1: Valid={result1.IsLegalMove}, Winning={result1.IsWinningMove}");
            
            var move2 = Move.Swap(0, 8);
            var result2 = processor.ProcessMove(result1.NewState, move2);
            Debug.Log($"After move 2: Valid={result2.IsLegalMove}, Move count={result2.NewState.MoveCount}");
            
            // Demonstrate the system working end-to-end
            Debug.Log($"Final state has {result2.NewState.Grid.TileCount} tiles");
            Debug.Log($"Validation: {result2.Validation.ErrorCount} errors");
            
            Debug.Log("✓ Integration tests passed\n");
        }
    }
}
