using System;
using System.Collections.Generic;
using Solver;
using UnityEngine;

namespace Quest
{
    [Serializable]
    public class Quest
    {
        public List<EntitiesToConnect> entitiesToConnectIDs = new();
        public List<Vector2Int> pathsToDisconnectIndexes = new();

        public Quest(List<EntitiesToConnect> entitiesToConnectIDs, List<Vector2Int> pathsToDisconnectIndexes)
        {
            this.entitiesToConnectIDs = entitiesToConnectIDs;
            this.pathsToDisconnectIndexes = pathsToDisconnectIndexes;
        }

        public bool CheckIfCompleted(Entity[] entities)
        {
            var entitiesToConnectIDsPathNums = new int[entitiesToConnectIDs.Count];

            if (entitiesToConnectIDs.Count == 0) Debug.LogError("Empty quest");

            for (var i = 0; i < entitiesToConnectIDs.Count; i++)
            {
                if (entitiesToConnectIDs[i].onlyAClump) continue;

                var pathNum = entities[entitiesToConnectIDs[i].entitiesIDs[0]].pathPoint.pathNum;
                entitiesToConnectIDsPathNums[i] = pathNum;

                if (pathNum == -1)
                    if (entitiesToConnectIDs[i].entitiesIDs.Count > 1)
                        return false;

                for (var j = 1; j < entitiesToConnectIDs[i].entitiesIDs.Count; j++)
                    if (entities[entitiesToConnectIDs[i].entitiesIDs[j]].pathPoint.pathNum != pathNum)
                        return false;
            }

            for (var i = 0; i < pathsToDisconnectIndexes.Count; i++)
            {
                var entitiesPathInd1PathNum = entitiesToConnectIDsPathNums[pathsToDisconnectIndexes[i].x];
                var entitiesPathInd2PathNum = entitiesToConnectIDsPathNums[pathsToDisconnectIndexes[i].y];
                if (entitiesPathInd1PathNum == entitiesPathInd2PathNum && entitiesPathInd1PathNum != -1 &&
                    entitiesPathInd2PathNum != -1) return false;
            }

            // Debug.Log("YAY");

            return true;
        }
    }
}