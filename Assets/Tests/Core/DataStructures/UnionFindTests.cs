using System;
using Core.DataStructures;
using NUnit.Framework;

namespace Tests.Core.DataStructures
{
    [TestFixture]
    public class UnionFindTests
    {
        [SetUp]
        public void SetUp()
        {
            _unionFind = new UnionFind(10);
        }

        private UnionFind _unionFind;

        [Test]
        public void Constructor_InitializesAllElementsAsSeparateSets()
        {
            // Arrange & Act
            var uf = new UnionFind(5);

            // Assert
            Assert.AreEqual(5, uf.CountSets());
            for (var i = 0; i < 5; i++) Assert.AreEqual(i, uf.Find(i), $"Element {i} should be its own root");
        }

        [Test]
        public void Find_ReturnsElementItself_WhenNotConnected()
        {
            // Assert
            Assert.AreEqual(0, _unionFind.Find(0));
            Assert.AreEqual(5, _unionFind.Find(5));
            Assert.AreEqual(9, _unionFind.Find(9));
        }

        [Test]
        public void Find_ThrowsException_WhenElementOutOfRange()
        {
            // Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => _unionFind.Find(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => _unionFind.Find(10));
            Assert.Throws<ArgumentOutOfRangeException>(() => _unionFind.Find(100));
        }

        [Test]
        public void Union_ConnectsTwoElements()
        {
            // Act
            var merged = _unionFind.Union(0, 1);

            // Assert
            Assert.IsTrue(merged, "Union should return true when merging different sets");
            Assert.IsTrue(_unionFind.Connected(0, 1));
            Assert.AreEqual(_unionFind.Find(0), _unionFind.Find(1));
        }

        [Test]
        public void Union_ReturnsFalse_WhenElementsAlreadyConnected()
        {
            // Arrange
            _unionFind.Union(0, 1);

            // Act
            var merged = _unionFind.Union(0, 1);

            // Assert
            Assert.IsFalse(merged, "Union should return false when elements already in same set");
        }

        [Test]
        public void Union_TransitiveProperty_ConnectsAllElements()
        {
            // Act
            _unionFind.Union(0, 1);
            _unionFind.Union(1, 2);
            _unionFind.Union(2, 3);

            // Assert - All should be connected transitively
            Assert.IsTrue(_unionFind.Connected(0, 1));
            Assert.IsTrue(_unionFind.Connected(0, 2));
            Assert.IsTrue(_unionFind.Connected(0, 3));
            Assert.IsTrue(_unionFind.Connected(1, 2));
            Assert.IsTrue(_unionFind.Connected(1, 3));
            Assert.IsTrue(_unionFind.Connected(2, 3));
        }

        [Test]
        public void Union_MultipleChains_KeepsSetsIndependent()
        {
            // Act - Create two separate chains
            _unionFind.Union(0, 1);
            _unionFind.Union(1, 2);

            _unionFind.Union(5, 6);
            _unionFind.Union(6, 7);

            // Assert - Elements within chain are connected
            Assert.IsTrue(_unionFind.Connected(0, 2));
            Assert.IsTrue(_unionFind.Connected(5, 7));

            // Assert - Elements between chains are not connected
            Assert.IsFalse(_unionFind.Connected(0, 5));
            Assert.IsFalse(_unionFind.Connected(2, 7));
        }

        [Test]
        public void Union_AllElements_CreatesOneSet()
        {
            // Act - Connect all elements in a chain
            for (var i = 0; i < 9; i++) _unionFind.Union(i, i + 1);

            // Assert
            Assert.AreEqual(1, _unionFind.CountSets());
            for (var i = 0; i < 10; i++)
            for (var j = 0; j < 10; j++)
                Assert.IsTrue(_unionFind.Connected(i, j),
                    $"Elements {i} and {j} should be connected");
        }

        [Test]
        public void Connected_ReturnsFalse_ForUnconnectedElements()
        {
            // Assert
            Assert.IsFalse(_unionFind.Connected(0, 1));
            Assert.IsFalse(_unionFind.Connected(3, 7));
        }

        [Test]
        public void Connected_ReturnsTrue_ForSameElement()
        {
            // Assert
            Assert.IsTrue(_unionFind.Connected(0, 0));
            Assert.IsTrue(_unionFind.Connected(5, 5));
        }

        [Test]
        public void Connected_ReturnsTrue_AfterUnion()
        {
            // Arrange
            _unionFind.Union(2, 5);

            // Assert
            Assert.IsTrue(_unionFind.Connected(2, 5));
            Assert.IsTrue(_unionFind.Connected(5, 2)); // Symmetric
        }

        [Test]
        public void CountSets_ReturnsCorrectCount_AfterMultipleUnions()
        {
            // Initial: 10 separate sets
            Assert.AreEqual(10, _unionFind.CountSets());

            // Connect 0-1-2 (3 elements -> 1 set)
            _unionFind.Union(0, 1);
            Assert.AreEqual(9, _unionFind.CountSets());

            _unionFind.Union(1, 2);
            Assert.AreEqual(8, _unionFind.CountSets());

            // Connect 5-6 (2 elements -> 1 set)
            _unionFind.Union(5, 6);
            Assert.AreEqual(7, _unionFind.CountSets());
        }

        [Test]
        public void CountSets_DoesNotChange_WhenUnioningAlreadyConnectedElements()
        {
            // Arrange
            _unionFind.Union(0, 1);
            var countBefore = _unionFind.CountSets();

            // Act
            _unionFind.Union(0, 1);

            // Assert
            Assert.AreEqual(countBefore, _unionFind.CountSets());
        }

        [Test]
        public void GetSet_ReturnsSingleElement_WhenNotConnected()
        {
            // Act
            var set = _unionFind.GetSet(5);

            // Assert
            Assert.AreEqual(1, set.Length);
            Assert.AreEqual(5, set[0]);
        }

        [Test]
        public void GetSet_ReturnsAllConnectedElements()
        {
            // Arrange
            _unionFind.Union(1, 3);
            _unionFind.Union(3, 5);
            _unionFind.Union(5, 7);

            // Act
            var set = _unionFind.GetSet(1);

            // Assert
            Assert.AreEqual(4, set.Length);
            CollectionAssert.Contains(set, 1);
            CollectionAssert.Contains(set, 3);
            CollectionAssert.Contains(set, 5);
            CollectionAssert.Contains(set, 7);
        }

        [Test]
        public void GetSet_ReturnsSameSet_ForAnyElementInSet()
        {
            // Arrange
            _unionFind.Union(0, 2);
            _unionFind.Union(2, 4);

            // Act
            var set0 = _unionFind.GetSet(0);
            var set2 = _unionFind.GetSet(2);
            var set4 = _unionFind.GetSet(4);

            // Assert
            CollectionAssert.AreEquivalent(set0, set2);
            CollectionAssert.AreEquivalent(set0, set4);
        }

        [Test]
        public void Reset_SeparatesAllElements()
        {
            // Arrange - Connect some elements
            _unionFind.Union(0, 1);
            _unionFind.Union(2, 3);
            _unionFind.Union(1, 2); // Now 0,1,2,3 are all connected

            // Act
            _unionFind.Reset();

            // Assert
            Assert.AreEqual(10, _unionFind.CountSets());
            Assert.IsFalse(_unionFind.Connected(0, 1));
            Assert.IsFalse(_unionFind.Connected(2, 3));
            Assert.IsFalse(_unionFind.Connected(1, 2));
        }

        [Test]
        public void Reset_AllowsNewUnions()
        {
            // Arrange
            _unionFind.Union(0, 5);
            _unionFind.Reset();

            // Act
            _unionFind.Union(0, 9);

            // Assert
            Assert.IsTrue(_unionFind.Connected(0, 9));
            Assert.IsFalse(_unionFind.Connected(0, 5));
        }

        [Test]
        public void PathCompression_OptimizesFindOperations()
        {
            // Arrange - Create a long chain: 0->1->2->3->4->5
            for (var i = 0; i < 5; i++) _unionFind.Union(i, i + 1);

            // Act - First Find on element 0 should compress the path
            var root1 = _unionFind.Find(0);

            // Second Find should be faster (but we can't measure that directly)
            var root2 = _unionFind.Find(0);

            // Assert - Both should return the same root
            Assert.AreEqual(root1, root2);

            // All elements in the chain should have the same root
            for (var i = 0; i <= 5; i++) Assert.AreEqual(root1, _unionFind.Find(i));
        }

        [Test]
        public void GameScenario_24PathPoints_SingleTileConnection()
        {
            // Arrange - Simulate 24 path points (as in actual game)
            var uf = new UnionFind(24);

            // Act - A curve tile connects two adjacent points (e.g., top and right)
            uf.Union(0, 1);

            // Assert
            Assert.IsTrue(uf.Connected(0, 1));
            Assert.AreEqual(23, uf.CountSets()); // 24 - 1 merged pair
        }

        [Test]
        public void GameScenario_MultipleIntersectingPaths()
        {
            // Arrange - Simulate path network
            var uf = new UnionFind(24);

            // Act - Simulate multiple tile connections
            // Path 1: points 0, 1, 2, 3
            uf.Union(0, 1);
            uf.Union(1, 2);
            uf.Union(2, 3);

            // Path 2: points 10, 11, 12
            uf.Union(10, 11);
            uf.Union(11, 12);

            // Path 3: points 20, 21
            uf.Union(20, 21);

            // Assert
            Assert.IsTrue(uf.Connected(0, 3), "Path 1 should be connected");
            Assert.IsTrue(uf.Connected(10, 12), "Path 2 should be connected");
            Assert.IsTrue(uf.Connected(20, 21), "Path 3 should be connected");

            Assert.IsFalse(uf.Connected(0, 10), "Different paths should not be connected");
            Assert.IsFalse(uf.Connected(10, 20), "Different paths should not be connected");

            // Should have 3 main paths + remaining isolated points
            // 4 points in path1, 3 in path2, 2 in path3 = 9 connected
            // 24 - 9 = 15 isolated + 3 paths = 18 sets
            Assert.AreEqual(18, uf.CountSets());
        }

        [Test]
        public void GameScenario_BridgeTileConnectsThreePaths()
        {
            // Arrange
            var uf = new UnionFind(24);

            // Create 3 separate paths
            uf.Union(0, 1); // Path A
            uf.Union(5, 6); // Path B
            uf.Union(10, 11); // Path C

            var setsBefore = uf.CountSets();

            // Act - Bridge tile connects all three paths
            uf.Union(1, 6); // Connect A to B
            uf.Union(6, 11); // Connect B to C (now all are connected)

            // Assert
            Assert.IsTrue(uf.Connected(0, 5), "Path A and B should be connected");
            Assert.IsTrue(uf.Connected(5, 10), "Path B and C should be connected");
            Assert.IsTrue(uf.Connected(0, 10), "Path A and C should be connected (transitive)");

            Assert.AreEqual(setsBefore - 2, uf.CountSets(), "Two unions should reduce set count by 2");
        }

        [Test]
        public void EdgeCase_EmptySet()
        {
            // Arrange
            var emptyUf = new UnionFind(0);

            // Assert
            Assert.AreEqual(0, emptyUf.CountSets());
        }

        [Test]
        public void EdgeCase_SingleElement()
        {
            // Arrange
            var singleUf = new UnionFind(1);

            // Assert
            Assert.AreEqual(1, singleUf.CountSets());
            Assert.IsTrue(singleUf.Connected(0, 0));
            Assert.AreEqual(0, singleUf.Find(0));
        }

        [Test]
        public void EdgeCase_UnionSameElement()
        {
            // Act
            var result = _unionFind.Union(5, 5);

            // Assert
            Assert.IsFalse(result, "Unioning element with itself should return false");
            Assert.AreEqual(10, _unionFind.CountSets(), "Set count should not change");
        }
    }
}