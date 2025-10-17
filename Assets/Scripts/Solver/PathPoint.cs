using System;
using UnityEngine;

namespace Solver
{
    [Serializable]
    public class PathPoint
    {
        public int pathNum = -1;
        public int entityIndex = -1;

        [SerializeField] private int connectionsNumber;
        
        // Public accessor for migration compatibility
        public int ConnectionsNumber => connectionsNumber;

        public PathPoint(int entityIndex, int connectionsNumber)
        {
            this.entityIndex = entityIndex;
            this.connectionsNumber = connectionsNumber;

            Setup();
        }

        public void UpdatePathNum(int newNum)
        {
            pathNum = newNum;
        }

        public void Setup()
        {
            if (entityIndex != -1) connectionsNumber = 1;
        }

        public void ResetConectionsNumber()
        {
            if (entityIndex != -1)
                connectionsNumber = 1;
            else
                connectionsNumber = 0;
        }

        public void RaiseConnectionsNumber()
        {
            connectionsNumber++;
        }

        public bool CheckIfPathOnTopValid()
        {
            if (entityIndex != -1) return true;

            return connectionsNumber == 0 || connectionsNumber == 2;
        }
    }
}