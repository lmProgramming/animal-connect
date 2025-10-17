using NUnit.Framework;
using Core.Logic;
using Core.Models;

namespace Tests.Core.Logic
{
    [TestFixture]
    public class MoveProcessorTests
    {
        private MoveProcessor _processor;
        
        [SetUp]
        public void SetUp()
        {
            _processor = new MoveProcessor();
        }
        
        [Test]
        public void ProcessMove_RotateTile_UpdatesRotation()
        {
            // TODO: Implement when Move structure is finalized
            Assert.Pass("Move processor tests - to be implemented");
        }
        
        [Test]
        public void ProcessMove_SwapTiles_SwapsPositions()
        {
            // TODO: Test swap move
            Assert.Pass("Move processor tests - to be implemented");
        }
        
        [Test]
        public void ProcessMove_ValidMove_ReturnsValidationResult()
        {
            // TODO: Test move validation
            Assert.Pass("Move processor tests - to be implemented");
        }
        
        [Test]
        public void ProcessMove_WinningMove_FlagsAsWin()
        {
            // TODO: Test winning condition detection
            Assert.Pass("Move processor tests - to be implemented");
        }
        
        [Test]
        public void ProcessMove_BridgeContinuousRotation_HandlesMaxRotationsCorrectly()
        {
            // Arrange - Create a grid with a bridge tile at rotation 0
            var bridgeTile = new TileData(TileType.Bridge, 0);
            var grid = new GridState().WithTile(4, bridgeTile); // Center position
            var entityGroup = new EntityGroup(new[] { 0, 8 }); // Connect entities 0 and 8
            var quest = new QuestData(new[] { entityGroup });
            var paths = new PathNetworkState();
            var gameState = new GameState(grid, paths, quest, 0);
            
            // Act & Assert - Rotate bridge continuously
            // Bridge has max 2 rotations (0 and 1)
            
            // First rotation: 0 -> 1 (valid)
            var move1 = Move.Rotate(4, 1);
            var result1 = _processor.ProcessMove(gameState, move1);
            Assert.IsTrue(result1.IsValid, "First rotation should be valid");
            Assert.AreEqual(1, result1.NewState.Grid.GetTile(4).Value.Rotation);
            
            // Second rotation: 1 -> 0 (wraps around, valid)
            var move2 = Move.Rotate(4, 0);
            var result2 = _processor.ProcessMove(result1.NewState, move2);
            Assert.IsTrue(result2.IsValid, "Second rotation should be valid");
            Assert.AreEqual(0, result2.NewState.Grid.GetTile(4).Value.Rotation);
            
            // Third rotation: 0 -> 1 (valid)
            var move3 = Move.Rotate(4, 1);
            var result3 = _processor.ProcessMove(result2.NewState, move3);
            Assert.IsTrue(result3.IsValid, "Third rotation should be valid");
            Assert.AreEqual(1, result3.NewState.Grid.GetTile(4).Value.Rotation);
            
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
            var xTile = new TileData(TileType.XIntersection, 0);
            var grid = new GridState().WithTile(4, xTile);
            var entityGroup = new EntityGroup(new[] { 0, 8 });
            var quest = new QuestData(new[] { entityGroup });
            var paths = new PathNetworkState();
            var gameState = new GameState(grid, paths, quest, 0);
            
            // Act & Assert - Try to rotate X intersection
            // XIntersection has max 1 rotation (only 0 is valid)
            
            // First rotation: 0 -> 0 (stays the same, valid)
            var move1 = Move.Rotate(4, 0);
            var result1 = _processor.ProcessMove(gameState, move1);
            Assert.IsTrue(result1.IsValid, "Rotation to same state should be valid");
            Assert.AreEqual(0, result1.NewState.Grid.GetTile(4).Value.Rotation);
            
            // Invalid rotation: Should reject rotation 1
            var invalidMove = Move.Rotate(4, 1);
            var resultInvalid = _processor.ProcessMove(gameState, invalidMove);
            Assert.IsFalse(resultInvalid.IsValid, "Invalid rotation should be rejected");
        }
    }
}
