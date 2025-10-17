using NUnit.Framework;
using Solver;

namespace Tests.EditMode
{
    /// <summary>
    /// Tests for Entity class
    /// </summary>
    public class EntityTests
    {
        [Test]
        public void Entity_Constructor_SetsEntityIndexAndPathPoint()
        {
            // Arrange
            var pathPoint = new PathPoint(0, 0);

            // Act
            var entity = new Entity(5, pathPoint);

            // Assert
            Assert.AreEqual(5, entity.entityIndex, "Entity index should be set correctly");
            Assert.AreSame(pathPoint, entity.pathPoint, "Path point reference should be set correctly");
        }

        [Test]
        public void Entity_ImplementsIEntity_Interface()
        {
            // Arrange
            var pathPoint = new PathPoint(0, 0);
            var entity = new Entity(3, pathPoint);

            // Act
            IEntity iEntity = entity;

            // Assert
            Assert.AreEqual(3, iEntity.EntityIndex, "IEntity.EntityIndex should return correct value");
            Assert.AreSame(pathPoint, iEntity.PathPoint, "IEntity.PathPoint should return correct reference");
        }

        [Test]
        public void Entity_PathPoint_CanBeUpdated()
        {
            // Arrange
            var pathPoint1 = new PathPoint(0, 0);
            var pathPoint2 = new PathPoint(1, 0);
            var entity = new Entity(7, pathPoint1);

            // Act
            entity.pathPoint = pathPoint2;

            // Assert
            Assert.AreSame(pathPoint2, entity.pathPoint, "Path point should be updated");
        }
    }
}
