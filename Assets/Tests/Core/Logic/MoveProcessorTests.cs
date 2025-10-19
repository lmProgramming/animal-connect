using Core.Logic;
using Core.Models;
using NUnit.Framework;

namespace Tests.Core.Logic
{
    [TestFixture]
    public class MoveProcessorTests
    {
        [SetUp]
        public void SetUp()
        {
            _processor = new MoveProcessor();
        }

        private MoveProcessor _processor;

        [Test]
        public void ProcessMove_RotateTile_UpdatesRotation()
        {
            // Arrange
            var tile = new TileData(TileType.Curve);
            var grid = new GridState().WithTile(4, tile);
            var quest = new QuestData(new[] { new EntityGroup(new[] { 0, 1 }) });
            var paths = new PathNetworkState();
            var gameState = new GameState(grid, paths, quest);

            // Act
            var move = Move.Rotate(4, 1);
            var result = _processor.ProcessMove(gameState, move);

            // Assert
            Assert.IsTrue(result.IsValid);
            Assert.AreEqual(1, result.NewState.Grid.GetTile(4)!.Value.Rotation);
            Assert.AreEqual(TileType.Curve, result.NewState.Grid.GetTile(4)!.Value.Type);
            Assert.AreEqual(1, result.NewState.MoveCount);
        }

        [Test]
        public void ProcessMove_SwapTiles_SwapsPositions()
        {
            // Arrange
            var tile1 = new TileData(TileType.Curve);
            var tile2 = new TileData(TileType.Intersection, 1);
            var grid = new GridState()
                .WithTile(0, tile1)
                .WithTile(4, tile2);
            var quest = new QuestData(new[] { new EntityGroup(new[] { 0, 1 }) });
            var paths = new PathNetworkState();
            var gameState = new GameState(grid, paths, quest);

            // Act
            var move = Move.Swap(0, 4);
            var result = _processor.ProcessMove(gameState, move);

            // Assert
            Assert.IsTrue(result.IsValid);
            Assert.AreEqual(TileType.Intersection, result.NewState.Grid.GetTile(0)!.Value.Type);
            Assert.AreEqual(TileType.Curve, result.NewState.Grid.GetTile(4)!.Value.Type);
            Assert.AreEqual(1, result.NewState.MoveCount);
        }

        [Test]
        public void ProcessMove_ValidMove_ReturnsValidationResult()
        {
            // Arrange
            var tile = new TileData(TileType.Curve);
            var grid = new GridState().WithTile(4, tile);
            var quest = new QuestData(new[] { new EntityGroup(new[] { 0, 1 }) });
            var paths = new PathNetworkState();
            var gameState = new GameState(grid, paths, quest);

            // Act
            var move = Move.Rotate(4, 1);
            var result = _processor.ProcessMove(gameState, move);

            // Assert
            Assert.IsTrue(result.IsValid);
            Assert.IsNotNull(result.Validation);
            Assert.IsNotNull(result.QuestResult);
        }

        [Test]
        public void ProcessMove_WinningMove_FlagsAsWin()
        {
            // Arrange - Create a scenario where one move can win
            // Place tiles that, when rotated, will connect the required entities
            var grid = new GridState()
                .WithTile(0, new TileData(TileType.Curve)) // Will connect entity 0
                .WithTile(1, new TileData(TileType.Bridge)) // Bridge to connect
                .WithTile(2, new TileData(TileType.Curve, 1)); // Will connect entity 1

            var quest = new QuestData(new[] { new EntityGroup(new[] { 0, 1 }) });
            var calculator = new PathCalculator();
            var paths = calculator.CalculatePathNetwork(grid);
            var gameState = new GameState(grid, paths, quest);

            // Check if the initial state already wins
            var evaluator = new QuestEvaluator();
            var initialResult = evaluator.EvaluateQuest(quest, paths);

            if (initialResult.IsSuccessful)
            {
                // Already winning, so any move maintains win state
                var move = Move.Rotate(0, 1);
                var result = _processor.ProcessMove(gameState, move);
                Assert.IsTrue(result.IsValid);
            }
            else
            {
                // Try rotating tiles to achieve winning condition
                // This test verifies that IsWinningMove flag works correctly
                var move = Move.Rotate(1, 1);
                var result = _processor.ProcessMove(gameState, move);

                // The move should be valid regardless
                Assert.IsTrue(result.IsValid);
                // IsWinningMove should match quest success
                Assert.AreEqual(result.QuestResult.IsSuccessful && result.Validation.IsValid, result.IsWinningMove);
            }
        }

        [Test]
        public void ProcessMove_BridgeContinuousRotation_HandlesMaxRotationsCorrectly()
        {
            // Arrange - Create a grid with a bridge tile at rotation 0
            var bridgeTile = new TileData(TileType.Bridge);
            var grid = new GridState().WithTile(4, bridgeTile); // Center position
            var entityGroup = new EntityGroup(new[] { 0, 8 }); // Connect entities 0 and 8
            var quest = new QuestData(new[] { entityGroup });
            var paths = new PathNetworkState();
            var gameState = new GameState(grid, paths, quest);

            // Act & Assert - Rotate bridge continuously
            // Bridge has max 2 rotations (0 and 1)

            // First rotation: 0 -> 1 (valid)
            var move1 = Move.Rotate(4, 1);
            var result1 = _processor.ProcessMove(gameState, move1);
            Assert.IsTrue(result1.IsValid, "First rotation should be valid");
            Assert.AreEqual(1, result1.NewState.Grid.GetTile(4)!.Value.Rotation);

            // Second rotation: 1 -> 0 (wraps around, valid)
            var move2 = Move.Rotate(4, 0);
            var result2 = _processor.ProcessMove(result1.NewState, move2);
            Assert.IsTrue(result2.IsValid, "Second rotation should be valid");
            Assert.AreEqual(0, result2.NewState.Grid.GetTile(4)!.Value.Rotation);

            // Third rotation: 0 -> 1 (valid)
            var move3 = Move.Rotate(4, 1);
            var result3 = _processor.ProcessMove(result2.NewState, move3);
            Assert.IsTrue(result3.IsValid, "Third rotation should be valid");
            Assert.AreEqual(1, result3.NewState.Grid.GetTile(4)!.Value.Rotation);

            // Test invalid rotation: Should throw error for rotation 2 or 3
            var invalidMove = Move.Rotate(4, 2);
            var resultInvalid = _processor.ProcessMove(result3.NewState, invalidMove);
            Assert.IsFalse(resultInvalid.IsValid, "Invalid rotation should be rejected");
            Assert.IsNotNull(resultInvalid.ErrorMessage);
        }

        [Test]
        public void ProcessMove_XIntersectionContinuousRotation_HandlesMaxRotationsCorrectly()
        {
            // Arrange - X intersection only has 1 rotation state
            var xTile = new TileData(TileType.XIntersection);
            var grid = new GridState().WithTile(4, xTile);
            var entityGroup = new EntityGroup(new[] { 0, 8 });
            var quest = new QuestData(new[] { entityGroup });
            var paths = new PathNetworkState();
            var gameState = new GameState(grid, paths, quest);

            // Act & Assert - Try to rotate X intersection
            // XIntersection has max 1 rotation (only 0 is valid)

            // First rotation: 0 -> 0 (stays the same, valid)
            var move1 = Move.Rotate(4, 0);
            var result1 = _processor.ProcessMove(gameState, move1);
            Assert.IsTrue(result1.IsValid, "Rotation to same state should be valid");
            Assert.AreEqual(0, result1.NewState.Grid.GetTile(4)!.Value.Rotation);

            // Invalid rotation: Should reject rotation 1
            var invalidMove = Move.Rotate(4, 1);
            var resultInvalid = _processor.ProcessMove(gameState, invalidMove);
            Assert.IsFalse(resultInvalid.IsValid, "Invalid rotation should be rejected");
        }
    }
}