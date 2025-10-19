using Core.Logic;
using Core.Models;
using NUnit.Framework;

namespace Tests.Integration
{
    [TestFixture]
    public class FullGameFlowTests
    {
        [SetUp]
        public void SetUp()
        {
            _processor = new MoveProcessor();
            _calculator = new PathCalculator();
            _evaluator = new QuestEvaluator();
        }

        private MoveProcessor _processor;
        private PathCalculator _calculator;
        private QuestEvaluator _evaluator;

        [Test]
        public void CompleteGame_FromStartToWin_WorksCorrectly()
        {
            // Arrange - Set up a simple winnable game
            var grid = new GridState()
                .WithTile(0, new TileData(TileType.Curve, 3)) // Top-left
                .WithTile(1, new TileData(TileType.Intersection, 2)) // Top-center
                .WithTile(2, new TileData(TileType.Curve, 2)) // Top-right
                .FillEmptyWith(new TileData(TileType.Empty));

            var quest = new QuestData(new[] { new EntityGroup(new[] { 0, 1 }) });
            var paths = _calculator.CalculatePathNetwork(grid);
            var gameState = new GameState(grid, paths, quest);

            // Act - Perform moves to win
            var move1 = Move.Rotate(1, 3);
            var result1 = _processor.ProcessMove(gameState, move1);

            gameState = result1.NewState;

            // Assert - Game progresses correctly
            Assert.IsTrue(result1.IsValid, "Move should be valid");
            Assert.AreEqual(1, gameState.MoveCount, "Move count should increment");

            Assert.IsTrue(result1.IsLegalMove, "Move should create legal connections");

            var questResult = _evaluator.EvaluateQuest(gameState.Quest, gameState.Paths);
            Assert.IsTrue(questResult.IsSuccessful, "Quest should be successfully completed");

            // Should be valid but not win
            var move2 = Move.Rotate(1, 1);
            var result2 = _processor.ProcessMove(gameState, move2);

            gameState = result2.NewState;

            Assert.IsTrue(result2.IsValid, "Second move should be valid");
            Assert.AreEqual(2, gameState.MoveCount, "Move count should be 2 after second move");

            questResult = _evaluator.EvaluateQuest(gameState.Quest, gameState.Paths);
            Assert.IsFalse(questResult.IsSuccessful, "Quest should be now not completed");
        }

        [Test]
        public void MultipleMovesSequence_MaintainsConsistentState()
        {
            // Arrange
            var grid = new GridState()
                .WithTile(0, new TileData(TileType.Curve))
                .WithTile(4, new TileData(TileType.Intersection))
                .WithTile(8, new TileData(TileType.Bridge));

            var quest = new QuestData(new[] { new EntityGroup(new[] { 0, 1, 2 }) });
            var paths = _calculator.CalculatePathNetwork(grid);
            var gameState = new GameState(grid, paths, quest);

            // Act - Perform sequence of moves
            var move1 = Move.Rotate(0, 1);
            var result1 = _processor.ProcessMove(gameState, move1);

            var move2 = Move.Rotate(4, 1);
            var result2 = _processor.ProcessMove(result1.NewState, move2);

            var move3 = Move.Swap(0, 8);
            var result3 = _processor.ProcessMove(result2.NewState, move3);

            // Assert - State consistency
            Assert.IsTrue(result1.IsValid);
            Assert.IsTrue(result2.IsValid);
            Assert.IsTrue(result3.IsValid);

            Assert.AreEqual(1, result1.NewState.MoveCount);
            Assert.AreEqual(2, result2.NewState.MoveCount);
            Assert.AreEqual(3, result3.NewState.MoveCount);

            // Verify grid state is consistent
            Assert.AreEqual(3, result3.NewState.Grid.TileCount, "Should still have 3 tiles");
        }

        [Test]
        public void ComplexQuest_FullGame()
        {
            // Arrange - Set up a simple winnable game
            var grid = new GridState()
                .WithTile(0, new TileData(TileType.XIntersection))
                .WithTile(1, new TileData(TileType.Curve, 2))
                .WithTile(2, new TileData(TileType.Curve))
                .WithTile(3, new TileData(TileType.Intersection, 2))
                .WithTile(4, new TileData(TileType.Curve))
                .WithTile(5, new TileData(TileType.Intersection, 2))
                .WithTile(6, new TileData(TileType.Bridge))
                .WithTile(7, new TileData(TileType.TwoCurves))
                .WithTile(8, new TileData(TileType.TwoCurves, 1));

            var quest = new QuestData(new[] { new EntityGroup(new[] { 8, 0 }), new EntityGroup(new[] { 3, 9 }) },
                new[] { new DisconnectRequirement(0, 1) });
            var paths = _calculator.CalculatePathNetwork(grid);
            var gameState = new GameState(grid, paths, quest);

            // Act - Perform moves to win
            var move1 = Move.Rotate(8, 0);
            var result1 = _processor.ProcessMove(gameState, move1);

            gameState = result1.NewState;

            // Assert - Game progresses correctly
            Assert.IsTrue(result1.IsValid, "Move should be valid");
            Assert.AreEqual(1, gameState.MoveCount, "Move count should increment");

            Assert.IsTrue(result1.IsLegalMove, "Move should create legal connections");

            var questResult = _evaluator.EvaluateQuest(gameState.Quest, gameState.Paths);
            Assert.IsTrue(questResult.IsSuccessful, "Quest should be successfully completed");

            // Should be valid but not win
            var move2 = Move.Swap(0, 7);
            var result2 = _processor.ProcessMove(gameState, move2);

            gameState = result2.NewState;

            Assert.IsTrue(result2.IsValid, "Second move should be valid");
            Assert.AreEqual(2, gameState.MoveCount, "Move count should be 2 after second move");

            questResult = _evaluator.EvaluateQuest(gameState.Quest, gameState.Paths);
            Assert.IsFalse(questResult.IsSuccessful, "Quest should be now not completed");
        }

        [Test]
        public void UndoRedo_RestoredState_MatchesOriginal()
        {
            // Arrange
            var grid = new GridState()
                .WithTile(4, new TileData(TileType.Curve));

            var quest = new QuestData(new[] { new EntityGroup(new[] { 0, 1 }) });
            var paths = _calculator.CalculatePathNetwork(grid);
            var originalState = new GameState(grid, paths, quest);

            // Act - Make a move
            var move = Move.Rotate(4, 1);
            var result = _processor.ProcessMove(originalState, move);
            var modifiedState = result.NewState;

            // Simulate undo by keeping reference to original state
            var restoredState = originalState;

            // Assert - Original state unchanged (immutability)
            Assert.AreEqual(0, restoredState.MoveCount);
            Assert.AreEqual(1, modifiedState.MoveCount);

            var originalTile = restoredState.Grid.GetTile(4);
            var modifiedTile = modifiedState.Grid.GetTile(4);

            Assert.AreEqual(0, originalTile!.Value.Rotation);
            Assert.AreEqual(1, modifiedTile!.Value.Rotation);

            // Verify that we can "redo" by applying the same move again
            var redoResult = _processor.ProcessMove(restoredState, move);
            Assert.AreEqual(modifiedTile.Value.Rotation, redoResult.NewState.Grid.GetTile(4)!.Value.Rotation);
        }
    }
}