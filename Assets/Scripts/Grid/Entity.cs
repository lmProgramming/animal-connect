using System;

namespace Solver
{
    [Serializable]
    public class Entity
    {
        public int entityIndex;
        public PathPoint pathPoint;

        public Entity(int entityIndex, PathPoint pathPoint)
        {
            this.entityIndex = entityIndex;
            this.pathPoint = pathPoint;
        }
    }
}