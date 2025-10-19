using Core.Logic;
using Core.Models;
using NUnit.Framework;

namespace Tests.Core.Logic
{
    [TestFixture]
    public class QuestEvaluatorTests
    {
        [SetUp]
        public void SetUp()
        {
            _evaluator = new QuestEvaluator();
        }

        private QuestEvaluator _evaluator;

        [Test]
        public void EvaluateQuest_EmptyQuest_ReturnsSuccess()
        {
            // Arrange
            var emptyQuest = new QuestData(new EntityGroup[] { });
            var network = new PathNetworkState();

            // Act
            var result = _evaluator.EvaluateQuest(emptyQuest, network);

            // Assert
            Assert.IsTrue(result.IsSuccessful, "Empty quest should be automatically successful");
            Assert.IsTrue(result.IsComplete);
        }

        [Test]
        public void EvaluateQuest_EntitiesConnected_ReturnsSuccess()
        {
            // Arrange
            var quest = new QuestData(new[] { new EntityGroup(new[] { 0, 1, 2 }) });
            var network = new PathNetworkState();

            // Connect entities 0, 1, and 2 by connecting their path points
            // Entity 0 -> path point 0, Entity 1 -> path point 8, Entity 2 -> path point 16
            network.ConnectPoints(0, 13);
            network.ConnectPoints(13, 1);
            network.ConnectPoints(13, 14);
            network.ConnectPoints(14, 2);

            // Act
            var result = _evaluator.EvaluateQuest(quest, network);

            // Assert
            Assert.IsTrue(result.IsSuccessful, "Connected entities should satisfy quest");
            Assert.IsTrue(result.IsComplete);
        }

        [Test]
        public void EvaluateQuest_EntitiesNotConnected_ReturnsIncomplete()
        {
            // Arrange
            var quest = new QuestData(new[] { new EntityGroup(new[] { 0, 1, 2 }) });
            var network = new PathNetworkState();

            // Only connect entities 0 and 1, but not 2
            network.ConnectPoints(0, 8); // Connect entity 0 to entity 1
            // Entity 2 (path point 16) is not connected

            // Act
            var result = _evaluator.EvaluateQuest(quest, network);

            // Assert
            Assert.IsFalse(result.IsSuccessful, "Partially connected entities should not satisfy quest");
            Assert.IsFalse(result.IsComplete);
        }

        [Test]
        public void EvaluateQuest_PathsShouldBeDisconnected_ReturnsCorrectResult()
        {
            // Arrange - Two groups that must be connected internally but NOT to each other
            var quest = new QuestData(
                new[]
                {
                    new EntityGroup(new[] { 0, 1 }), // Group 0
                    new EntityGroup(new[] { 2, 3 }) // Group 1
                },
                new[] { new DisconnectRequirement(0, 1) } // Groups must NOT connect
            );
            var network = new PathNetworkState();

            // Connect entities within each group
            network.ConnectPoints(0, 13);
            network.ConnectPoints(13, 1);
            network.ConnectPoints(2, 15); // Connect entity 2 to entity 3 (group 1)
            // Do NOT connect the two groups

            // Act
            var result = _evaluator.EvaluateQuest(quest, network);

            // Assert
            Assert.IsTrue(result.IsSuccessful, "Disconnected groups should satisfy disconnect requirement");

            // Now test failure case - connect the groups
            network.ConnectPoints(13, 14);
            network.ConnectPoints(2, 14);
            var failResult = _evaluator.EvaluateQuest(quest, network);
            Assert.IsFalse(failResult.IsSuccessful, "Connected groups should fail disconnect requirement");
        }
    }
}