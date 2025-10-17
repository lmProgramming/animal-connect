using NUnit.Framework;
using Core.Logic;
using Core.Models;
using Core.Configuration;
using System.Linq;

namespace Tests.Core.Logic
{
    [TestFixture]
    public class ConnectionValidatorTests
    {
        private ConnectionValidator _validator;
        private PathCalculator _calculator;
        
        [SetUp]
        public void SetUp()
        {
            _validator = new ConnectionValidator();
            _calculator = new PathCalculator();
        }
        
        #region Basic Validation Tests
        
        [Test]
        public void ValidateConnections_EmptyNetwork_IsValid()
        {
            // Arrange
            var network = new PathNetworkState();
            
            // Act
            var result = _validator.ValidateConnections(network);
            
            // Assert
            Assert.IsTrue(result.IsValid);
            Assert.AreEqual(0, result.Errors.Count);
        }
        
        [Test]
        public void IsValid_EmptyNetwork_ReturnsTrue()
        {
            // Arrange
            var network = new PathNetworkState();
            
            // Act
            bool isValid = _validator.IsValid(network);
            
            // Assert
            Assert.IsTrue(isValid);
        }
        
        #endregion
        
        #region Entity Point Validation
        
        [Test]
        public void ValidateConnections_EntityWithOneConnection_IsValid()
        {
            // Arrange
            var network = new PathNetworkState();
            int entityPoint = GridConfiguration.EntityToPathPoint[0]; // Entity 0's path point
            network.ConnectPoints(entityPoint, 13); // Connect to a non-entity point
            
            // Act
            var result = _validator.ValidateConnections(network);
            
            // Assert
            Assert.IsTrue(result.IsValid, "Entity with 1 connection should be valid");
        }
        
        [Test]
        public void ValidateConnections_EntityWithZeroConnections_IsValid()
        {
            // Arrange - Entity point with no connections
            var network = new PathNetworkState();
            
            // Act
            var result = _validator.ValidateConnections(network);
            
            // Assert
            Assert.IsTrue(result.IsValid, "Entity with 0 connections should be valid (unconnected)");
        }
        
        [Test]
        public void ValidateConnections_EntityWithTwoConnections_IsInvalid()
        {
            // Arrange
            var network = new PathNetworkState();
            int entityPoint = GridConfiguration.EntityToPathPoint[0];
            
            // Create invalid scenario: entity connected twice
            network.ConnectPoints(entityPoint, 13);
            network.ConnectPoints(entityPoint, 3);
            
            // Act
            var result = _validator.ValidateConnections(network);
            
            // Assert
            Assert.IsFalse(result.IsValid);
            Assert.Greater(result.Errors.Count, 0);
            Assert.IsTrue(result.Errors.Any(e => e.PathPoint == entityPoint));
        }
        
        [Test]
        public void ValidateConnections_EntityWithThreeConnections_IsInvalid()
        {
            // Arrange
            var network = new PathNetworkState();
            int entityPoint = GridConfiguration.EntityToPathPoint[1];
            
            // Create invalid scenario
            network.ConnectPoints(new[] { entityPoint, 13, 14, 4 });
            
            // Act
            var result = _validator.ValidateConnections(network);
            
            // Assert
            Assert.IsFalse(result.IsValid);
        }
        
        #endregion
        
        #region Non-Entity Point Validation
        
        [Test]
        public void ValidateConnections_NonEntityWithZeroConnections_IsValid()
        {
            // Arrange
            var network = new PathNetworkState();
            // Point 13 is non-entity
            
            // Act
            var result = _validator.ValidateConnections(network);
            
            // Assert
            Assert.IsTrue(result.IsValid);
        }
        
        [Test]
        public void ValidateConnections_NonEntityWithTwoConnections_IsValid()
        {
            // Arrange
            var network = new PathNetworkState();
            int nonEntityPoint = 13; // Non-entity point
            
            network.ConnectPoints(nonEntityPoint, 3);
            network.ConnectPoints(nonEntityPoint, 17);
            
            // Act
            var result = _validator.ValidateConnections(network);
            
            // Assert
            Assert.IsTrue(result.IsValid, "Non-entity with 2 connections should be valid");
        }
        
        [Test]
        public void ValidateConnections_NonEntityWithOneConnection_IsInvalid()
        {
            // Arrange
            var network = new PathNetworkState();
            int nonEntityPoint = 13;
            
            network.ConnectPoints(nonEntityPoint, 3);
            
            // Act
            var result = _validator.ValidateConnections(network);
            
            // Assert
            Assert.IsFalse(result.IsValid, "Non-entity with 1 connection should be invalid (dead end)");
            Assert.IsTrue(result.Errors.Any(e => e.PathPoint == nonEntityPoint));
        }
        
        [Test]
        public void ValidateConnections_NonEntityWithThreeConnections_IsInvalid()
        {
            // Arrange
            var network = new PathNetworkState();
            int nonEntityPoint = 13;
            
            network.ConnectPoints(nonEntityPoint, 0);
            network.ConnectPoints(nonEntityPoint, 3);
            network.ConnectPoints(nonEntityPoint, 14);
            
            // Act
            var result = _validator.ValidateConnections(network);
            
            // Assert
            Assert.IsFalse(result.IsValid, "Non-entity with 3 connections should be invalid");
        }
        
        #endregion
        
        #region Real Tile Scenarios
        
        [Test]
        public void ValidateConnections_SingleCurveTile_IsValid()
        {
            // Arrange
            var grid = new GridState().WithTile(0, new TileData(TileType.Curve, 0));
            var network = _calculator.CalculatePathNetwork(grid);
            
            // Act
            var result = _validator.ValidateConnections(network);
            
            // Assert
            Assert.IsTrue(result.IsValid, "Single curve should create valid connections");
        }
        
        [Test]
        public void ValidateConnections_TwoCurvesTile_IsValid()
        {
            // Arrange
            var grid = new GridState().WithTile(4, new TileData(TileType.TwoCurves, 0));
            var network = _calculator.CalculatePathNetwork(grid);
            
            // Act
            var result = _validator.ValidateConnections(network);
            
            // Assert
            Assert.IsTrue(result.IsValid);
        }
        
        [Test]
        public void ValidateConnections_IntersectionTile_IsInvalid()
        {
            // Arrange - Single intersection creates dead ends (3 connections on non-entity points)
            var grid = new GridState().WithTile(4, new TileData(TileType.Intersection, 0));
            var network = _calculator.CalculatePathNetwork(grid);
            
            // Act
            var result = _validator.ValidateConnections(network);
            
            // Assert
            // This is actually invalid in real game - intersection needs other tiles to be valid
            Assert.IsFalse(result.IsValid, "Lone intersection has points with only 1 connection");
        }
        
        [Test]
        public void ValidateConnections_XIntersectionTile_IsInvalid()
        {
            // Arrange
            var grid = new GridState().WithTile(4, new TileData(TileType.XIntersection, 0));
            var network = _calculator.CalculatePathNetwork(grid);
            
            // Act
            var result = _validator.ValidateConnections(network);
            
            // Assert
            Assert.IsFalse(result.IsValid, "Lone X-intersection creates dead ends");
        }
        
        [Test]
        public void ValidateConnections_BridgeTile_IsValid()
        {
            // Arrange
            var grid = new GridState().WithTile(4, new TileData(TileType.Bridge, 0));
            var network = _calculator.CalculatePathNetwork(grid);
            
            // Act
            var result = _validator.ValidateConnections(network);
            
            // Assert
            Assert.IsTrue(result.IsValid, "Bridge creates two valid straight paths");
        }
        
        [Test]
        public void ValidateConnections_TwoConnectedCurves_IsValid()
        {
            // Arrange - Two curves forming a proper path with 2 connections at shared point
            var grid = new GridState()
                .WithTile(0, new TileData(TileType.Curve, 0))  // 13-3
                .WithTile(3, new TileData(TileType.Curve, 1)); // 3-17
            
            var network = _calculator.CalculatePathNetwork(grid);
            
            // Act
            var result = _validator.ValidateConnections(network);
            
            // Assert
            Assert.IsTrue(result.IsValid, "Connected curves should be valid (shared point has 2 connections)");
        }
        
        #endregion
        
        #region IsPathPointValid
        
        [Test]
        public void IsPathPointValid_ValidPoint_ReturnsTrue()
        {
            // Arrange
            var network = new PathNetworkState();
            int entityPoint = GridConfiguration.EntityToPathPoint[0];
            network.ConnectPoints(entityPoint, 13);
            
            // Act
            bool isValid = _validator.IsPathPointValid(network, entityPoint);
            
            // Assert
            Assert.IsTrue(isValid);
        }
        
        [Test]
        public void IsPathPointValid_InvalidPoint_ReturnsFalse()
        {
            // Arrange
            var network = new PathNetworkState();
            int nonEntityPoint = 13;
            network.ConnectPoints(nonEntityPoint, 3); // Only 1 connection - invalid
            
            // Act
            bool isValid = _validator.IsPathPointValid(network, nonEntityPoint);
            
            // Assert
            Assert.IsFalse(isValid);
        }
        
        #endregion
        
        #region GetInvalidPathPoints
        
        [Test]
        public void GetInvalidPathPoints_ValidNetwork_ReturnsEmpty()
        {
            // Arrange
            var network = new PathNetworkState();
            
            // Act
            var invalidPoints = _validator.GetInvalidPathPoints(network).ToList();
            
            // Assert
            Assert.AreEqual(0, invalidPoints.Count);
        }
        
        [Test]
        public void GetInvalidPathPoints_InvalidNetwork_ReturnsInvalidPoints()
        {
            // Arrange
            var network = new PathNetworkState();
            network.ConnectPoints(13, 3); // Point 13 has 1 connection - invalid
            
            // Act
            var invalidPoints = _validator.GetInvalidPathPoints(network).ToList();
            
            // Assert
            Assert.Greater(invalidPoints.Count, 0);
            CollectionAssert.Contains(invalidPoints, 13);
            CollectionAssert.Contains(invalidPoints, 3);
        }
        
        [Test]
        public void GetInvalidPathPoints_MixedValidityNetwork_ReturnsOnlyInvalid()
        {
            // Arrange
            var network = new PathNetworkState();
            
            // Valid: two connections at non-entity point (create path 4-7-17)
            network.ConnectPoints(4, 7);
            network.ConnectPoints(7, 17);
            
            // Invalid: one connection at non-entity point
            network.ConnectPoints(13, 3);
            
            // Act
            var invalidPoints = _validator.GetInvalidPathPoints(network).ToList();
            
            // Assert
            CollectionAssert.Contains(invalidPoints, 13);
            CollectionAssert.Contains(invalidPoints, 3);
            CollectionAssert.DoesNotContain(invalidPoints, 4);
            CollectionAssert.DoesNotContain(invalidPoints, 7);
            CollectionAssert.DoesNotContain(invalidPoints, 17);
        }
        
        #endregion
        
        #region ValidationResult Tests
        
        [Test]
        public void ValidationResult_ContainsDetailedErrors()
        {
            // Arrange
            var network = new PathNetworkState();
            int entityPoint = GridConfiguration.EntityToPathPoint[0];
            
            // Create multiple errors
            network.ConnectPoints(entityPoint, 13);
            network.ConnectPoints(entityPoint, 3);  // Entity with 2 connections
            network.ConnectPoints(14, 4);           // Non-entity with 1 connection
            
            // Act
            var result = _validator.ValidateConnections(network);
            
            // Assert
            Assert.IsFalse(result.IsValid);
            Assert.Greater(result.Errors.Count, 0);
            
            foreach (var error in result.Errors)
            {
                Assert.IsNotNull(error.Message);
                Assert.GreaterOrEqual(error.PathPoint, 0);
                Assert.Less(error.PathPoint, GridConfiguration.TotalPathPoints);
            }
        }
        
        #endregion
    }
}
