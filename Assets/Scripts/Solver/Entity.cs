using System;

namespace Solver
{
    [Serializable]
    public class Entity : IEntity
    {
        public int entityIndex;
        public PathPoint pathPoint;

        public int EntityIndex => entityIndex;
        public IPathPoint PathPoint => pathPoint;

        public Entity(int entityIndex, PathPoint pathPoint)
        {
            this.entityIndex = entityIndex;
            this.pathPoint = pathPoint;
        }
    }
}