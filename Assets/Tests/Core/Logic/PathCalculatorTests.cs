using System.Linq;
using Core.Configuration;
using Core.Logic;
using Core.Models;
using NUnit.Framework;

namespace Tests.Core.Logic
{
    [TestFixture]
    public class PathCalculatorTests
    {
        [SetUp]
        public void SetUp()
        {
            _calculator = new PathCalculator();
            _emptyGrid = new GridState();
        }

        private PathCalculator _calculator;
        private GridState _emptyGrid;

        [Test]
        public void CalculatePathNetwork_EmptyGrid_AllPointsDisconnected()
        {
            // Act
            var network = _calculator.CalculatePathNetwork(_emptyGrid);

            // Assert - No connections should exist
            for (var i = 0; i < GridConfiguration.TotalPathPoints; i++)
            {
                Assert.AreEqual(0, network.GetConnectionCount(i),
                    $"Point {i} should have no connections");

                // Each point should be its own path
                Assert.AreEqual(i, network.GetPathId(i));
            }
        }

        [Test]
        public void CalculatePathNetwork_SingleCurveTile_ConnectsTwoPoints()
        {
            // Arrange - Place curve at slot 0 (rotation 0: right-bottom)
            var tile = new TileData(TileType.Curve);
            var grid = _emptyGrid.WithTile(0, tile);

            // Slot 0 has path points [0, 13, 3, 12] (top, right, bottom, left)
            // Curve connects right (13) to bottom (3)

            // Act
            var network = _calculator.CalculatePathNetwork(grid);

            // Assert
            Assert.IsTrue(network.AreConnected(13, 3), "Right and bottom should be connected");
            Assert.AreEqual(1, network.GetConnectionCount(13));
            Assert.AreEqual(1, network.GetConnectionCount(3));

            // Other points at slot 0 should not be connected
            Assert.IsFalse(network.AreConnected(0, 13), "Top not connected to right");
            Assert.IsFalse(network.AreConnected(12, 3), "Left not connected to bottom");
        }

        [Test]
        public void CalculatePathNetwork_CurveTileRotated90_ConnectsCorrectSides()
        {
            // Arrange - Curve rotated 90°: connects bottom-left
            var tile = new TileData(TileType.Curve, 1);
            var grid = _emptyGrid.WithTile(4, tile); // Center slot [4, 18, 7, 17]

            // Act
            var network = _calculator.CalculatePathNetwork(grid);

            // Assert - Bottom (7) and left (17) should be connected
            Assert.IsTrue(network.AreConnected(7, 17));
            Assert.AreEqual(1, network.GetConnectionCount(7));
            Assert.AreEqual(1, network.GetConnectionCount(17));
        }

        [Test]
        public void CalculatePathNetwork_TwoCurvesTile_CreatesTwoSeparatePaths()
        {
            // Arrange - Two curves tile at slot 4
            var tile = new TileData(TileType.TwoCurves);
            var grid = _emptyGrid.WithTile(4, tile);

            // Slot 4: [4, 18, 7, 17] (top, right, bottom, left)
            // Two curves: top-left (4-17) and right-bottom (18-7)

            // Act
            var network = _calculator.CalculatePathNetwork(grid);

            // Assert
            Assert.IsTrue(network.AreConnected(4, 17), "First curve: top-left");
            Assert.IsTrue(network.AreConnected(18, 7), "Second curve: right-bottom");
            Assert.IsFalse(network.AreConnected(4, 18), "Two paths should be independent");

            Assert.AreEqual(1, network.GetConnectionCount(4));
            Assert.AreEqual(1, network.GetConnectionCount(17));
            Assert.AreEqual(1, network.GetConnectionCount(18));
            Assert.AreEqual(1, network.GetConnectionCount(7));
        }

        [Test]
        public void CalculatePathNetwork_IntersectionTile_ConnectsThreeSides()
        {
            // Arrange - T-intersection at slot 1
            var tile = new TileData(TileType.Intersection);
            var grid = _emptyGrid.WithTile(1, tile);

            // Slot 1: [1, 14, 4, 13] (top, right, bottom, left)
            // Intersection connects top-right-bottom (1, 14, 4)

            // Act
            var network = _calculator.CalculatePathNetwork(grid);

            // Assert
            Assert.IsTrue(network.AreConnected(1, 14));
            Assert.IsTrue(network.AreConnected(1, 4));
            Assert.IsTrue(network.AreConnected(14, 4));
            Assert.IsFalse(network.AreConnected(1, 13), "Left should not be connected");

            Assert.AreEqual(1, network.GetConnectionCount(1));
            Assert.AreEqual(1, network.GetConnectionCount(14));
            Assert.AreEqual(1, network.GetConnectionCount(4));
            Assert.AreEqual(0, network.GetConnectionCount(13));
        }

        [Test]
        public void CalculatePathNetwork_XIntersectionTile_ConnectsAllFourSides()
        {
            // Arrange
            var tile = new TileData(TileType.XIntersection);
            var grid = _emptyGrid.WithTile(4, tile);

            // Slot 4: [4, 18, 7, 17]

            // Act
            var network = _calculator.CalculatePathNetwork(grid);

            // Assert - All 4 sides connected
            Assert.IsTrue(network.AreConnected(4, 18));
            Assert.IsTrue(network.AreConnected(4, 7));
            Assert.IsTrue(network.AreConnected(4, 17));
            Assert.IsTrue(network.AreConnected(18, 7));
            Assert.IsTrue(network.AreConnected(18, 17));
            Assert.IsTrue(network.AreConnected(7, 17));

            foreach (var point in new[] { 4, 18, 7, 17 }) Assert.AreEqual(1, network.GetConnectionCount(point));
        }

        [Test]
        public void CalculatePathNetwork_BridgeTile_CreatesTwoIndependentPaths()
        {
            // Arrange
            var tile = new TileData(TileType.Bridge);
            var grid = _emptyGrid.WithTile(4, tile);

            // Slot 4: [4, 18, 7, 17]
            // Bridge: vertical (4-7) and horizontal (18-17)

            // Act
            var network = _calculator.CalculatePathNetwork(grid);

            // Assert
            Assert.IsTrue(network.AreConnected(4, 7), "Vertical path");
            Assert.IsTrue(network.AreConnected(18, 17), "Horizontal path");
            Assert.IsFalse(network.AreConnected(4, 18), "Paths should not intersect");
            Assert.IsFalse(network.AreConnected(7, 17), "Paths should not intersect");

            foreach (var point in new[] { 4, 7, 18, 17 }) Assert.AreEqual(1, network.GetConnectionCount(point));
        }

        [Test]
        public void CalculatePathNetwork_TwoAdjacentCurves_FormsLongPath()
        {
            // Arrange - Two curves that connect
            // Slot 0: [0, 13, 3, 12] - rotation 0 connects right-bottom (13-3)
            // Slot 3: [3, 17, 6, 16] - rotation 3 connects top-right (3-17) - shares point 3 with slot 0
            var curve1 = new TileData(TileType.Curve);
            var curve2 = new TileData(TileType.Curve, 3); // Rotated to connect top-right

            var grid = _emptyGrid
                .WithTile(0, curve1)
                .WithTile(3, curve2);

            // Act
            var network = _calculator.CalculatePathNetwork(grid);

            // Assert - Should form one continuous path through point 3
            Assert.IsTrue(network.AreConnected(13, 17), "Path should connect through shared point");
            Assert.AreEqual(2, network.GetConnectionCount(3), "Shared point has 2 connections");
        }

        [Test]
        public void CalculatePathNetwork_MultipleDisconnectedPaths()
        {
            // Arrange - Create 3 separate paths
            // Slot 0: [0, 13, 3, 12] - rotation 0 connects right-bottom (13-3)
            // Slot 2: [2, 15, 5, 14] - rotation 1 connects bottom-left (5-14)
            // Slot 8: [8, 23, 11, 22] - rotation 0 connects right-bottom (23-11)
            var grid = _emptyGrid
                .WithTile(0, new TileData(TileType.Curve)) // Path 1: 13-3
                .WithTile(2, new TileData(TileType.Curve, 1)) // Path 2: 5-14
                .WithTile(8, new TileData(TileType.Curve)); // Path 3: 23-11

            // Act
            var network = _calculator.CalculatePathNetwork(grid);

            // Assert - Each path is independent
            Assert.IsTrue(network.AreConnected(13, 3), "Path 1");
            Assert.IsTrue(network.AreConnected(5, 14), "Path 2");
            Assert.IsTrue(network.AreConnected(23, 11), "Path 3");

            Assert.IsFalse(network.AreConnected(13, 14), "Paths 1 and 2 disconnected");
            Assert.IsFalse(network.AreConnected(14, 23), "Paths 2 and 3 disconnected");
            Assert.IsFalse(network.AreConnected(13, 23), "Paths 1 and 3 disconnected");
        }

        [Test]
        public void CalculatePathNetwork_ComplexGrid_CorrectlyTracksAllConnections()
        {
            // Arrange - Create a more complex scenario
            var grid = _emptyGrid
                .WithTile(0, new TileData(TileType.Curve)) // 13-3
                .WithTile(1, new TileData(TileType.Intersection)) // 1-14-4
                .WithTile(4, new TileData(TileType.XIntersection)) // 4-18-7-17
                .WithTile(7, new TileData(TileType.Bridge)); // 7-10, 22-21

            // Act
            var network = _calculator.CalculatePathNetwork(grid);

            // Assert - Check key connections
            Assert.IsTrue(network.AreConnected(1, 4), "Intersection connects to X");
            Assert.IsTrue(network.AreConnected(4, 7), "X connects to bridge");
            Assert.IsTrue(network.AreConnected(1, 7), "Path flows through");

            // Bridge's other path should be separate
            Assert.IsTrue(network.AreConnected(22, 21), "Bridge's horizontal path");
            Assert.IsFalse(network.AreConnected(7, 22), "Bridge paths independent");
        }

        [Test]
        public void GetAffectedPathPoints_ReturnsCorrectPointsForEachSlot()
        {
            // Test a few slots to verify correct mapping
            var slot0Points = _calculator.GetAffectedPathPoints(0).ToArray();
            CollectionAssert.AreEqual(new[] { 0, 13, 3, 12 }, slot0Points);

            var slot4Points = _calculator.GetAffectedPathPoints(4).ToArray();
            CollectionAssert.AreEqual(new[] { 4, 18, 7, 17 }, slot4Points);

            var slot8Points = _calculator.GetAffectedPathPoints(8).ToArray();
            CollectionAssert.AreEqual(new[] { 8, 23, 11, 22 }, slot8Points);
        }

        [Test]
        public void ValidateTilePlacement_SingleTile_ReturnsFalse()
        {
            // Arrange - Place a single curve that creates dead-ends
            var tile = new TileData(TileType.Curve);

            // Act
            var isValid = _calculator.ValidateTilePlacement(_emptyGrid, 0, tile);

            // Assert
            Assert.IsFalse(isValid, "Single tile creates dead-ends, violating 'road in, road out' rule");
        }

        [Test]
        public void ValidateTilePlacement_TwoConnectedCurves_ReturnsFalse()
        {
            // Arrange - Two curves that connect but still have dead-ends at endpoints
            var grid = _emptyGrid.WithTile(0, new TileData(TileType.Curve));
            var newTile = new TileData(TileType.Curve, 1);

            // Act
            var isValid = _calculator.ValidateTilePlacement(grid, 3, newTile);

            // Assert
            Assert.IsFalse(isValid, "Endpoints still have dead-ends");
        }

        [Test]
        public void ValidateTilePlacement_ClosedLoop_ReturnsTrue()
        {
            // Arrange - Create a closed loop where all internal points have 2 connections
            // This requires 4 curves forming a square around the edges
            var grid = _emptyGrid
                .WithTile(0, new TileData(TileType.Curve)) // Top-left: connects top to right
                .WithTile(1, new TileData(TileType.Curve, 1)) // Top-right: connects right to bottom
                .WithTile(4, new TileData(TileType.Curve, 2)); // Middle: connects bottom to left

            var newTile = new TileData(TileType.Curve, 3); // Connects left to top, completing loop

            // Act
            var isValid = _calculator.ValidateTilePlacement(grid, 3, newTile);

            // Assert
            Assert.IsTrue(isValid, "Closed loop with all points having 2 connections is valid");
        }

        [Test]
        public void UpdateForTileChange_ProducesSameResultAsFullCalculation()
        {
            // Arrange
            var oldGrid = _emptyGrid.WithTile(0, new TileData(TileType.Curve));
            var newGrid = oldGrid.WithTile(4, new TileData(TileType.Intersection, 1));

            // Act
            var incrementalNetwork = _calculator.UpdateForTileChange(oldGrid, newGrid, 4);
            var fullNetwork = _calculator.CalculatePathNetwork(newGrid);

            // Assert - Both methods should produce identical results
            for (var i = 0; i < GridConfiguration.TotalPathPoints; i++)
            {
                Assert.AreEqual(
                    fullNetwork.GetConnectionCount(i),
                    incrementalNetwork.GetConnectionCount(i),
                    $"Connection count mismatch at point {i}");

                Assert.AreEqual(
                    fullNetwork.GetPathId(i),
                    incrementalNetwork.GetPathId(i),
                    $"Path ID mismatch at point {i}");
            }
        }

        [Test]
        public void UpdateForTileSwap_ProducesSameResultAsFullCalculation()
        {
            // Arrange
            var oldGrid = _emptyGrid
                .WithTile(0, new TileData(TileType.Curve))
                .WithTile(8, new TileData(TileType.Bridge, 1));

            var newGrid = oldGrid.WithSwap(0, 8);

            // Act
            var swapNetwork = _calculator.UpdateForTileSwap(oldGrid, newGrid, 0, 8);
            var fullNetwork = _calculator.CalculatePathNetwork(newGrid);

            // Assert
            for (var i = 0; i < GridConfiguration.TotalPathPoints; i++)
                Assert.AreEqual(
                    fullNetwork.GetConnectionCount(i),
                    swapNetwork.GetConnectionCount(i),
                    $"Connection count mismatch at point {i}");
        }

        [Test]
        public void CalculatePathNetwork_AllTileTypes_ProduceValidConnections()
        {
            // Test each tile type to ensure no exceptions and valid connections
            var tileTypes = new[]
            {
                TileType.Curve,
                TileType.TwoCurves,
                TileType.Intersection,
                TileType.XIntersection,
                TileType.Bridge
            };

            foreach (var tileType in tileTypes)
            {
                var tile = new TileData(tileType);
                var grid = _emptyGrid.WithTile(4, tile);

                // Act & Assert - Should not throw
                Assert.DoesNotThrow(() =>
                    {
                        var network = _calculator.CalculatePathNetwork(grid);

                        // Verify connection counts are reasonable (0-4 per point)
                        for (var i = 0; i < GridConfiguration.TotalPathPoints; i++)
                        {
                            var count = network.GetConnectionCount(i);
                            Assert.GreaterOrEqual(count, 0);
                            Assert.LessOrEqual(count, 4,
                                $"{tileType} creates too many connections at point {i}");
                        }
                    }, $"{tileType} should not throw exceptions");
            }
        }

        [Test]
        public void CalculatePathNetwork_AllRotations_ProduceValidConnections()
        {
            // Test curve tile at all rotations
            for (var rotation = 0; rotation < 4; rotation++)
            {
                var tile = new TileData(TileType.Curve, rotation);
                var grid = _emptyGrid.WithTile(4, tile);

                var network = _calculator.CalculatePathNetwork(grid);

                // Should connect exactly 2 points with 1 connection each
                var connectedPoints = 0;
                for (var i = 0; i < GridConfiguration.TotalPathPoints; i++)
                    if (network.GetConnectionCount(i) > 0)
                        connectedPoints++;

                Assert.AreEqual(2, connectedPoints,
                    $"Curve at rotation {rotation} should connect exactly 2 points");
            }
        }
    }
}