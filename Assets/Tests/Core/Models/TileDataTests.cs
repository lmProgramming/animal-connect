using Core.Models;
using NUnit.Framework;

namespace Tests.Core.Models
{
    [TestFixture]
    public class TileDataTests
    {
        [Test]
        public void Constructor_SetsTypeAndRotation()
        {
            // Act
            var tile = new TileData(TileType.Curve, 2);

            // Assert
            Assert.AreEqual(TileType.Curve, tile.Type);
            Assert.AreEqual(2, tile.Rotation);
        }

        [Test]
        public void Constructor_DefaultRotationIsZero()
        {
            // Act
            var tile = new TileData(TileType.Intersection);

            // Assert
            Assert.AreEqual(0, tile.Rotation);
        }

        [Test]
        public void WithRotation_CreatesNewTileWithDifferentRotation()
        {
            // Arrange
            var original = new TileData(TileType.Curve);

            // Act
            var rotated = original.WithRotation(2);

            // Assert
            Assert.AreEqual(0, original.Rotation, "Original should be unchanged");
            Assert.AreEqual(2, rotated.Rotation);
            Assert.AreEqual(TileType.Curve, rotated.Type);
        }

        [Test]
        public void GetMaxRotations_ReturnsCorrectValueForEachTileType()
        {
            Assert.AreEqual(4, new TileData(TileType.Curve).GetMaxRotations());
            Assert.AreEqual(2, new TileData(TileType.TwoCurves).GetMaxRotations());
            Assert.AreEqual(4, new TileData(TileType.Intersection).GetMaxRotations());
            Assert.AreEqual(1, new TileData(TileType.XIntersection).GetMaxRotations());
            Assert.AreEqual(2, new TileData(TileType.Bridge).GetMaxRotations());
        }

        [Test]
        public void GetConnections_CurveTile_ConnectsRightToBottom()
        {
            // Arrange
            var tile = new TileData(TileType.Curve);

            // Act
            var connections = tile.GetConnections();

            // Assert
            Assert.AreEqual(1, connections.Count);
            var conn = connections[0];
            Assert.AreEqual(2, conn.ConnectedSides.Count);
            CollectionAssert.Contains(conn.ConnectedSides, 1); // Right
            CollectionAssert.Contains(conn.ConnectedSides, 2); // Bottom
        }

        [Test]
        public void GetConnections_TwoCurvesTile_HasTwoSeparateConnections()
        {
            // Arrange
            var tile = new TileData(TileType.TwoCurves);

            // Act
            var connections = tile.GetConnections();

            // Assert
            Assert.AreEqual(2, connections.Count);

            // First curve: top-left
            CollectionAssert.AreEquivalent(new[] { 0, 3 }, connections[0].ConnectedSides);

            // Second curve: right-bottom
            CollectionAssert.AreEquivalent(new[] { 1, 2 }, connections[1].ConnectedSides);
        }

        [Test]
        public void GetConnections_IntersectionTile_ConnectsThreeSides()
        {
            // Arrange
            var tile = new TileData(TileType.Intersection);

            // Act
            var connections = tile.GetConnections();

            // Assert
            Assert.AreEqual(1, connections.Count);
            var conn = connections[0];
            Assert.AreEqual(3, conn.ConnectedSides.Count);
            CollectionAssert.AreEquivalent(new[] { 0, 1, 2 }, conn.ConnectedSides); // Top, Right, Bottom
        }

        [Test]
        public void GetConnections_XIntersectionTile_ConnectsAllFourSides()
        {
            // Arrange
            var tile = new TileData(TileType.XIntersection);

            // Act
            var connections = tile.GetConnections();

            // Assert
            Assert.AreEqual(1, connections.Count);
            var conn = connections[0];
            Assert.AreEqual(4, conn.ConnectedSides.Count);
            CollectionAssert.AreEquivalent(new[] { 0, 1, 2, 3 }, conn.ConnectedSides);
        }

        [Test]
        public void GetConnections_BridgeTile_HasTwoSeparateStraightConnections()
        {
            // Arrange
            var tile = new TileData(TileType.Bridge);

            // Act
            var connections = tile.GetConnections();

            // Assert
            Assert.AreEqual(2, connections.Count);

            // Vertical bridge: top-bottom
            CollectionAssert.AreEquivalent(new[] { 0, 2 }, connections[0].ConnectedSides);

            // Horizontal bridge: right-left
            CollectionAssert.AreEquivalent(new[] { 1, 3 }, connections[1].ConnectedSides);
        }

        [Test]
        public void GetConnections_CurveTileRotated90_ConnectsBottomToLeft()
        {
            // Arrange
            var tile = new TileData(TileType.Curve, 1);

            // Act
            var connections = tile.GetConnections();

            // Assert
            Assert.AreEqual(1, connections.Count);
            var conn = connections[0];
            CollectionAssert.AreEquivalent(new[] { 2, 3 }, conn.ConnectedSides); // Bottom and Left
        }

        [Test]
        public void GetConnections_IntersectionRotated180_ConnectsRightBottomLeft()
        {
            // Arrange
            var tile = new TileData(TileType.Intersection, 2);

            // Act
            var connections = tile.GetConnections();

            // Assert
            Assert.AreEqual(1, connections.Count);
            var conn = connections[0];
            CollectionAssert.AreEquivalent(new[] { 1, 2, 3 }, conn.ConnectedSides); // Right, Bottom, Left
        }

        [Test]
        public void Equals_SameTileTypeAndRotation_ReturnsTrue()
        {
            // Arrange
            var tile1 = new TileData(TileType.Curve, 1);
            var tile2 = new TileData(TileType.Curve, 1);

            // Assert
            Assert.IsTrue(tile1.Equals(tile2));
            Assert.IsTrue(tile1 == tile2);
            Assert.IsFalse(tile1 != tile2);
        }

        [Test]
        public void Equals_DifferentRotation_ReturnsFalse()
        {
            // Arrange
            var tile1 = new TileData(TileType.Curve);
            var tile2 = new TileData(TileType.Curve, 1);

            // Assert
            Assert.IsFalse(tile1.Equals(tile2));
            Assert.IsFalse(tile1 == tile2);
            Assert.IsTrue(tile1 != tile2);
        }

        [Test]
        public void Equals_DifferentType_ReturnsFalse()
        {
            // Arrange
            var tile1 = new TileData(TileType.Curve);
            var tile2 = new TileData(TileType.Intersection);

            // Assert
            Assert.IsFalse(tile1.Equals(tile2));
        }

        [Test]
        public void GetHashCode_EqualTiles_HaveSameHashCode()
        {
            // Arrange
            var tile1 = new TileData(TileType.Bridge, 1);
            var tile2 = new TileData(TileType.Bridge, 1);

            // Assert
            Assert.AreEqual(tile1.GetHashCode(), tile2.GetHashCode());
        }

        [Test]
        public void ToString_ReturnsReadableFormat()
        {
            // Arrange
            var tile = new TileData(TileType.Curve, 2);

            // Act
            var str = tile.ToString();

            // Assert
            Assert.AreEqual("Curve(R2)", str);
        }
    }
}