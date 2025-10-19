using System.Collections.Generic;
using System.Linq;
using Core.Configuration;
using Core.Models;

namespace Core.Logic
{
    /// <summary>
    ///     Evaluates whether quest requirements are satisfied.
    ///     Pure functions - no side effects.
    /// </summary>
    public class QuestEvaluator
    {
        /// <summary>
        ///     Evaluates if the current path configuration satisfies all quest requirements.
        /// </summary>
        public QuestResult EvaluateQuest(QuestData quest, PathNetworkState network)
        {
            // First check if all "must connect" requirements are met
            var connectionResults = new List<(int groupIndex, bool satisfied, string message)>();

            for (var i = 0; i < quest.EntitiesToConnect.Count; i++)
            {
                var group = quest.EntitiesToConnect[i];
                var result = CheckGroupConnected(group, network, i);
                connectionResults.Add(result);

                if (!result.satisfied && !group.OnlyAClump)
                    return QuestResult.Incomplete($"Group {i}: {result.message}");
            }

            // Then check if all "must disconnect" requirements are met
            foreach (var disconnectReq in quest.PathsToDisconnect)
            {
                var group1 = quest.EntitiesToConnect[disconnectReq.GroupIndex1];
                var group2 = quest.EntitiesToConnect[disconnectReq.GroupIndex2];

                if (!CheckGroupsDisconnected(group1, group2, network))
                    return QuestResult.Failed(
                        $"Groups {disconnectReq.GroupIndex1} and {disconnectReq.GroupIndex2} must not be connected");
            }

            // All requirements satisfied!
            return QuestResult.Success();
        }

        /// <summary>
        ///     Quick check if quest is complete - just returns true/false.
        /// </summary>
        public bool IsQuestComplete(QuestData quest, PathNetworkState network)
        {
            return EvaluateQuest(quest, network).IsSuccessful;
        }

        /// <summary>
        ///     Gets the completion percentage (0.0 to 1.0) for progress tracking.
        /// </summary>
        public float GetCompletionProgress(QuestData quest, PathNetworkState network)
        {
            var totalRequirements = quest.EntitiesToConnect.Count + quest.PathsToDisconnect.Count;
            if (totalRequirements == 0)
                return 1.0f;

            var satisfied = 0;

            // Check connection requirements
            foreach (var group in quest.EntitiesToConnect)
                if (!group.OnlyAClump && CheckGroupConnected(group, network, 0).satisfied)
                    satisfied++;

            // Check disconnection requirements
            foreach (var disconnectReq in quest.PathsToDisconnect)
            {
                var group1 = quest.EntitiesToConnect[disconnectReq.GroupIndex1];
                var group2 = quest.EntitiesToConnect[disconnectReq.GroupIndex2];

                if (CheckGroupsDisconnected(group1, group2, network)) satisfied++;
            }

            return (float)satisfied / totalRequirements;
        }

        /// <summary>
        ///     Checks if all entities in a group are connected to the same path.
        /// </summary>
        private (int groupIndex, bool satisfied, string message) CheckGroupConnected(
            EntityGroup group,
            PathNetworkState network,
            int groupIndex)
        {
            if (group.OnlyAClump) return (groupIndex, true, "Clump group (no connection requirement)");

            if (group.EntityIds.Count == 0) return (groupIndex, false, "Empty group");

            if (group.EntityIds.Count == 1) return (groupIndex, true, "Single entity (automatically satisfied)");

            // Get path points for all entities in the group
            var pathPoints = group.EntityIds
                .Select(entityId => GridConfiguration.GetPathPointForEntity(entityId))
                .ToArray();

            // Get the path ID of the first entity
            var firstPathId = network.GetPathId(pathPoints[0]);

            // Check if all entities share the same path
            for (var i = 1; i < pathPoints.Length; i++)
                if (network.GetPathId(pathPoints[i]) != firstPathId)
                    return (groupIndex, false,
                        $"Entity {group.EntityIds[i]} not connected to entity {group.EntityIds[0]}");

            return (groupIndex, true, $"All {group.EntityIds.Count} entities connected");
        }

        /// <summary>
        ///     Checks if two entity groups are NOT connected (for disconnect requirements).
        /// </summary>
        private bool CheckGroupsDisconnected(
            EntityGroup group1,
            EntityGroup group2,
            PathNetworkState network)
        {
            // Get representative path point from each group
            var pathPoint1 = GetRepresentativePathPoint(group1);
            var pathPoint2 = GetRepresentativePathPoint(group2);

            if (pathPoint1 == -1 || pathPoint2 == -1)
                return true; // Can't be connected if one group is invalid

            // Groups must NOT be on the same path
            return !network.AreConnected(pathPoint1, pathPoint2);
        }

        /// <summary>
        ///     Gets a representative path point for an entity group.
        /// </summary>
        private int GetRepresentativePathPoint(EntityGroup group)
        {
            if (group.EntityIds.Count == 0)
                return -1;

            return GridConfiguration.GetPathPointForEntity(group.EntityIds[0]);
        }

        /// <summary>
        ///     Gets detailed status for each quest requirement (for UI/debugging).
        /// </summary>
        public QuestStatus GetDetailedStatus(QuestData quest, PathNetworkState network)
        {
            var groupStatuses = new List<GroupStatus>();

            for (var i = 0; i < quest.EntitiesToConnect.Count; i++)
            {
                var group = quest.EntitiesToConnect[i];
                var result = CheckGroupConnected(group, network, i);
                groupStatuses.Add(new GroupStatus(i, result.satisfied, result.message));
            }

            var disconnectStatuses = new List<DisconnectStatus>();

            foreach (var disconnectReq in quest.PathsToDisconnect)
            {
                var group1 = quest.EntitiesToConnect[disconnectReq.GroupIndex1];
                var group2 = quest.EntitiesToConnect[disconnectReq.GroupIndex2];
                var satisfied = CheckGroupsDisconnected(group1, group2, network);

                disconnectStatuses.Add(new DisconnectStatus(
                    disconnectReq.GroupIndex1,
                    disconnectReq.GroupIndex2,
                    satisfied
                ));
            }

            return new QuestStatus(groupStatuses, disconnectStatuses);
        }
    }

    /// <summary>
    ///     Result of quest evaluation.
    /// </summary>
    public readonly struct QuestResult
    {
        public bool IsComplete { get; }
        public bool IsSuccessful { get; }
        public string Message { get; }

        private QuestResult(bool isComplete, bool isSuccessful, string message)
        {
            IsComplete = isComplete;
            IsSuccessful = isSuccessful;
            Message = message;
        }

        public static QuestResult Success()
        {
            return new QuestResult(true, true, "Quest complete!");
        }

        public static QuestResult Incomplete(string reason)
        {
            return new QuestResult(false, false, reason);
        }

        public static QuestResult Failed(string reason)
        {
            return new QuestResult(true, false, reason);
        }

        public override string ToString()
        {
            return $"QuestResult: {(IsSuccessful ? "SUCCESS" : IsComplete ? "FAILED" : "INCOMPLETE")} - {Message}";
        }
    }

    /// <summary>
    ///     Detailed status information for UI display.
    /// </summary>
    public readonly struct QuestStatus
    {
        public IReadOnlyList<GroupStatus> GroupStatuses { get; }
        public IReadOnlyList<DisconnectStatus> DisconnectStatuses { get; }

        public QuestStatus(
            IEnumerable<GroupStatus> groupStatuses,
            IEnumerable<DisconnectStatus> disconnectStatuses)
        {
            GroupStatuses = groupStatuses?.ToList() ?? new List<GroupStatus>();
            DisconnectStatuses = disconnectStatuses?.ToList() ?? new List<DisconnectStatus>();
        }

        public bool AllSatisfied =>
            GroupStatuses.All(g => g.IsSatisfied) &&
            DisconnectStatuses.All(d => d.IsSatisfied);
    }

    public struct GroupStatus
    {
        public int GroupIndex { get; }
        public bool IsSatisfied { get; }
        public string Message { get; }

        public GroupStatus(int groupIndex, bool isSatisfied, string message)
        {
            GroupIndex = groupIndex;
            IsSatisfied = isSatisfied;
            Message = message;
        }
    }

    public struct DisconnectStatus
    {
        public int Group1Index { get; }
        public int Group2Index { get; }
        public bool IsSatisfied { get; }

        public DisconnectStatus(int group1Index, int group2Index, bool isSatisfied)
        {
            Group1Index = group1Index;
            Group2Index = group2Index;
            IsSatisfied = isSatisfied;
        }
    }
}