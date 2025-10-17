using NUnit.Framework;
using Core.Models;
using System;
using System.Linq;

namespace Tests.Core.Models
{
    [TestFixture]
    public class PathNetworkStateTests
    {
        private PathNetworkState _network;
        
        [SetUp]
        public void SetUp()
        {
            _network = new PathNetworkState();
        }
        
        #region Constructor and Basic Operations
        
        [Test]
        public void Constructor_DefaultSize_Creates24PathPoints()
        {
            // Arrange & Act
            var network = new PathNetworkState();
            
            // Assert - All points should be in separate sets initially
            for (int i = 0; i < PathNetworkState.StandardPathPointCount; i++)
            {
                Assert.AreEqual(i, network.GetPathId(i), 
                    $"Point {i} should be its own path initially");
            }
        }
        
        [Test]
        public void Constructor_CustomSize_CreatesCorrectNumberOfPoints()
        {
            // Arrange & Act
            var network = new PathNetworkState(10);
            
            // Assert
            for (int i = 0; i < 10; i++)
            {
                Assert.DoesNotThrow(() => network.GetPathId(i));
            }
            
            Assert.Throws<ArgumentOutOfRangeException>(() => network.GetPathId(10));
        }
        
        #endregion
        
        #region GetPathId
        
        [Test]
        public void GetPathId_UnconnectedPoint_ReturnsItselfAsId()
        {
            // Assert
            Assert.AreEqual(5, _network.GetPathId(5));
            Assert.AreEqual(12, _network.GetPathId(12));
        }
        
        [Test]
        public void GetPathId_InvalidPoint_ThrowsException()
        {
            // Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => _network.GetPathId(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => _network.GetPathId(24));
            Assert.Throws<ArgumentOutOfRangeException>(() => _network.GetPathId(100));
        }
        
        #endregion
        
        #region AreConnected
        
        [Test]
        public void AreConnected_SamePoint_ReturnsTrue()
        {
            // Assert
            Assert.IsTrue(_network.AreConnected(0, 0));
            Assert.IsTrue(_network.AreConnected(15, 15));
        }
        
        [Test]
        public void AreConnected_UnconnectedPoints_ReturnsFalse()
        {
            // Assert
            Assert.IsFalse(_network.AreConnected(0, 1));
            Assert.IsFalse(_network.AreConnected(10, 20));
        }
        
        [Test]
        public void AreConnected_AfterConnection_ReturnsTrue()
        {
            // Arrange
            _network.ConnectPoints(5, 10);
            
            // Assert
            Assert.IsTrue(_network.AreConnected(5, 10));
            Assert.IsTrue(_network.AreConnected(10, 5)); // Symmetric
        }
        
        [Test]
        public void AreConnected_TransitiveConnection_ReturnsTrue()
        {
            // Arrange
            _network.ConnectPoints(0, 1);
            _network.ConnectPoints(1, 2);
            _network.ConnectPoints(2, 3);
            
            // Assert - All should be transitively connected
            Assert.IsTrue(_network.AreConnected(0, 3));
            Assert.IsTrue(_network.AreConnected(0, 2));
            Assert.IsTrue(_network.AreConnected(1, 3));
        }
        
        #endregion
        
        #region ConnectPoints (Two Points)
        
        [Test]
        public void ConnectPoints_TwoPoints_ConnectsThem()
        {
            // Act
            _network.ConnectPoints(7, 14);
            
            // Assert
            Assert.IsTrue(_network.AreConnected(7, 14));
        }
        
        [Test]
        public void ConnectPoints_TwoPoints_IncrementsConnectionCounts()
        {
            // Act
            _network.ConnectPoints(3, 8);
            
            // Assert
            Assert.AreEqual(1, _network.GetConnectionCount(3));
            Assert.AreEqual(1, _network.GetConnectionCount(8));
        }
        
        [Test]
        public void ConnectPoints_MultipleConnections_AccumulatesCounts()
        {
            // Act
            _network.ConnectPoints(5, 10);
            _network.ConnectPoints(5, 15);
            
            // Assert
            Assert.AreEqual(2, _network.GetConnectionCount(5), "Point 5 connected twice");
            Assert.AreEqual(1, _network.GetConnectionCount(10));
            Assert.AreEqual(1, _network.GetConnectionCount(15));
        }
        
        #endregion
        
        #region ConnectPoints (Multiple Points)
        
        [Test]
        public void ConnectPoints_MultiplePoints_ConnectsAllTogether()
        {
            // Act
            _network.ConnectPoints(new[] { 0, 5, 10, 15 });
            
            // Assert
            Assert.IsTrue(_network.AreConnected(0, 5));
            Assert.IsTrue(_network.AreConnected(0, 10));
            Assert.IsTrue(_network.AreConnected(0, 15));
            Assert.IsTrue(_network.AreConnected(5, 10));
            Assert.IsTrue(_network.AreConnected(5, 15));
            Assert.IsTrue(_network.AreConnected(10, 15));
        }
        
        [Test]
        public void ConnectPoints_MultiplePoints_IncrementsAllCounts()
        {
            // Act
            _network.ConnectPoints(new[] { 1, 2, 3, 4 });
            
            // Assert
            Assert.AreEqual(1, _network.GetConnectionCount(1));
            Assert.AreEqual(1, _network.GetConnectionCount(2));
            Assert.AreEqual(1, _network.GetConnectionCount(3));
            Assert.AreEqual(1, _network.GetConnectionCount(4));
        }
        
        [Test]
        public void ConnectPoints_SinglePoint_DoesNothing()
        {
            // Act
            _network.ConnectPoints(new[] { 5 });
            
            // Assert
            Assert.AreEqual(0, _network.GetConnectionCount(5), "Single point should not increment count");
        }
        
        [Test]
        public void ConnectPoints_EmptyArray_DoesNothing()
        {
            // Act & Assert
            Assert.DoesNotThrow(() => _network.ConnectPoints(new int[] { }));
        }
        
        #endregion
        
        #region GetConnectionCount
        
        [Test]
        public void GetConnectionCount_UnconnectedPoint_ReturnsZero()
        {
            // Assert
            Assert.AreEqual(0, _network.GetConnectionCount(0));
            Assert.AreEqual(0, _network.GetConnectionCount(23));
        }
        
        [Test]
        public void GetConnectionCount_ConnectedPoint_ReturnsCorrectCount()
        {
            // Arrange
            _network.ConnectPoints(5, 10);
            _network.ConnectPoints(5, 15);
            _network.ConnectPoints(5, 20);
            
            // Assert
            Assert.AreEqual(3, _network.GetConnectionCount(5));
        }
        
        [Test]
        public void GetConnectionCount_InvalidPoint_ThrowsException()
        {
            // Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => _network.GetConnectionCount(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => _network.GetConnectionCount(24));
        }
        
        #endregion
        
        #region GetPointsInPath
        
        [Test]
        public void GetPointsInPath_IsolatedPoint_ReturnsOnlyThatPoint()
        {
            // Act
            var points = _network.GetPointsInPath(10).ToList();
            
            // Assert
            Assert.AreEqual(1, points.Count);
            Assert.AreEqual(10, points[0]);
        }
        
        [Test]
        public void GetPointsInPath_ConnectedPoints_ReturnsAllInPath()
        {
            // Arrange
            _network.ConnectPoints(1, 5);
            _network.ConnectPoints(5, 9);
            _network.ConnectPoints(9, 13);
            
            // Act
            var points = _network.GetPointsInPath(1).ToList();
            
            // Assert
            Assert.AreEqual(4, points.Count);
            CollectionAssert.Contains(points, 1);
            CollectionAssert.Contains(points, 5);
            CollectionAssert.Contains(points, 9);
            CollectionAssert.Contains(points, 13);
        }
        
        [Test]
        public void GetPointsInPath_ReturnsConsistentResultForAnyPointInPath()
        {
            // Arrange
            _network.ConnectPoints(2, 7);
            _network.ConnectPoints(7, 12);
            
            // Act
            var pointsFrom2 = _network.GetPointsInPath(2).ToList();
            var pointsFrom7 = _network.GetPointsInPath(7).ToList();
            var pointsFrom12 = _network.GetPointsInPath(12).ToList();
            
            // Assert
            CollectionAssert.AreEquivalent(pointsFrom2, pointsFrom7);
            CollectionAssert.AreEquivalent(pointsFrom2, pointsFrom12);
        }
        
        #endregion
        
        #region Reset
        
        [Test]
        public void Reset_DisconnectsAllPoints()
        {
            // Arrange
            _network.ConnectPoints(0, 5);
            _network.ConnectPoints(5, 10);
            _network.ConnectPoints(15, 20);
            
            // Act
            _network.Reset();
            
            // Assert
            Assert.IsFalse(_network.AreConnected(0, 5));
            Assert.IsFalse(_network.AreConnected(5, 10));
            Assert.IsFalse(_network.AreConnected(15, 20));
        }
        
        [Test]
        public void Reset_ClearsAllConnectionCounts()
        {
            // Arrange
            _network.ConnectPoints(3, 7);
            _network.ConnectPoints(3, 11);
            
            // Act
            _network.Reset();
            
            // Assert
            Assert.AreEqual(0, _network.GetConnectionCount(3));
            Assert.AreEqual(0, _network.GetConnectionCount(7));
            Assert.AreEqual(0, _network.GetConnectionCount(11));
        }
        
        [Test]
        public void Reset_AllowsNewConnections()
        {
            // Arrange
            _network.ConnectPoints(0, 10);
            _network.Reset();
            
            // Act
            _network.ConnectPoints(0, 15);
            
            // Assert
            Assert.IsTrue(_network.AreConnected(0, 15));
            Assert.IsFalse(_network.AreConnected(0, 10));
        }
        
        #endregion
        
        #region Game-Specific Scenarios
        
        [Test]
        public void GameScenario_CurveTileConnection()
        {
            // Simulate a curve tile at slot 0 connecting path points 13 (right) and 3 (bottom)
            // Act
            _network.ConnectPoints(13, 3);
            
            // Assert
            Assert.IsTrue(_network.AreConnected(13, 3));
            Assert.AreEqual(1, _network.GetConnectionCount(13));
            Assert.AreEqual(1, _network.GetConnectionCount(3));
        }
        
        [Test]
        public void GameScenario_TwoCurvesTile_TwoSeparatePaths()
        {
            // Simulate a two-curves tile connecting:
            // - Top (0) to Left (21)
            // - Right (13) to Bottom (3)
            
            // Act
            _network.ConnectPoints(0, 21);
            _network.ConnectPoints(13, 3);
            
            // Assert
            Assert.IsTrue(_network.AreConnected(0, 21), "First curve connected");
            Assert.IsTrue(_network.AreConnected(13, 3), "Second curve connected");
            Assert.IsFalse(_network.AreConnected(0, 13), "Two curves should not connect");
        }
        
        [Test]
        public void GameScenario_IntersectionTile_ThreeWayConnection()
        {
            // Simulate T-intersection connecting top (0), right (13), and bottom (3)
            // Act
            _network.ConnectPoints(new[] { 0, 13, 3 });
            
            // Assert
            Assert.IsTrue(_network.AreConnected(0, 13));
            Assert.IsTrue(_network.AreConnected(0, 3));
            Assert.IsTrue(_network.AreConnected(13, 3));
            
            Assert.AreEqual(1, _network.GetConnectionCount(0));
            Assert.AreEqual(1, _network.GetConnectionCount(13));
            Assert.AreEqual(1, _network.GetConnectionCount(3));
        }
        
        [Test]
        public void GameScenario_XIntersectionTile_FourWayConnection()
        {
            // Simulate X-intersection connecting all 4 sides
            // Act
            _network.ConnectPoints(new[] { 0, 13, 3, 21 });
            
            // Assert - All points should be connected
            foreach (int i in new[] { 0, 13, 3, 21 })
            {
                foreach (int j in new[] { 0, 13, 3, 21 })
                {
                    Assert.IsTrue(_network.AreConnected(i, j), 
                        $"Points {i} and {j} should be connected in X-intersection");
                }
            }
        }
        
        [Test]
        public void GameScenario_BridgeTile_TwoIndependentPaths()
        {
            // Simulate bridge with vertical (0-3) and horizontal (13-21) paths
            // Act
            _network.ConnectPoints(0, 3);   // Vertical
            _network.ConnectPoints(13, 21); // Horizontal
            
            // Assert
            Assert.IsTrue(_network.AreConnected(0, 3), "Vertical path connected");
            Assert.IsTrue(_network.AreConnected(13, 21), "Horizontal path connected");
            Assert.IsFalse(_network.AreConnected(0, 13), "Bridge paths independent");
            Assert.IsFalse(_network.AreConnected(3, 21), "Bridge paths independent");
            
            // Each point should have exactly 1 connection
            Assert.AreEqual(1, _network.GetConnectionCount(0));
            Assert.AreEqual(1, _network.GetConnectionCount(3));
            Assert.AreEqual(1, _network.GetConnectionCount(13));
            Assert.AreEqual(1, _network.GetConnectionCount(21));
        }
        
        [Test]
        public void GameScenario_MultipleAdjacentTiles_MergePaths()
        {
            // Simulate two adjacent curve tiles forming one long path
            // Tile 1: connects points 13-3
            // Tile 2: connects points 3-7 (point 3 is shared edge)
            
            // Act
            _network.ConnectPoints(13, 3);
            _network.ConnectPoints(3, 7);
            
            // Assert
            Assert.IsTrue(_network.AreConnected(13, 7), "Path should connect through shared point");
            Assert.AreEqual(2, _network.GetConnectionCount(3), "Middle point has 2 connections");
            Assert.AreEqual(1, _network.GetConnectionCount(13), "End point has 1 connection");
            Assert.AreEqual(1, _network.GetConnectionCount(7), "End point has 1 connection");
        }
        
        [Test]
        public void GameScenario_ComplexPathNetwork()
        {
            // Create a more complex network with multiple paths
            // Path 1: 0-1-2-3
            _network.ConnectPoints(0, 1);
            _network.ConnectPoints(1, 2);
            _network.ConnectPoints(2, 3);
            
            // Path 2: 10-11-12
            _network.ConnectPoints(10, 11);
            _network.ConnectPoints(11, 12);
            
            // Path 3: 20-21
            _network.ConnectPoints(20, 21);
            
            // Assert
            Assert.IsTrue(_network.AreConnected(0, 3), "Path 1 connected");
            Assert.IsTrue(_network.AreConnected(10, 12), "Path 2 connected");
            Assert.IsTrue(_network.AreConnected(20, 21), "Path 3 connected");
            
            Assert.IsFalse(_network.AreConnected(0, 10), "Different paths not connected");
            Assert.IsFalse(_network.AreConnected(10, 20), "Different paths not connected");
            Assert.IsFalse(_network.AreConnected(0, 20), "Different paths not connected");
            
            // Check connection counts
            Assert.AreEqual(1, _network.GetConnectionCount(0), "End point");
            Assert.AreEqual(2, _network.GetConnectionCount(1), "Middle point");
            Assert.AreEqual(2, _network.GetConnectionCount(2), "Middle point");
            Assert.AreEqual(1, _network.GetConnectionCount(3), "End point");
        }
        
        [Test]
        public void GameScenario_InvalidConnection_TooManyConnections()
        {
            // In actual game, path points should have 0, 1, or 2 connections
            // This test verifies we can track overcounting (for validation)
            
            // Act
            _network.ConnectPoints(5, 10);
            _network.ConnectPoints(5, 15);
            _network.ConnectPoints(5, 20); // 3rd connection - invalid!
            
            // Assert
            Assert.AreEqual(3, _network.GetConnectionCount(5), 
                "Should track invalid connection count for validation");
        }
        
        #endregion
    }
}
