using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class QuestVisualizer : MonoBehaviour
{
    int[] entityIndexesPriority = new int[12] { 4, 7, 1, 11, 8, 10, 3, 9, 5, 6, 2, 0 };

    [SerializeField]
    EntitySprites sprites;
    [SerializeField]
    GameObject arrowPrefab;
    [SerializeField]
    GameObject arrowDisconnectPrefab;

    [SerializeField]
    int distanceBetweenPaths;
    [SerializeField]
    int distanceBetweenPathEntities;
    [SerializeField]
    int distanceBetweenObjectsInClumps;

    [SerializeField]
    GameObject entityPrefab;

    [SerializeField]
    Transform questVisualizationHolder;

    PathSorter pathSorter = new();

    class Path
    {
        public int arrowEntityID { get; private set; }

        public List<int> otherEntitiesClump;

        public List<Path> inferiorPathsToDisconnect = new List<Path>(); 
        public List<Path> superiorPathsToDisconnect = new List<Path>();

        public GameObject representantObject;

        public virtual void Setup(EntitiesToConnect path, int mostPrioritizedEntityID)
        {
            arrowEntityID = mostPrioritizedEntityID;

            // list of entities without the one that will point at others with it's arrow
            otherEntitiesClump = new List<int>(path.entitiesIDs);
            otherEntitiesClump.Remove(mostPrioritizedEntityID);
        }

        public void AddInferiorPathToDisconnect(Path enemyPath)
        {
            inferiorPathsToDisconnect.Add(enemyPath);
        }

        public void AddSuperiorPathToDisconnect(Path enemyPath)
        {
            superiorPathsToDisconnect.Add(enemyPath);
        }

        public bool IsPathEnemy(Path potentialEnemyPath)
        {
            return inferiorPathsToDisconnect.Contains(potentialEnemyPath) || superiorPathsToDisconnect.Contains(potentialEnemyPath);
        }
    }

    class ClumpPath : Path
    {
        public void Setup(EntitiesToConnect path)
        {
            otherEntitiesClump = new List<int>(path.entitiesIDs);
        }
    }
    
    class PathSorter
    {
        public List<Path> SortPaths(List<Path> paths)
        {
            List<Path> unsortedPaths = new List<Path>(paths);

            Path[] sortedPaths = new Path[paths.Count];

            while (unsortedPaths.Count > 0)
            {
                Path selectedPath = MathExt.RandomPullFrom(unsortedPaths);

                int chosenIndex = FindBestSpotForPath(selectedPath, sortedPaths);

                sortedPaths[chosenIndex] = selectedPath;
            }

            return sortedPaths.ToList();
        }

        bool CheckIfSortedPathsDecent(List<Path> paths)
        {
            List<int> adversariesLeft = new();

            Path[] sortedPaths = paths.ToArray();

            for (int i = 0; i < sortedPaths.Length; i++)
            {
                adversariesLeft.Add(Mathf.Max(FindIndexesOfEnemies(sortedPaths[i], sortedPaths).Length, 2));
            }

            for (int i = 1; i < sortedPaths.Length - 1; i++)
            {
                if (sortedPaths[i - 1].IsPathEnemy(sortedPaths[i]))
                {
                    adversariesLeft[i]--;
                }
                if (sortedPaths[i + 1].IsPathEnemy(sortedPaths[i]))
                {
                    adversariesLeft[i]--;
                }

                if (adversariesLeft[i] != 0)
                {
                    Debug.Log(adversariesLeft[i]);
                    return false;
                }
            }
            return true;
        }

        int FindBestSpotForPath(Path path, Path[] sortedPaths)
        {
            List<int> spotsWithAntagonisticPath = new List<int>();

            for (int i = 0; i < sortedPaths.Length; i++)
            {
                if (path.IsPathEnemy(sortedPaths[i]))
                {
                    spotsWithAntagonisticPath.Add(i);
                }
            }

            if (spotsWithAntagonisticPath.Count == 0)
            {
                if (path.inferiorPathsToDisconnect.Count >= 1 || path.superiorPathsToDisconnect.Count >= 1)
                {
                    return FindEmptySpotNotOnEdgesPreferably(sortedPaths);
                }
                else
                {
                    return FindEmptySpotOnEdgePreferably(sortedPaths);
                }
            }
            else if (spotsWithAntagonisticPath.Count == 1)
            {
                int indexOfEnemy = FindIndexOfEnemy(path, sortedPaths);

                if (indexOfEnemy != -1)
                {
                    int[] neighbouringSlots = NeighboursOf(indexOfEnemy, sortedPaths.Length);

                    for (int i = 0; i < neighbouringSlots.Length; i++)
                    {
                        if (sortedPaths[neighbouringSlots[i]] == null)
                        {
                            return neighbouringSlots[i];
                        }
                    }

                    return FindEmptySpotNotOnEdgesPreferably(sortedPaths);
                }
                else
                {
                    return FindEmptySpotNotOnEdgesPreferably(sortedPaths);
                }
            }
            else
            {
                return FindEmptySpotNotOnEdgesPreferably(sortedPaths);
            }
        }

        int FindIndexOfEnemy(Path path, Path[] sortedPath)
        {
            for (int i = 0; i < sortedPath.Length; i++)
            {
                if (path.IsPathEnemy(sortedPath[i]))
                {
                    return i;
                }
            }

            return -1;
        }

        int[] FindIndexesOfEnemies(Path path, Path[] sortedPath)
        {
            List<int> indexes = new();

            for (int i = 0; i < sortedPath.Length; i++)
            {
                if (path.IsPathEnemy(sortedPath[i]))
                {
                    indexes.Add(i);
                }
            }

            return indexes.ToArray();
        }

        int[] NeighboursOf(int index, int sortedPathsLength)
        {
            List<int> neighbours = new List<int>() { index - 1, index + 1 };

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

        int FindEmptySpotOnEdgePreferably(Path[] sortedPaths)
        {
            if (sortedPaths.Length >= 3)
            {
                if (sortedPaths[0] == null)
                {
                    if (sortedPaths[1] == null)
                    {
                        return 0;
                    }
                    else if (sortedPaths[sortedPaths.Length - 1] == null)
                    {
                        if (sortedPaths[sortedPaths.Length - 2] == null)
                        {
                            return sortedPaths.Length - 1;
                        }
                    }
                    else
                    {
                        return 0;
                    }
                }

                if (sortedPaths[sortedPaths.Length - 1] == null)
                {
                    return sortedPaths.Length - 1;
                }

                for (int i = 1; i < sortedPaths.Length - 1; i++)
                {
                    if (sortedPaths[i] == null)
                    {
                        return i;
                    }
                }
            }

            if (sortedPaths[0] == null)
            {
                return 0;
            }

            return 1;
        }


        int FindEmptySpotNotOnEdgesPreferably(Path[] sortedPaths)
        {
            for (int i = 1; i < sortedPaths.Length; i++)
            {
                if (sortedPaths[i] == null)
                {
                    return i;
                }
            }

            if (sortedPaths[0] == null)
            {
                return 0;
            }

            return sortedPaths.Length - 1;
        }
    }

    (List<Path>, int) ConvertToPaths(List<EntitiesToConnect> entitiesToConnectIDs)
    {
        List<Path> paths = new List<Path>();

        int actualPathsCount = 0;

        foreach (var path in entitiesToConnectIDs)
        {
            if (path.onlyAClump)
            {
                ClumpPath newPath = new ClumpPath();
                newPath.Setup(path);

                paths.Add(newPath);
            }
            else
            {
                int mostPrioritizedEntityID = GetPrioritizedEntity(path.entitiesIDs);

                Path newPath = new Path();
                newPath.Setup(path, mostPrioritizedEntityID);

                paths.Add(newPath);

                if (path.entitiesIDs.Count > 1)
                {
                    actualPathsCount++;
                }
            }
        }

        paths = pathSorter.SortPaths(paths);

        return (paths, actualPathsCount);
    }

    public void GenerateVisualization(Quest quest)
    {
        List<EntitiesToConnect> entitiesToConnectIDs = quest.entitiesToConnectIDs;
        List<Vector2Int> pathsToDisconnectIndexes = quest.pathsToDisconnectIndexes;

        var pathsInfo = ConvertToPaths(entitiesToConnectIDs);

        List<Path> paths = pathsInfo.Item1;

        int actualPathsCount = pathsInfo.Item2;

        foreach (var enemyPathsIndexes in pathsToDisconnectIndexes)
        {
            Path pathOne = paths[enemyPathsIndexes.x];
            Path pathTwo = paths[enemyPathsIndexes.y];

            int pathOneRepresentant = pathOne.arrowEntityID;
            int pathTwoRepresentant = pathTwo.arrowEntityID;

            int moreImportantEntity = GetPrioritizedEntity(pathOneRepresentant, pathTwoRepresentant);

            if (moreImportantEntity == pathOne.arrowEntityID)
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

        Vector2 startingPosition = new Vector2(-(paths.Count - 1) * distanceBetweenPaths / 2, 0);

        foreach (var path in paths)
        {
            VisualizePath(path, startingPosition);

            startingPosition += new Vector2(distanceBetweenPaths, 0);

            //if (path.otherEntitiesClump.Count > 0)
            //{
            //    startingPosition += new Vector2(distanceBetweenPaths, 0);
            //}
        }

        for (int i = 0; i < paths.Count; i++)
        {
            Path path = paths[i];
            foreach (var disconnectPath in path.inferiorPathsToDisconnect)
            {
                int distance = Mathf.Abs(i - paths.FindIndex((x) => x == disconnectPath));

                PointDisconnectArrow(path.representantObject.GetComponent<RectTransform>().anchoredPosition, distance, disconnectPath.representantObject.GetComponent<RectTransform>().anchoredPosition);
            }
        }

        ResizeQuestHolder(paths.Count);
    }

    void ResizeQuestHolder(int pathsAmount)
    {
        float multiplier = pathsAmount > 3 ? Mathf.Clamp(1 - (pathsAmount - 3) / 4f, 0.5f, 1f) : 1;
        questVisualizationHolder.transform.localScale *= multiplier;
    }

    void VisualizePath(Path path, Vector2 position)
    {
        Vector2 representantPosition = position + new Vector2(0, distanceBetweenPathEntities);

        GameObject representant = CreateEntityVisualization(path.arrowEntityID, representantPosition);

        path.representantObject = representant;

        List<int> otherEntitiesClump = path.otherEntitiesClump;

        if (otherEntitiesClump.Count > 0)
        {
            Vector2 clumpCentrePosition = position - new Vector2(0, distanceBetweenPathEntities);

            CreateEntitiesClump(otherEntitiesClump, clumpCentrePosition);

            PointArrow(representant.GetComponent<RectTransform>().anchoredPosition, clumpCentrePosition, position);
        }
    }

    public GameObject CreateEntityVisualization(int entityID, Vector2 position = default)
    {
        if (position == default)
        {
            position = Vector2.zero;
        }

        GameObject entity = Instantiate(entityPrefab, questVisualizationHolder);
        entity.GetComponent<Image>().sprite = GetSpriteFromEntityID(entityID);

        entity.GetComponent<RectTransform>().anchoredPosition = position;

        return entity;
    }

    GameObject[] CreateEntitiesClump(List<int> entitiesClump, Vector2 clumpCentrePosition)
    {
        int count = entitiesClump.Count;

        GameObject[] entities = new GameObject[count];

        Vector2[] positionBiases = ClumpPositionShifts(count);

        for (int i = 0; i < count; i++)
        {
            Vector2 position = clumpCentrePosition + positionBiases[i];

            GameObject newEntity = CreateEntityVisualization(entitiesClump[i], position);       

            entities[i] = newEntity;
        }

        return entities;
    }

    Vector2[] ClumpPositionShifts(int count)
    {
        int distance = distanceBetweenObjectsInClumps;
        int halfDistance = distanceBetweenObjectsInClumps / 2;

        Vector2[] shifts = new Vector2[count];

        for (int i = 0; i < count; i++)
        {
            int bar = i / 2;

            if (i % 2 == 0)
            {
                if (i == count - 1)
                {
                    shifts[i] = new Vector2(0, -bar * distance);
                }
                else
                {
                    shifts[i] = new Vector2(-halfDistance, -bar * distance);
                }
            }
            else
            {
                shifts[i] = new Vector2(halfDistance, -bar * distance);
            }

            Debug.Log(i + " " + i % 2 + " " + shifts[i]);
        }

        Debug.Log("NICE");

        for (int i = 0; i < shifts.Length; i++)
        {
            Debug.Log(shifts[i]);
        }

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

    void PointArrow(Vector2 representantPosition, Vector2 clumpCentre, Vector2 position)
    {
        GameObject arrow = Instantiate(arrowPrefab, questVisualizationHolder);

        // for some reason this way
        float angleRadians = MathExt.AngleBetweenTwoPoints(clumpCentre, representantPosition);

        arrow.GetComponent<RectTransform>().anchoredPosition = position;
        arrow.GetComponent<RectTransform>().rotation = Quaternion.Euler(0, 0, angleRadians);
    }

    void PointDisconnectArrow(Vector2 representantPosition, int distanceBetweenPaths, Vector2 submissivePathRepresentant)
    {
        GameObject arrow = Instantiate(arrowDisconnectPrefab, questVisualizationHolder);

        // for some reason this way
        float angleRadians = MathExt.AngleBetweenTwoPoints(submissivePathRepresentant, representantPosition);

        arrow.GetComponent<RectTransform>().anchoredPosition = (representantPosition + submissivePathRepresentant) / 2;
        arrow.GetComponent<RectTransform>().rotation = Quaternion.Euler(0, 0, angleRadians);
    }

    public Sprite GetSpriteFromEntityID(int entityID)
    {
        return sprites.sprites[entityID];
    }

    public int GetPrioritizedEntity(List<int> entities)
    {
        int mostPrioritizedEntityIndex = 0;

        Debug.Log(entities[0]);

        for (int i = 1; i < entities.Count; i++)
        {
            int entityOne = entities[mostPrioritizedEntityIndex];
            int entityTwo = entities[i];
            if (GetPrioritizedEntity(entityOne, entityTwo) == entityTwo)
            {
                mostPrioritizedEntityIndex = i;
            }

            Debug.Log(entityTwo);
        }

        Debug.Log(mostPrioritizedEntityIndex);

        return entities[mostPrioritizedEntityIndex];
    }

    public int GetPrioritizedEntity(int entityOne, int entityTwo)
    {
        for (int i = 0; i < entityIndexesPriority.Length; i++)
        {
            if (entityIndexesPriority[i] == entityOne)
            {
                return entityOne;
            }
            else if (entityIndexesPriority[i] == entityTwo)
            {
                return entityTwo;
            }
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
        foreach (Transform child in questVisualizationHolder)
        {
            Destroy(child.gameObject);
        }
    }
}
