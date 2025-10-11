using System.Collections.Generic;
using Other;
using UnityEngine;

namespace Quest
{
    public static class QuestGenerator
    {
        public static Quest GenerateQuest(float difficulty)
        {
            List<EntitiesToConnect> entitiesToConnect = new();

            List<MockPath> pathList = new();

            List<Vector2Int> pathsToDisconnect = new();

            List<int> availableEntityIndex = new();

            for (var i = 0; i < 12; i++) availableEntityIndex.Add(i);

            int pathsAmount;

            if (difficulty == 0)
            {
                // pathsAmount = 1;

                var entitiesToConnectAmount = 2;

                pathList.Add(new MockPath(entitiesToConnectAmount));
            }
            else if (difficulty < 0.1f)
            {
                // pathsAmount = 1;

                var entitiesToConnectAmount = MathExt.RandomInclusive(3, 4);

                pathList.Add(new MockPath(entitiesToConnectAmount));
            }
            else if (difficulty < 0.2f)
            {
                pathsAmount = MathExt.RandomInclusive(1, 2);

                if (pathsAmount == 1)
                {
                    var entitiesToConnectAmount = MathExt.RandomInclusive(5, 6);

                    pathList.Add(new MockPath(entitiesToConnectAmount));
                }
                else
                {
                    var entitiesToConnectAmountTotal = MathExt.RandomInclusive(4, 6);

                    var entitiesForPath1 = MathExt.RandomInclusive(2, Mathf.Min(4, entitiesToConnectAmountTotal - 2));

                    pathList.Add(new MockPath(entitiesForPath1));

                    var entitiesForPath2 = entitiesToConnectAmountTotal - entitiesForPath1;

                    pathList.Add(new MockPath(entitiesForPath2));
                }
            }
            else if (difficulty < 0.3f)
            {
                // pathsAmount = 2;

                var entitiesToConnectAmountTotal = MathExt.RandomInclusive(4, 6);

                var entitiesForPath1 = MathExt.RandomInclusive(2, Mathf.Min(4, entitiesToConnectAmountTotal - 2));

                pathList.Add(new MockPath(entitiesForPath1));

                var entitiesForPath2 = entitiesToConnectAmountTotal - entitiesForPath1;

                pathList.Add(new MockPath(entitiesForPath2));

                pathsToDisconnect.Add(new Vector2Int(0, 1));
            }
            else
            {
                pathList.Add(new MockPath(2));
                pathList.Add(new MockPath(2));
                pathList.Add(new MockPath(2));
                pathsToDisconnect.Add(new Vector2Int(0, 1));
                pathsToDisconnect.Add(new Vector2Int(1, 2));
                pathsToDisconnect.Add(new Vector2Int(0, 2));
            }

            foreach (var mockPath in pathList)
            {
                var entitiesToConnectToAdd = GenerateRealPath(mockPath, availableEntityIndex);

                entitiesToConnect.Add(entitiesToConnectToAdd);
            }

            return new Quest(entitiesToConnect, pathsToDisconnect);
        }

        private static EntitiesToConnect GenerateRealPath(MockPath mockPath, List<int> availableEntitiesIDs)
        {
            var entitiesToAdd = mockPath.EntitiesAmount;

            EntitiesToConnect entitiesToConnect = new();

            while (entitiesToAdd > 0)
            {
                var entityToAdd = MathExt.RandomPullFrom(availableEntitiesIDs);

                entitiesToConnect.entitiesIDs.Add(entityToAdd);

                entitiesToAdd--;
            }

            return entitiesToConnect;
        }

        private class MockPath
        {
            public readonly int EntitiesAmount;

            public MockPath(int entitiesAmount)
            {
                EntitiesAmount = entitiesAmount;
            }
        }
    }
}