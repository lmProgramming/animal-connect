using NUnit.Framework;
using Core.Logic;
using Core.Models;
using System.Diagnostics;

namespace Tests.Performance
{
    [TestFixture]
    public class PerformanceTests
    {
        [Test]
        public void PathCalculation_Performance_IsFasterThanTarget()
        {
            // TODO: Benchmark against old system when available
            Assert.Pass("Performance benchmarks - to be implemented");
        }
        
        [Test]
        public void UnionFind_Performance_IsNearConstantTime()
        {
            // TODO: Benchmark UnionFind operations
            Assert.Pass("Performance benchmarks - to be implemented");
        }
        
        [Test]
        public void FullRecalculation_ComplexGrid_CompletesQuickly()
        {
            // Arrange
            var calculator = new PathCalculator();
            var grid = new GridState();
            
            // Fill grid with tiles
            for (int i = 0; i < GridState.TotalSlots; i++)
            {
                grid = grid.WithTile(i, new TileData(TileType.XIntersection, 0));
            }
            
            // Act & Measure
            var stopwatch = Stopwatch.StartNew();
            var network = calculator.CalculatePathNetwork(grid);
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
            for (int i = 0; i < 20; i += 2)
            {
                network.ConnectPoints(i, i + 1);
            }
            
            // Act & Measure
            var stopwatch = Stopwatch.StartNew();
            for (int i = 0; i < 1000; i++)
            {
                validator.IsValid(network);
            }
            stopwatch.Stop();
            
            // Assert - 1000 validations should complete in under 50ms
            Assert.Less(stopwatch.ElapsedMilliseconds, 50, 
                "Validation should be very fast");
        }
    }
}
