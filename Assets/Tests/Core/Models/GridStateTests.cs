using System;
using System.Linq;
using Core.Models;
using NUnit.Framework;

namespace Tests.Core.Models
{
    [TestFixture]
    public class GridStateTests
    {
        [SetUp]
        public void SetUp()
        {
            _gridState = new GridState();
        }

        private GridState _gridState;

        [Test]
        public void Constructor_CreatesEmptyGrid()
        {
            // Arrange & Act
            var grid = new GridState();

            // Assert
            for (var i = 0; i < GridState.TotalSlots; i++) Assert.IsNull(grid.GetTile(i), $"Slot {i} should be empty");
        }

        [Test]
        public void Constants_HaveCorrectValues()
        {
            Assert.AreEqual(3, GridState.GridSize);
            Assert.AreEqual(9, GridState.TotalSlots);
        }

        [Test]
        public void GetTile_EmptySlot_ReturnsNull()
        {
            // Assert
            Assert.IsNull(_gridState.GetTile(0));
            Assert.IsNull(_gridState.GetTile(4));
            Assert.IsNull(_gridState.GetTile(8));
        }

        [Test]
        public void GetTile_InvalidPosition_ThrowsException()
        {
            // Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => _gridState.GetTile(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => _gridState.GetTile(9));
            Assert.Throws<ArgumentOutOfRangeException>(() => _gridState.GetTile(100));
        }

        [Test]
        public void GetTile_ByCoordinates_ReturnsCorrectTile()
        {
            // Arrange
            var tile = new TileData(TileType.Curve, 1);
            _gridState = _gridState.WithTile(4, tile); // Center position (1,1)

            // Act
            var retrieved = _gridState.GetTile(1, 1);

            // Assert
            Assert.IsTrue(retrieved.HasValue);
            Assert.AreEqual(tile, retrieved.Value);
        }

        [Test]
        public void GetTile_ByCoordinates_InvalidCoords_ThrowsException()
        {
            // Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => _gridState.GetTile(-1, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => _gridState.GetTile(0, 3));
            Assert.Throws<ArgumentOutOfRangeException>(() => _gridState.GetTile(3, 3));
        }

        [Test]
        public void WithTile_CreatesNewGridState()
        {
            // Arrange
            var tile = new TileData(TileType.Curve);

            // Act
            var newGrid = _gridState.WithTile(0, tile);

            // Assert
            Assert.AreNotSame(_gridState, newGrid, "Should create new instance");
            Assert.IsNull(_gridState.GetTile(0), "Original grid should be unchanged");
            Assert.IsTrue(newGrid.GetTile(0).HasValue, "New grid should have tile");
        }

        [Test]
        public void WithTile_PlacesTileAtCorrectPosition()
        {
            // Arrange
            var tile = new TileData(TileType.Intersection, 2);

            // Act
            var newGrid = _gridState.WithTile(5, tile);

            // Assert
            Assert.AreEqual(tile, newGrid.GetTile(5)!.Value);
        }

        [Test]
        public void WithTile_CanOverwriteExistingTile()
        {
            // Arrange
            var tile1 = new TileData(TileType.Curve);
            var tile2 = new TileData(TileType.Bridge, 1);
            _gridState = _gridState.WithTile(3, tile1);

            // Act
            var newGrid = _gridState.WithTile(3, tile2);

            // Assert
            Assert.AreEqual(tile2, newGrid.GetTile(3)!.Value);
        }

        [Test]
        public void WithTile_InvalidPosition_ThrowsException()
        {
            // Arrange
            var tile = new TileData(TileType.Curve);

            // Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => _gridState.WithTile(-1, tile));
            Assert.Throws<ArgumentOutOfRangeException>(() => _gridState.WithTile(9, tile));
        }

        [Test]
        public void WithoutTile_RemovesTile()
        {
            // Arrange
            var tile = new TileData(TileType.Curve);
            _gridState = _gridState.WithTile(2, tile);

            // Act
            var newGrid = _gridState.WithoutTile(2);

            // Assert
            Assert.IsTrue(_gridState.GetTile(2).HasValue, "Original should have tile");
            Assert.IsNull(newGrid.GetTile(2), "New grid should not have tile");
        }

        [Test]
        public void WithoutTile_OnEmptySlot_DoesNotThrow()
        {
            // Act & Assert
            Assert.DoesNotThrow(() => _gridState.WithoutTile(0));
        }

        [Test]
        public void WithRotation_RotatesTile()
        {
            // Arrange
            var tile = new TileData(TileType.Curve);
            _gridState = _gridState.WithTile(4, tile);

            // Act
            var newGrid = _gridState.WithRotation(4, 2);

            // Assert
            Assert.AreEqual(0, _gridState.GetTile(4)!.Value.Rotation, "Original unchanged");
            Assert.AreEqual(2, newGrid.GetTile(4)!.Value.Rotation);
            Assert.AreEqual(TileType.Curve, newGrid.GetTile(4)!.Value.Type);
        }

        [Test]
        public void WithRotation_OnEmptySlot_ThrowsException()
        {
            // Assert
            var ex = Assert.Throws<InvalidOperationException>(() =>
                _gridState.WithRotation(0, 1));
            StringAssert.Contains("Cannot rotate empty slot", ex.Message);
        }

        [Test]
        public void WithSwap_SwapsTwoTiles()
        {
            // Arrange
            var tile1 = new TileData(TileType.Curve);
            var tile2 = new TileData(TileType.Bridge, 1);
            _gridState = _gridState.WithTile(0, tile1).WithTile(8, tile2);

            // Act
            var newGrid = _gridState.WithSwap(0, 8);

            // Assert
            Assert.AreEqual(tile1, _gridState.GetTile(0)!.Value, "Original unchanged");
            Assert.AreEqual(tile2, _gridState.GetTile(8)!.Value, "Original unchanged");

            Assert.AreEqual(tile2, newGrid.GetTile(0)!.Value, "Swapped");
            Assert.AreEqual(tile1, newGrid.GetTile(8)!.Value, "Swapped");
        }

        [Test]
        public void WithSwap_CanSwapTileWithEmptySlot()
        {
            // Arrange
            var tile = new TileData(TileType.Intersection, 2);
            _gridState = _gridState.WithTile(3, tile);

            // Act
            var newGrid = _gridState.WithSwap(3, 5);

            // Assert
            Assert.IsNull(newGrid.GetTile(3));
            Assert.AreEqual(tile, newGrid.GetTile(5)!.Value);
        }

        [Test]
        public void WithSwap_SwappingEmptySlots_Works()
        {
            // Act
            var newGrid = _gridState.WithSwap(1, 7);

            // Assert
            Assert.IsNull(newGrid.GetTile(1));
            Assert.IsNull(newGrid.GetTile(7));
        }

        [Test]
        public void WithSwap_SamePosition_Works()
        {
            // Arrange
            var tile = new TileData(TileType.Curve);
            _gridState = _gridState.WithTile(4, tile);

            // Act
            var newGrid = _gridState.WithSwap(4, 4);

            // Assert
            Assert.AreEqual(tile, newGrid.GetTile(4)!.Value);
        }

        [Test]
        public void GetOccupiedSlots_EmptyGrid_ReturnsEmpty()
        {
            // Act
            var occupied = _gridState.GetOccupiedSlots().ToList();

            // Assert
            Assert.AreEqual(0, occupied.Count);
        }

        [Test]
        public void GetOccupiedSlots_ReturnsAllTiles()
        {
            // Arrange
            var tile1 = new TileData(TileType.Curve);
            var tile2 = new TileData(TileType.Bridge, 1);
            var tile3 = new TileData(TileType.Intersection, 2);

            _gridState = _gridState
                .WithTile(1, tile1)
                .WithTile(4, tile2)
                .WithTile(7, tile3);

            // Act
            var occupied = _gridState.GetOccupiedSlots().ToList();

            // Assert
            Assert.AreEqual(3, occupied.Count);

            Assert.IsTrue(occupied.Any(x => x.position == 1 && x.tile == tile1));
            Assert.IsTrue(occupied.Any(x => x.position == 4 && x.tile == tile2));
            Assert.IsTrue(occupied.Any(x => x.position == 7 && x.tile == tile3));
        }

        [Test]
        public void GetEmptySlots_FullGrid_ReturnsEmpty()
        {
            // Arrange
            var tile = new TileData(TileType.Curve);
            for (var i = 0; i < GridState.TotalSlots; i++) _gridState = _gridState.WithTile(i, tile);

            // Act
            var empty = _gridState.GetEmptySlots().ToList();

            // Assert
            Assert.AreEqual(0, empty.Count);
        }

        [Test]
        public void GetEmptySlots_EmptyGrid_ReturnsAllSlots()
        {
            // Act
            var empty = _gridState.GetEmptySlots().ToList();

            // Assert
            Assert.AreEqual(GridState.TotalSlots, empty.Count);
            for (var i = 0; i < GridState.TotalSlots; i++) CollectionAssert.Contains(empty, i);
        }

        [Test]
        public void GetEmptySlots_PartiallyFilled_ReturnsOnlyEmpty()
        {
            // Arrange
            _gridState = _gridState
                .WithTile(0, new TileData(TileType.Curve))
                .WithTile(4, new TileData(TileType.Bridge, 1))
                .WithTile(8, new TileData(TileType.Intersection, 2));

            // Act
            var empty = _gridState.GetEmptySlots().ToList();

            // Assert
            Assert.AreEqual(6, empty.Count);
            CollectionAssert.DoesNotContain(empty, 0);
            CollectionAssert.DoesNotContain(empty, 4);
            CollectionAssert.DoesNotContain(empty, 8);
        }

        [Test]
        public void IsEmpty_NewGrid_ReturnsTrue()
        {
            // Assert
            Assert.IsTrue(_gridState.IsEmpty);
        }

        [Test]
        public void IsEmpty_GridWithTiles_ReturnsFalse()
        {
            // Arrange
            _gridState = _gridState.WithTile(0, new TileData(TileType.Curve));

            // Assert
            Assert.IsFalse(_gridState.IsEmpty);
        }

        [Test]
        public void IsFull_EmptyGrid_ReturnsFalse()
        {
            // Assert
            Assert.IsFalse(_gridState.IsFull);
        }

        [Test]
        public void IsFull_CompletelyFilledGrid_ReturnsTrue()
        {
            // Arrange
            var tile = new TileData(TileType.Curve);
            for (var i = 0; i < GridState.TotalSlots; i++) _gridState = _gridState.WithTile(i, tile);

            // Assert
            Assert.IsTrue(_gridState.IsFull);
        }

        [Test]
        public void IsFull_PartiallyFilled_ReturnsFalse()
        {
            // Arrange
            _gridState = _gridState
                .WithTile(0, new TileData(TileType.Curve))
                .WithTile(1, new TileData(TileType.Bridge, 1));

            // Assert
            Assert.IsFalse(_gridState.IsFull);
        }

        [Test]
        public void TileCount_EmptyGrid_ReturnsZero()
        {
            // Assert
            Assert.AreEqual(0, _gridState.TileCount);
        }

        [Test]
        public void TileCount_ReturnsCorrectCount()
        {
            // Arrange
            _gridState = _gridState
                .WithTile(0, new TileData(TileType.Curve))
                .WithTile(3, new TileData(TileType.Bridge, 1))
                .WithTile(7, new TileData(TileType.Intersection, 2));

            // Assert
            Assert.AreEqual(3, _gridState.TileCount);
        }

        [Test]
        public void ComplexScenario_MultipleOperationsChained()
        {
            // Arrange
            var tile1 = new TileData(TileType.Curve);
            var tile2 = new TileData(TileType.Bridge, 1);
            var tile3 = new TileData(TileType.Intersection, 2);

            // Act - Chain multiple operations
            var result = _gridState
                .WithTile(0, tile1)
                .WithTile(4, tile2)
                .WithRotation(4, 0)
                .WithTile(8, tile3)
                .WithSwap(0, 8)
                .WithoutTile(4);

            // Assert
            Assert.AreEqual(tile3, result.GetTile(0)!.Value);
            Assert.IsNull(result.GetTile(4));
            Assert.AreEqual(tile1, result.GetTile(8)!.Value);
            Assert.AreEqual(2, result.TileCount);
        }

        [Test]
        public void Immutability_OriginalRemainsUnchangedAfterOperations()
        {
            // Arrange
            var original = new GridState();
            var tile = new TileData(TileType.Curve);

            // Act - Perform multiple operations
            var modified = original
                .WithTile(0, tile)
                .WithTile(1, tile)
                .WithRotation(0, 2);

            // Assert - Original is unchanged
            Assert.IsTrue(original.IsEmpty);
            Assert.AreEqual(0, original.TileCount);

            // Modified has changes
            Assert.AreEqual(2, modified.TileCount);
        }
    }
}