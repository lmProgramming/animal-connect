using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Core.Models
{
    /// <summary>
    ///     Immutable representation of a quest (puzzle objective).
    ///     Matches the structure from the original Quest.Quest class.
    /// </summary>
    [Serializable]
    public class QuestData
    {
        public QuestData(
            IEnumerable<EntityGroup> entitiesToConnect,
            IEnumerable<DisconnectRequirement> pathsToDisconnect = null)
        {
            EntitiesToConnect = entitiesToConnect?.ToList() ??
                                throw new ArgumentNullException(nameof(entitiesToConnect));
            PathsToDisconnect = pathsToDisconnect?.ToList() ??
                                new List<DisconnectRequirement>();
        }

        public IReadOnlyList<EntityGroup> EntitiesToConnect { get; }
        public IReadOnlyList<DisconnectRequirement> PathsToDisconnect { get; }

        /// <summary>
        ///     Gets the total number of entities involved in this quest.
        /// </summary>
        public int TotalEntitiesInvolved
        {
            get
            {
                return EntitiesToConnect
                    .SelectMany(g => g.EntityIds)
                    .Distinct()
                    .Count();
            }
        }

        /// <summary>
        ///     Checks if this quest has disconnect requirements (harder difficulty).
        /// </summary>
        public bool HasDisconnectRequirements => PathsToDisconnect.Count > 0;

        /// <summary>
        ///     Gets the difficulty rating of this quest.
        /// </summary>
        public float GetDifficultyRating()
        {
            var difficulty = 0f;

            // Base difficulty on number of entities
            difficulty += TotalEntitiesInvolved * 0.05f;

            // Add difficulty for multiple groups
            difficulty += EntitiesToConnect.Count * 0.1f;

            // Add significant difficulty for disconnect requirements
            difficulty += PathsToDisconnect.Count * 0.3f;

            return Math.Min(difficulty, 1.0f);
        }

        public override string ToString()
        {
            var result = new StringBuilder();
            result.AppendLine($"Quest (Difficulty: {GetDifficultyRating():F2}):");

            for (var i = 0; i < EntitiesToConnect.Count; i++)
            {
                var group = EntitiesToConnect[i];
                result.AppendLine($"  Group {i}: Connect entities [{string.Join(", ", group.EntityIds)}]" +
                                  (group.OnlyAClump ? " (clump only)" : ""));
            }

            if (PathsToDisconnect.Count > 0)
            {
                result.AppendLine("  Disconnect requirements:");
                foreach (var req in PathsToDisconnect)
                    result.AppendLine($"    Groups {req.GroupIndex1} and {req.GroupIndex2} must NOT connect");
            }

            return result.ToString();
        }
    }

    /// <summary>
    ///     Represents a group of entities that should be connected together.
    /// </summary>
    [Serializable]
    public class EntityGroup
    {
        public EntityGroup(IEnumerable<int> entityIds, bool onlyAClump = false)
        {
            EntityIds = entityIds?.ToList() ?? throw new ArgumentNullException(nameof(entityIds));
            OnlyAClump = onlyAClump;

            if (EntityIds.Count == 0)
                throw new ArgumentException("Entity group must contain at least one entity");
        }

        public IReadOnlyList<int> EntityIds { get; }

        /// <summary>
        ///     If true, this is just a clump of objects that don't need to be connected,
        ///     but must be unconnected to something else.
        /// </summary>
        public bool OnlyAClump { get; }

        public override string ToString()
        {
            return $"[{string.Join(", ", EntityIds)}]" + (OnlyAClump ? " (clump)" : "");
        }
    }

    /// <summary>
    ///     Represents a requirement that two entity groups must NOT be connected.
    /// </summary>
    [Serializable]
    public struct DisconnectRequirement
    {
        public int GroupIndex1 { get; }
        public int GroupIndex2 { get; }

        public DisconnectRequirement(int groupIndex1, int groupIndex2)
        {
            GroupIndex1 = groupIndex1;
            GroupIndex2 = groupIndex2;
        }

        public override string ToString()
        {
            return $"Groups {GroupIndex1} and {GroupIndex2} must be disconnected";
        }
    }
}