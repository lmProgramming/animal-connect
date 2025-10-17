using System.Linq;
using Core.Configuration;
using Core.Logic;
using Core.Models;
using NUnit.Framework;

namespace Tests.Core.Logic
{
    [TestFixture]
    public class ConnectionValidatorTests
    {
        [SetUp]
        public void SetUp()
        {
            _validator = new ConnectionValidator();
            _calculator = new PathCalculator();
        }

        private ConnectionValidator _validator;
        private PathCalculator _calculator;

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
            var isValid = _validator.IsValid(network);

            // Assert
            Assert.IsTrue(isValid);
        }

        [Test]
        public void ValidateConnections_EntityWithOneConnection_IsInvalid()
        {
            // Arrange
            var network = new PathNetworkState();
            var entityPoint = GridConfiguration.EntityToPathPoint[0]; // Entity 0's path point
            network.ConnectPoints(entityPoint, 13); // Connect to a non-entity point

            // Act
            var result = _validator.ValidateConnections(network);

            // Assert
            // Entity is valid (1 connection allowed), but point 13 is a dead-end (1 connection)
            Assert.IsFalse(result.IsValid, "Non-entity point 13 has dead-end");
            Assert.IsTrue(result.Errors.Any(e => e.PathPoint == 13), "Point 13 should be invalid");
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
            var entityPoint = GridConfiguration.EntityToPathPoint[0];

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
            var entityPoint = GridConfiguration.EntityToPathPoint[1];

            // Create invalid scenario
            network.ConnectPoints(new[] { entityPoint, 13, 14, 4 });

            // Act
            var result = _validator.ValidateConnections(network);

            // Assert
            Assert.IsFalse(result.IsValid);
        }

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
        public void ValidateConnections_NonEntityWithTwoConnections_IsInvalid()
        {
            // Arrange
            var network = new PathNetworkState();
            var nonEntityPoint = 13; // Non-entity point

            network.ConnectPoints(nonEntityPoint, 3);
            network.ConnectPoints(nonEntityPoint, 17);

            // Act
            var result = _validator.ValidateConnections(network);

            // Assert
            // Point 13 is valid (2 connections), but points 3 and 17 are dead-ends (1 connection each)
            Assert.IsFalse(result.IsValid, "Points 3 and 17 are dead-ends");
            Assert.IsTrue(result.Errors.Any(e => e.PathPoint == 3), "Point 3 should be invalid");
            Assert.IsTrue(result.Errors.Any(e => e.PathPoint == 17), "Point 17 should be invalid");
        }

        [Test]
        public void ValidateConnections_NonEntityWithOneConnection_IsInvalid()
        {
            // Arrange
            var network = new PathNetworkState();
            var nonEntityPoint = 13;

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
            var nonEntityPoint = 13;

            network.ConnectPoints(nonEntityPoint, 0);
            network.ConnectPoints(nonEntityPoint, 3);
            network.ConnectPoints(nonEntityPoint, 14);

            // Act
            var result = _validator.ValidateConnections(network);

            // Assert
            Assert.IsFalse(result.IsValid, "Non-entity with 3 connections should be invalid");
        }

        [Test]
        public void ValidateConnections_SingleCurveTile_IsInvalid()
        {
            // Arrange - Single curve creates dead-ends at both connection points
            var grid = new GridState().WithTile(0, new TileData(TileType.Curve));
            var network = _calculator.CalculatePathNetwork(grid);

            // Act
            var result = _validator.ValidateConnections(network);

            // Assert
            Assert.IsFalse(result.IsValid, "Single curve creates dead-ends (1 connection each) at 2 points");
            Assert.Greater(result.ErrorCount, 0);
        }

        [Test]
        public void ValidateConnections_TwoCurvesTile_IsInvalid()
        {
            // Arrange - TwoCurves creates dead-ends at all 4 connection points
            var grid = new GridState().WithTile(4, new TileData(TileType.TwoCurves));
            var network = _calculator.CalculatePathNetwork(grid);

            // Act
            var result = _validator.ValidateConnections(network);

            // Assert
            Assert.IsFalse(result.IsValid, "Lone two-curves creates 4 dead-ends");
            Assert.Greater(result.ErrorCount, 0);
        }

        [Test]
        public void ValidateConnections_IntersectionTile_IsInvalid()
        {
            // Arrange - Single intersection creates dead ends (3 connections on non-entity points)
            var grid = new GridState().WithTile(4, new TileData(TileType.Intersection));
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
            var grid = new GridState().WithTile(4, new TileData(TileType.XIntersection));
            var network = _calculator.CalculatePathNetwork(grid);

            // Act
            var result = _validator.ValidateConnections(network);

            // Assert
            Assert.IsFalse(result.IsValid, "Lone X-intersection creates dead ends");
        }

        [Test]
        public void ValidateConnections_BridgeTile_IsInvalid()
        {
            // Arrange - Single bridge tile creates dead-ends at all 4 connection points
            var grid = new GridState().WithTile(4, new TileData(TileType.Bridge));
            var network = _calculator.CalculatePathNetwork(grid);

            // Act
            var result = _validator.ValidateConnections(network);

            // Assert
            Assert.IsFalse(result.IsValid, "Lone bridge creates dead-ends (1 connection each) at 4 non-entity points");
            Assert.Greater(result.ErrorCount, 0);
        }

        [Test]
        public void ValidateConnections_TwoConnectedCurves_IsInvalid()
        {
            // Arrange - Two curves forming a path. The shared point has 2 connections (valid),
            // but the endpoints still have dead-ends (1 connection each)
            var grid = new GridState()
                .WithTile(0, new TileData(TileType.Curve)) // 13-3
                .WithTile(3, new TileData(TileType.Curve, 1)); // 3-17

            var network = _calculator.CalculatePathNetwork(grid);

            // Act
            var result = _validator.ValidateConnections(network);

            // Assert
            Assert.IsFalse(result.IsValid, "Path has dead-ends at endpoints (13 and 17 each have 1 connection)");
            // Points 13 and 17 should be invalid (dead-ends), but point 3 should be valid (2 connections)
        }

        [Test]
        public void IsPathPointValid_ValidPoint_ReturnsTrue()
        {
            // Arrange
            var network = new PathNetworkState();
            var entityPoint = GridConfiguration.EntityToPathPoint[0];
            network.ConnectPoints(entityPoint, 13);

            // Act
            var isValid = _validator.IsPathPointValid(network, entityPoint);

            // Assert
            Assert.IsTrue(isValid);
        }

        [Test]
        public void IsPathPointValid_InvalidPoint_ReturnsFalse()
        {
            // Arrange
            var network = new PathNetworkState();
            var nonEntityPoint = 13;
            network.ConnectPoints(nonEntityPoint, 3); // Only 1 connection - invalid

            // Act
            var isValid = _validator.IsPathPointValid(network, nonEntityPoint);

            // Assert
            Assert.IsFalse(isValid);
        }

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

            // Create a valid closed loop: 4 → 7 → 17 → 18 → 4
            network.ConnectPoints(4, 7);
            network.ConnectPoints(7, 17);
            network.ConnectPoints(17, 18);
            network.ConnectPoints(18, 4);

            // Create invalid dead-ends
            network.ConnectPoints(13, 3);

            // Act
            var invalidPoints = _validator.GetInvalidPathPoints(network).ToList();

            // Assert
            // Invalid points (dead-ends)
            CollectionAssert.Contains(invalidPoints, 13);
            CollectionAssert.Contains(invalidPoints, 3);

            // Valid points (all have 2 connections in the loop)
            CollectionAssert.DoesNotContain(invalidPoints, 4);
            CollectionAssert.DoesNotContain(invalidPoints, 7);
            CollectionAssert.DoesNotContain(invalidPoints, 17);
            CollectionAssert.DoesNotContain(invalidPoints, 18);
        }

        [Test]
        public void ValidateConnections_DeadEnd_IsInvalid()
        {
            // Arrange - Create a dead-end scenario where a path terminates at a non-entity point
            var network = new PathNetworkState();
            var nonEntityPoint = 13;
            var anotherNonEntityPoint = 17;

            // Connect to create: path -> nonEntityPoint (dead end)
            network.ConnectPoints(nonEntityPoint, anotherNonEntityPoint);

            // Act
            var result = _validator.ValidateConnections(network);

            // Assert
            Assert.IsFalse(result.IsValid, "Dead-end violates 'road in, road out' rule");
            Assert.Greater(result.ErrorCount, 0, "Dead-ends should be errors, not warnings");
            Assert.IsTrue(result.Errors.Any(e => e.PathPoint == nonEntityPoint && e.Severity == ErrorSeverity.Error));
            Assert.IsTrue(result.Errors.Any(e =>
                e.PathPoint == anotherNonEntityPoint && e.Severity == ErrorSeverity.Error));
        }

        [Test]
        public void ValidateConnections_ThroughPath_IsInvalid()
        {
            // Arrange - Path has dead-ends at endpoints
            var network = new PathNetworkState();
            var point1 = 13;
            var point2 = 17;
            var point3 = 14;

            // Create a path: point1 -> point2 -> point3
            network.ConnectPoints(point1, point2);
            network.ConnectPoints(point2, point3);

            // Act
            var result = _validator.ValidateConnections(network);

            // Assert
            Assert.IsFalse(result.IsValid, "Path endpoints are dead-ends");
            // Point2 should have exactly 2 connections (road in, road out) - VALID
            Assert.AreEqual(2, network.GetConnectionCount(point2));
            // But points 1 and 3 have dead-ends - INVALID
            Assert.IsTrue(result.Errors.Any(e => e.PathPoint == point1 || e.PathPoint == point3));
        }

        [Test]
        public void ValidateConnections_ComplexValidPath_IsInvalid()
        {
            // Arrange - Path endpoints still have dead-ends
            var network = new PathNetworkState();

            // Create paths with dead-ends at endpoints
            // Path 1: 3 -> 13 -> 14 (points 3 and 14 are dead-ends)
            network.ConnectPoints(3, 13);
            network.ConnectPoints(13, 14);

            // Path 2: 17 -> 18 -> 5 (points 17 and 5 are dead-ends)
            network.ConnectPoints(17, 18);
            network.ConnectPoints(18, 5);

            // Act
            var result = _validator.ValidateConnections(network);

            // Assert
            Assert.IsFalse(result.IsValid, "Paths have dead-ends at endpoints");
            // Points 3, 14, 17, 5 should be invalid (dead-ends)
            // Points 13, 18 should be valid (2 connections each - road in, road out)
        }

        [Test]
        public void ValidateConnections_EntityConnectedToDeadEnd_IsInvalid()
        {
            // Arrange - Entity connected to a path that ends at a non-entity point
            var network = new PathNetworkState();
            var entityPoint = GridConfiguration.EntityToPathPoint[0];
            var nonEntityPoint = 13;

            // Entity -> nonEntityPoint (dead end at nonEntityPoint)
            network.ConnectPoints(entityPoint, nonEntityPoint);

            // Act
            var result = _validator.ValidateConnections(network);

            // Assert
            // The entity is valid (1 connection allowed), but the non-entity point is invalid (dead-end)
            Assert.IsFalse(result.IsValid);
            Assert.IsTrue(result.Errors.Any(e => e.PathPoint == nonEntityPoint));
        }

        [Test]
        public void ValidateConnections_EntityToEntityViaTwoPoints_IsValid()
        {
            // Arrange - Two entities connected through a valid path
            // This IS valid because entities can have 1 connection, and all intermediate
            // points have 2 connections (road in, road out)
            var network = new PathNetworkState();
            var entity1Point = GridConfiguration.EntityToPathPoint[0];
            var entity2Point = GridConfiguration.EntityToPathPoint[1];
            var intermediatePoint1 = 13;
            var intermediatePoint2 = 14;

            // entity1 -> intermediatePoint1 -> intermediatePoint2 -> entity2
            network.ConnectPoints(entity1Point, intermediatePoint1);
            network.ConnectPoints(intermediatePoint1, intermediatePoint2);
            network.ConnectPoints(intermediatePoint2, entity2Point);

            // Act
            var result = _validator.ValidateConnections(network);

            // Assert
            Assert.IsTrue(result.IsValid, "Entities connected via valid intermediate points should be valid");
            Assert.AreEqual(0, result.ErrorCount);

            // Verify intermediate points have exactly 2 connections (road in, road out)
            Assert.AreEqual(2, network.GetConnectionCount(intermediatePoint1));
            Assert.AreEqual(2, network.GetConnectionCount(intermediatePoint2));

            // Entities have 1 connection each (valid for entities)
            Assert.AreEqual(1, network.GetConnectionCount(entity1Point));
            Assert.AreEqual(1, network.GetConnectionCount(entity2Point));
        }

        [Test]
        public void ValidateConnections_ClosedLoop_IsValid()
        {
            // Arrange - Create a closed loop where all points have 2 connections
            var network = new PathNetworkState();
            var point1 = 13;
            var point2 = 14;
            var point3 = 17;
            var point4 = 18;

            // Create a square loop: point1 -> point2 -> point3 -> point4 -> point1
            network.ConnectPoints(point1, point2);
            network.ConnectPoints(point2, point3);
            network.ConnectPoints(point3, point4);
            network.ConnectPoints(point4, point1);

            // Act
            var result = _validator.ValidateConnections(network);

            // Assert
            Assert.IsTrue(result.IsValid, "Closed loop with all points having 2 connections is valid");
            Assert.AreEqual(0, result.ErrorCount);

            // All points should have exactly 2 connections
            Assert.AreEqual(2, network.GetConnectionCount(point1));
            Assert.AreEqual(2, network.GetConnectionCount(point2));
            Assert.AreEqual(2, network.GetConnectionCount(point3));
            Assert.AreEqual(2, network.GetConnectionCount(point4));
        }

        [Test]
        public void ValidationResult_ContainsDetailedErrors()
        {
            // Arrange
            var network = new PathNetworkState();
            var entityPoint = GridConfiguration.EntityToPathPoint[0];

            // Create multiple errors
            network.ConnectPoints(entityPoint, 13);
            network.ConnectPoints(entityPoint, 3); // Entity with 2 connections
            network.ConnectPoints(14, 4); // Non-entity with 1 connection

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
    }
}