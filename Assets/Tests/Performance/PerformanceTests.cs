using System.Diagnostics;
using Core.DataStructures;
using Core.Logic;
using Core.Models;
using NUnit.Framework;

namespace Tests.Performance
{
    [TestFixture]
    public class PerformanceTests
    {
        [Test]
        public void PathCalculation_Performance_IsFasterThanTarget()
        {
            // Arrange
            var calculator = new PathCalculator();
            var grid = new GridState();

            // Create a moderately complex grid
            grid = grid.WithTile(0, new TileData(TileType.Curve));
            grid = grid.WithTile(1, new TileData(TileType.Bridge));
            grid = grid.WithTile(2, new TileData(TileType.Intersection, 1));
            grid = grid.WithTile(3, new TileData(TileType.Curve, 2));
            grid = grid.WithTile(4, new TileData(TileType.XIntersection));
            grid = grid.WithTile(5, new TileData(TileType.Bridge, 1));

            // Act & Measure - Run multiple times to get average
            var stopwatch = Stopwatch.StartNew();
            for (var i = 0; i < 1000; i++) calculator.CalculatePathNetwork(grid);
            stopwatch.Stop();

            var averageMs = stopwatch.ElapsedMilliseconds / 1000.0;

            // Assert - Each calculation should take less than 1ms on average
            Assert.Less(averageMs, 1.0,
                $"Path calculation took {averageMs:F3}ms on average, should be under 1ms");
        }

        [Test]
        public void UnionFind_Performance_IsNearConstantTime()
        {
            // Arrange
            var unionFind = new UnionFind(1000);

            // Act & Measure - Time union operations
            var stopwatch = Stopwatch.StartNew();

            // Perform many union operations
            for (var i = 0; i < 999; i++) unionFind.Union(i, i + 1);

            // Perform many find operations
            for (var i = 0; i < 1000; i++) unionFind.Find(i);

            stopwatch.Stop();

            // Assert - Should complete in under 10ms due to path compression
            Assert.Less(stopwatch.ElapsedMilliseconds, 10,
                $"UnionFind operations took {stopwatch.ElapsedMilliseconds}ms, should be under 10ms");

            // Verify path compression worked - all should point to same root
            var root = unionFind.Find(0);
            for (var i = 1; i < 1000; i++)
                Assert.AreEqual(root, unionFind.Find(i),
                    "All elements should be in the same set after chaining unions");
        }

        [Test]
        public void FullRecalculation_ComplexGrid_CompletesQuickly()
        {
            // Arrange
            var calculator = new PathCalculator();
            var grid = new GridState();

            // Fill grid with tiles
            for (var i = 0; i < GridState.TotalSlots; i++)
                grid = grid.WithTile(i, new TileData(TileType.XIntersection));

            // Act & Measure
            var stopwatch = Stopwatch.StartNew();
            calculator.CalculatePathNetwork(grid);
            stopwatch.Stop();

            // Assert - Should complete in under 5ms
            Assert.Less(stopwatch.ElapsedMilliseconds, 5,
                "Path calculation should be very fast even for complex grids");
        }

        [Test]
        public void ValidationPerformance_ComplexNetwork_IsEfficient()
        {
            // Arrange
            var validator = new ConnectionValidator();
            var network = new PathNetworkState();

            // Create complex network
            for (var i = 0; i < 20; i += 2) network.ConnectPoints(i, i + 1);

            // Act & Measure
            var stopwatch = Stopwatch.StartNew();
            for (var i = 0; i < 1000; i++) validator.IsValid(network);
            stopwatch.Stop();

            // Assert - 1000 validations should complete in under 50ms
            Assert.Less(stopwatch.ElapsedMilliseconds, 50,
                "Validation should be very fast");
        }
    }
}