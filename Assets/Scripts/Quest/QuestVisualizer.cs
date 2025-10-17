using System.Collections.Generic;
using System.Linq;
using Other;
using UnityEngine;
using UnityEngine.UI;

namespace Quest
{
    public class QuestVisualizer : MonoBehaviour
    {
        [SerializeField] private EntitySprites sprites;
        [SerializeField] private GameObject arrowPrefab;
        [SerializeField] private GameObject arrowDisconnectPrefab;

        [SerializeField] private int distanceBetweenPaths;
        [SerializeField] private int distanceBetweenPathEntities;
        [SerializeField] private int distanceBetweenObjectsInClumps;

        [SerializeField] private GameObject entityPrefab;

        [SerializeField] private Transform questVisualizationHolder;
        private readonly int[] _entityIndexesPriority = { 4, 7, 1, 11, 8, 10, 3, 9, 5, 6, 2, 0 };

        private readonly PathSorter _pathSorter = new();

        private (List<Path>, int) ConvertToPaths(List<EntitiesToConnect> entitiesToConnectIDs)
        {
            var paths = new List<Path>();

            var actualPathsCount = 0;

            foreach (var path in entitiesToConnectIDs)
                if (path.onlyAClump)
                {
                    var newPath = new ClumpPath();
                    newPath.Setup(path);

                    paths.Add(newPath);
                }
                else
                {
                    var mostPrioritizedEntityID = GetPrioritizedEntity(path.entitiesIDs);

                    var newPath = new Path();
                    newPath.Setup(path, mostPrioritizedEntityID);

                    paths.Add(newPath);

                    if (path.entitiesIDs.Count > 1) actualPathsCount++;
                }

            paths = _pathSorter.SortPaths(paths);

            return (paths, actualPathsCount);
        }

        public void GenerateVisualization(Quest quest)
        {
            var entitiesToConnectIDs = quest.entitiesToConnectIDs;
            var pathsToDisconnectIndexes = quest.pathsToDisconnectIndexes;

            var pathsInfo = ConvertToPaths(entitiesToConnectIDs);

            var paths = pathsInfo.Item1;

            var actualPathsCount = pathsInfo.Item2;

            foreach (var enemyPathsIndexes in pathsToDisconnectIndexes)
            {
                var pathOne = paths[enemyPathsIndexes.x];
                var pathTwo = paths[enemyPathsIndexes.y];

                var pathOneRepresentant = pathOne.ArrowEntityID;
                var pathTwoRepresentant = pathTwo.ArrowEntityID;

                var moreImportantEntity = GetPrioritizedEntity(pathOneRepresentant, pathTwoRepresentant);

                if (moreImportantEntity == pathOne.ArrowEntityID)
                {
                    pathOne.AddInferiorPathToDisconnect(pathTwo);
                    pathTwo.AddSuperiorPathToDisconnect(pathOne);
                }
                else
                {
                    pathTwo.AddInferiorPathToDisconnect(pathOne);
                    pathOne.AddSuperiorPathToDisconnect(pathTwo);
                }
            }

            var startingPosition = new Vector2(-(paths.Count - 1) * distanceBetweenPaths / 2f, 0);

            foreach (var path in paths)
            {
                VisualizePath(path, startingPosition);

                startingPosition += new Vector2(distanceBetweenPaths, 0);

                //if (path.otherEntitiesClump.Count > 0)
                //{
                //    startingPosition += new Vector2(distanceBetweenPaths, 0);
                //}
            }

            for (var i = 0; i < paths.Count; i++)
            {
                var path = paths[i];
                foreach (var disconnectPath in path.InferiorPathsToDisconnect)
                {
                    var distance = Mathf.Abs(i - paths.FindIndex(x => x == disconnectPath));

                    PointDisconnectArrow(path.RepresentantObject.GetComponent<RectTransform>().anchoredPosition,
                        distance, disconnectPath.RepresentantObject.GetComponent<RectTransform>().anchoredPosition);
                }
            }

            ResizeQuestHolder(paths.Count);
        }

        private void ResizeQuestHolder(int pathsAmount)
        {
            var multiplier = pathsAmount > 3 ? Mathf.Clamp(1 - (pathsAmount - 3) / 4f, 0.5f, 1f) : 1;
            questVisualizationHolder.transform.localScale *= multiplier;
        }

        private void VisualizePath(Path path, Vector2 position)
        {
            var representantPosition = position + new Vector2(0, distanceBetweenPathEntities);

            var representant = CreateEntityVisualization(path.ArrowEntityID, representantPosition);

            path.RepresentantObject = representant;

            var otherEntitiesClump = path.OtherEntitiesClump;

            if (otherEntitiesClump.Count > 0)
            {
                var clumpCentrePosition = position - new Vector2(0, distanceBetweenPathEntities);

                CreateEntitiesClump(otherEntitiesClump, clumpCentrePosition);

                PointArrow(representant.GetComponent<RectTransform>().anchoredPosition, clumpCentrePosition, position);
            }
        }

        public GameObject CreateEntityVisualization(int entityID, Vector2 position = default)
        {
            if (position == default) position = Vector2.zero;

            var entity = Instantiate(entityPrefab, questVisualizationHolder);
            entity.GetComponent<Image>().sprite = GetSpriteFromEntityID(entityID);

            entity.GetComponent<RectTransform>().anchoredPosition = position;

            return entity;
        }

        private GameObject[] CreateEntitiesClump(List<int> entitiesClump, Vector2 clumpCentrePosition)
        {
            var count = entitiesClump.Count;

            var entities = new GameObject[count];

            var positionBiases = ClumpPositionShifts(count);

            for (var i = 0; i < count; i++)
            {
                var position = clumpCentrePosition + positionBiases[i];

                var newEntity = CreateEntityVisualization(entitiesClump[i], position);

                entities[i] = newEntity;
            }

            return entities;
        }

        private Vector2[] ClumpPositionShifts(int count)
        {
            var distance = distanceBetweenObjectsInClumps;
            var halfDistance = distanceBetweenObjectsInClumps / 2;

            var shifts = new Vector2[count];

            for (var i = 0; i < count; i++)
            {
                var bar = i / 2;

                if (i % 2 == 0)
                {
                    if (i == count - 1)
                        shifts[i] = new Vector2(0, -bar * distance);
                    else
                        shifts[i] = new Vector2(-halfDistance, -bar * distance);
                }
                else
                {
                    shifts[i] = new Vector2(halfDistance, -bar * distance);
                }

                Debug.Log(i + " " + i % 2 + " " + shifts[i]);
            }

            Debug.Log("NICE");

            for (var i = 0; i < shifts.Length; i++) Debug.Log(shifts[i]);

            return shifts;
        }

        //Vector2[] ClumpPositionShifts(int count)
        //{
        //    int distance = distanceBetweenObjectsInClumps;
        //    int halfDistance = distanceBetweenObjectsInClumps / 2;
        //    switch (count)
        //    {
        //        case 1:
        //            return new Vector2[] { Vector2.zero };
        //        //case 2:
        //        //    return new Vector2[] 
        //        //    { 
        //        //        new Vector2(-halfDistance, 0), 
        //        //        new Vector2( halfDistance, 0) 
        //        //    };
        //        //case 3:
        //        //    return MathExt.GetTriangleApexes(distance);
        //        //case 4:
        //        //    return new Vector2[] 
        //        //    {
        //        //        new Vector2(-halfDistance,  halfDistance), 
        //        //        new Vector2( halfDistance,  halfDistance),
        //        //        new Vector2(-halfDistance, -halfDistance),
        //        //        new Vector2( halfDistance, -halfDistance)
        //        //    };
        //        default:
        //            return ElementsInCircleAroundShifts(count, halfDistance);
        //    }
        //}

        //Vector2[] ElementsInCircleAroundShifts(int count, int radius)
        //{
        //    double angleBetweenObjects = 2 * Math.PI / count;

        //    List<Vector2> shifts = new();

        //    double startingBias = angleBetweenObjects / 2;

        //    for (int i = 0; i < count; i++)
        //    {
        //        // Calculate the position of the object
        //        double x = radius * Math.Cos(i * angleBetweenObjects + startingBias);
        //        double y = radius * Math.Sin(i * angleBetweenObjects + startingBias);

        //        shifts.Add(new Vector2((float)x, (float)y));
        //    }

        //    return shifts.ToArray();
        //}

        private void PointArrow(Vector2 representantPosition, Vector2 clumpCentre, Vector2 position)
        {
            var arrow = Instantiate(arrowPrefab, questVisualizationHolder);

            // for some reason this way
            var angleRadians = MathExt.AngleBetweenTwoPoints(clumpCentre, representantPosition);

            arrow.GetComponent<RectTransform>().anchoredPosition = position;
            arrow.GetComponent<RectTransform>().rotation = Quaternion.Euler(0, 0, angleRadians);
        }

        private void PointDisconnectArrow(Vector2 representantPosition, int curDistanceBetweenPaths,
            Vector2 submissivePathRepresentant)
        {
            var arrow = Instantiate(arrowDisconnectPrefab, questVisualizationHolder);

            // for some reason this way
            var angleRadians = MathExt.AngleBetweenTwoPoints(submissivePathRepresentant, representantPosition);

            arrow.GetComponent<RectTransform>().anchoredPosition =
                (representantPosition + submissivePathRepresentant) / 2;
            arrow.GetComponent<RectTransform>().rotation = Quaternion.Euler(0, 0, angleRadians);
        }

        public Sprite GetSpriteFromEntityID(int entityID)
        {
            return sprites.sprites[entityID];
        }

        public int GetPrioritizedEntity(List<int> entities)
        {
            var mostPrioritizedEntityIndex = 0;

            Debug.Log(entities[0]);

            for (var i = 1; i < entities.Count; i++)
            {
                var entityOne = entities[mostPrioritizedEntityIndex];
                var entityTwo = entities[i];
                if (GetPrioritizedEntity(entityOne, entityTwo) == entityTwo) mostPrioritizedEntityIndex = i;

                Debug.Log(entityTwo);
            }

            Debug.Log(mostPrioritizedEntityIndex);

            return entities[mostPrioritizedEntityIndex];
        }

        public int GetPrioritizedEntity(int entityOne, int entityTwo)
        {
            for (var i = 0; i < _entityIndexesPriority.Length; i++)
            {
                if (_entityIndexesPriority[i] == entityOne) return entityOne;

                if (_entityIndexesPriority[i] == entityTwo) return entityTwo;
            }

            return entityOne;
        }

        //private void OnValidate()
        //{
        //    if (GameManager.Instance != null)
        //    {
        //        GenerateVisualization(GameManager.Instance.Quest);

        //    }
        //}

        public void DeleteVisualization()
        {
            foreach (Transform child in questVisualizationHolder) Destroy(child.gameObject);
        }

        private class Path
        {
            public readonly List<Path> InferiorPathsToDisconnect = new();
            public readonly List<Path> SuperiorPathsToDisconnect = new();

            public List<int> OtherEntitiesClump;

            public GameObject RepresentantObject;
            public int ArrowEntityID { get; private set; }

            public virtual void Setup(EntitiesToConnect path, int mostPrioritizedEntityID)
            {
                ArrowEntityID = mostPrioritizedEntityID;

                // list of entities without the one that will point at others with it's arrow
                OtherEntitiesClump = new List<int>(path.entitiesIDs);
                OtherEntitiesClump.Remove(mostPrioritizedEntityID);
            }

            public void AddInferiorPathToDisconnect(Path enemyPath)
            {
                InferiorPathsToDisconnect.Add(enemyPath);
            }

            public void AddSuperiorPathToDisconnect(Path enemyPath)
            {
                SuperiorPathsToDisconnect.Add(enemyPath);
            }

            public bool IsPathEnemy(Path potentialEnemyPath)
            {
                return InferiorPathsToDisconnect.Contains(potentialEnemyPath) ||
                       SuperiorPathsToDisconnect.Contains(potentialEnemyPath);
            }
        }

        private class ClumpPath : Path
        {
            public void Setup(EntitiesToConnect path)
            {
                OtherEntitiesClump = new List<int>(path.entitiesIDs);
            }
        }

        private class PathSorter
        {
            public List<Path> SortPaths(List<Path> paths)
            {
                var unsortedPaths = new List<Path>(paths);

                var sortedPaths = new Path[paths.Count];

                while (unsortedPaths.Count > 0)
                {
                    var selectedPath = MathExt.RandomPullFrom(unsortedPaths);

                    var chosenIndex = FindBestSpotForPath(selectedPath, sortedPaths);

                    sortedPaths[chosenIndex] = selectedPath;
                }

                return sortedPaths.ToList();
            }

            private bool CheckIfSortedPathsDecent(List<Path> paths)
            {
                List<int> adversariesLeft = new();

                var sortedPaths = paths.ToArray();

                for (var i = 0; i < sortedPaths.Length; i++)
                    adversariesLeft.Add(Mathf.Max(FindIndexesOfEnemies(sortedPaths[i], sortedPaths).Length, 2));

                for (var i = 1; i < sortedPaths.Length - 1; i++)
                {
                    if (sortedPaths[i - 1].IsPathEnemy(sortedPaths[i])) adversariesLeft[i]--;
                    if (sortedPaths[i + 1].IsPathEnemy(sortedPaths[i])) adversariesLeft[i]--;

                    if (adversariesLeft[i] != 0)
                    {
                        Debug.Log(adversariesLeft[i]);
                        return false;
                    }
                }

                return true;
            }

            private int FindBestSpotForPath(Path path, Path[] sortedPaths)
            {
                var spotsWithAntagonisticPath = new List<int>();

                for (var i = 0; i < sortedPaths.Length; i++)
                    if (path.IsPathEnemy(sortedPaths[i]))
                        spotsWithAntagonisticPath.Add(i);

                if (spotsWithAntagonisticPath.Count == 0)
                {
                    if (path.InferiorPathsToDisconnect.Count >= 1 || path.SuperiorPathsToDisconnect.Count >= 1)
                        return FindEmptySpotNotOnEdgesPreferably(sortedPaths);

                    return FindEmptySpotOnEdgePreferably(sortedPaths);
                }

                if (spotsWithAntagonisticPath.Count == 1)
                {
                    var indexOfEnemy = FindIndexOfEnemy(path, sortedPaths);

                    if (indexOfEnemy != -1)
                    {
                        var neighbouringSlots = NeighboursOf(indexOfEnemy, sortedPaths.Length);

                        for (var i = 0; i < neighbouringSlots.Length; i++)
                            if (sortedPaths[neighbouringSlots[i]] == null)
                                return neighbouringSlots[i];
                    }
                }

                return FindEmptySpotNotOnEdgesPreferably(sortedPaths);
            }

            private int FindIndexOfEnemy(Path path, Path[] sortedPath)
            {
                for (var i = 0; i < sortedPath.Length; i++)
                    if (path.IsPathEnemy(sortedPath[i]))
                        return i;

                return -1;
            }

            private int[] FindIndexesOfEnemies(Path path, Path[] sortedPath)
            {
                List<int> indexes = new();

                for (var i = 0; i < sortedPath.Length; i++)
                    if (path.IsPathEnemy(sortedPath[i]))
                        indexes.Add(i);

                return indexes.ToArray();
            }

            private int[] NeighboursOf(int index, int sortedPathsLength)
            {
                var neighbours = new List<int> { index - 1, index + 1 };

                if (neighbours[0] < 0)
                {
                    neighbours.RemoveAt(0);

                    if (neighbours[0] >= sortedPathsLength)
                    {
                        Debug.Log("WHAT??????????? 150 quest visualizer");
                        neighbours.RemoveAt(0);
                    }
                }
                else if (neighbours[1] >= sortedPathsLength)
                {
                    neighbours.RemoveAt(1);
                }

                return neighbours.ToArray();
            }

            private int FindEmptySpotOnEdgePreferably(Path[] sortedPaths)
            {
                if (sortedPaths.Length >= 3)
                {
                    if (sortedPaths[0] == null)
                    {
                        if (sortedPaths[1] == null) return 0;

                        if (sortedPaths[sortedPaths.Length - 1] == null)
                        {
                            if (sortedPaths[sortedPaths.Length - 2] == null) return sortedPaths.Length - 1;
                        }
                        else
                        {
                            return 0;
                        }
                    }

                    if (sortedPaths[sortedPaths.Length - 1] == null) return sortedPaths.Length - 1;

                    for (var i = 1; i < sortedPaths.Length - 1; i++)
                        if (sortedPaths[i] == null)
                            return i;
                }

                if (sortedPaths[0] == null) return 0;

                return 1;
            }


            private int FindEmptySpotNotOnEdgesPreferably(Path[] sortedPaths)
            {
                for (var i = 1; i < sortedPaths.Length; i++)
                    if (sortedPaths[i] == null)
                        return i;

                if (sortedPaths[0] == null) return 0;

                return sortedPaths.Length - 1;
            }
        }
    }
}