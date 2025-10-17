using System;
using System.Collections.Generic;
using UnityEngine;
using Core.Models;
using Core.Logic;

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

        /// <summary>
        /// Checks if the quest is completed using the new Core system.
        /// </summary>
        public bool CheckIfCompleted(PathNetworkState pathNetwork)
        {
            if (pathNetwork == null)
            {
                Debug.LogError("Quest: PathNetworkState is null!");
                return false;
            }

            // Convert this Quest to QuestData and use QuestEvaluator
            var questData = ToQuestData();
            var evaluator = new QuestEvaluator();
            var result = evaluator.EvaluateQuest(questData, pathNetwork);

            return result.IsComplete;
        }

        /// <summary>
        /// Converts this Quest to the new QuestData format.
        /// </summary>
        public QuestData ToQuestData()
        {
            var entityGroups = new List<EntityGroup>();

            foreach (var group in entitiesToConnectIDs)
            {
                if (group.entitiesIDs.Count > 0)
                {
                    entityGroups.Add(new EntityGroup(
                        group.entitiesIDs.ToArray(),
                        group.onlyAClump
                    ));
                }
            }

            // Convert disconnect requirements
            var disconnectRequirements = new List<DisconnectRequirement>();
            foreach (var disconnect in pathsToDisconnectIndexes)
            {
                disconnectRequirements.Add(new DisconnectRequirement(disconnect.x, disconnect.y));
            }

            return new QuestData(entityGroups, disconnectRequirements);
        }
    }
}