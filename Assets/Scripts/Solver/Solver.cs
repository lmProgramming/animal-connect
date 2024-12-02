using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;

public class Solver : MonoBehaviour
{
    TileVirtual[] tiles = new TileVirtual[9];
    GridSlotVirtual[] gridSlots = new GridSlotVirtual[9];
    PathPoint[] pathPoints = new PathPoint[24];

    Entity[] entities = new Entity[12];

    private void Start()
    {
        tiles[0] = new TileVirtual(Tile.TileType.Curve);
        tiles[1] = new TileVirtual(Tile.TileType.Curve);
        tiles[2] = new TileVirtual(Tile.TileType.Curve);
        tiles[3] = new TileVirtual(Tile.TileType.TwoCurves);
        tiles[4] = new TileVirtual(Tile.TileType.TwoCurves);
        tiles[5] = new TileVirtual(Tile.TileType.XIntersection);
        tiles[6] = new TileVirtual(Tile.TileType.Bridge);
        tiles[7] = new TileVirtual(Tile.TileType.TIntersection);
        tiles[8] = new TileVirtual(Tile.TileType.TIntersection);

        for (int i = 0; i < 9; i++)
        {
            gridSlots[i] = new GridSlotVirtual();
        }

        for (int i = 0; i < 24; i++)
        {
            pathPoints[i] = new PathPoint(-1, 0);
        }

        gridSlots[0].pathPoints[0] = pathPoints[0];
        gridSlots[0].pathPoints[1] = pathPoints[13];
        gridSlots[0].pathPoints[2] = pathPoints[3];
        gridSlots[0].pathPoints[3] = pathPoints[12];

        gridSlots[1].pathPoints[0] = pathPoints[1];
        gridSlots[1].pathPoints[1] = pathPoints[14];
        gridSlots[1].pathPoints[2] = pathPoints[4];
        gridSlots[1].pathPoints[3] = pathPoints[13];

        gridSlots[2].pathPoints[0] = pathPoints[2];
        gridSlots[2].pathPoints[1] = pathPoints[15];
        gridSlots[2].pathPoints[2] = pathPoints[5];
        gridSlots[2].pathPoints[3] = pathPoints[14];

        gridSlots[3].pathPoints[0] = pathPoints[3];
        gridSlots[3].pathPoints[1] = pathPoints[17];
        gridSlots[3].pathPoints[2] = pathPoints[6];
        gridSlots[3].pathPoints[3] = pathPoints[16];

        gridSlots[4].pathPoints[0] = pathPoints[4];
        gridSlots[4].pathPoints[1] = pathPoints[18];
        gridSlots[4].pathPoints[2] = pathPoints[7];
        gridSlots[4].pathPoints[3] = pathPoints[17];

        gridSlots[5].pathPoints[0] = pathPoints[5];
        gridSlots[5].pathPoints[1] = pathPoints[19];
        gridSlots[5].pathPoints[2] = pathPoints[8];
        gridSlots[5].pathPoints[3] = pathPoints[18];

        gridSlots[6].pathPoints[0] = pathPoints[6];
        gridSlots[6].pathPoints[1] = pathPoints[21];
        gridSlots[6].pathPoints[2] = pathPoints[9];
        gridSlots[6].pathPoints[3] = pathPoints[20];

        gridSlots[7].pathPoints[0] = pathPoints[7];
        gridSlots[7].pathPoints[1] = pathPoints[22];
        gridSlots[7].pathPoints[2] = pathPoints[10];
        gridSlots[7].pathPoints[3] = pathPoints[21];

        gridSlots[8].pathPoints[0] = pathPoints[8];
        gridSlots[8].pathPoints[1] = pathPoints[23];
        gridSlots[8].pathPoints[2] = pathPoints[11];
        gridSlots[8].pathPoints[3] = pathPoints[22];

        entities[0]  = new Entity(0, pathPoints[0]);
        entities[1]  = new Entity(0, pathPoints[1]);
        entities[2]  = new Entity(0, pathPoints[2]);
        entities[3]  = new Entity(0, pathPoints[15]);
        entities[4]  = new Entity(0, pathPoints[19]);
        entities[5]  = new Entity(0, pathPoints[23]);
        entities[6]  = new Entity(0, pathPoints[11]);
        entities[7]  = new Entity(0, pathPoints[10]);
        entities[8]  = new Entity(0, pathPoints[9]);
        entities[9]  = new Entity(0, pathPoints[20]);
        entities[10] = new Entity(0, pathPoints[16]);
        entities[11] = new Entity(0, pathPoints[12]);
    }

    public void SolveQuest(Quest quest)
    {
        List<int> entitiesIndexesThatMustBeConnectedList = new List<int>();
        for (int i = 0; i < quest.entitiesToConnectIDs.Count; i++)
        {
            if (quest.entitiesToConnectIDs[i].entitiesIDs.Count > 1)
            {
                for (int j = 0; j < quest.entitiesToConnectIDs[i].entitiesIDs.Count; j++)
                {
                    entitiesIndexesThatMustBeConnectedList.Add(quest.entitiesToConnectIDs[i].entitiesIDs[j]);
                }
            }
        }

        currentTime = DateTime.Now;

        entitiesIndexesThatMustBeConnectedList.Sort();
        int[] entitiesIndexesThatMustBeConnected = entitiesIndexesThatMustBeConnectedList.Distinct().ToArray();

        List<int> tilesOccupied = new List<int>();
        List<int> tilesFree = new List<int>() { 0, 1, 2, 3, 4, 5, 6, 7, 8 };

        x = 0;

        ExploreFurther(0, tilesOccupied, tilesFree, quest);

        Debug.Log("MIAU");

        Debug.Log(gridSlots);

        PrintCurrentBoard();
    }

    DateTime currentTime;

    public void SolveCurrentQuest()
    {
        SolveQuest(GameManager.Instance.Quest);
    }

    public void PrintCurrentBoard()
    {
        for (int i = 0; i < 9; i++)
        {
            Debug.Log(gridSlots[i].tile.tileType + " " + gridSlots[i].tile.rotations);
        }
    }

    int x;

    bool ExploreFurther(int tilesUsed, List<int> tilesOccupied, List<int> tilesFree, Quest quest)
    {
        if (tilesUsed >= 8)
        {
            return CheckIfWon(quest);
        }

        if (currentTime.Second + 4 < DateTime.Now.Second || x > 10000)
        {
            Debug.Log(tilesUsed);
            Debug.Log(x);
            throw new Exception();
        }

        x++;

        TileVirtual tile = tiles[tilesUsed];

        tilesUsed++;

        for (int i = 0; i < tilesFree.Count; i++)
        {
            int tileIndex = tilesFree[i];

            gridSlots[tileIndex].tile = tiles[tilesUsed];

            tilesFree.RemoveAt(i);

            for (int j = 0; j < 4; j++)
            {
                Debug.Log(tilesUsed);

                PrintCurrentBoard();

                x++;

                if (currentTime.Second + 4 < DateTime.Now.Second || x > 100)
                {
                    Debug.Log(tilesUsed);
                    Debug.Log(x);
                    PrintCurrentBoard();

                    throw new Exception();
                }

                tile.rotations = j;

                if (!FastCheckIfConnectionsAreValid(tileIndex))
                {
                    return false;
                }

                bool won = ExploreFurther(tilesUsed, tilesOccupied, tilesFree, quest);

                if (won)
                {
                    return true;
                }
            }

            tilesFree.Add(tileIndex);

            gridSlots[tileIndex].tile = null;
        }

        return false;
    }

    bool FastCheckIfConnectionsAreValid(int gridSlotIndex)
    {
        return 
            gridSlots[gridSlotIndex].pathPoints[0].CheckIfPathOnTopValid() &&
            gridSlots[gridSlotIndex].pathPoints[1].CheckIfPathOnTopValid() && 
            gridSlots[gridSlotIndex].pathPoints[2].CheckIfPathOnTopValid() && 
            gridSlots[gridSlotIndex].pathPoints[3].CheckIfPathOnTopValid();
    }

    bool CheckIfWon(Quest quest)
    {
        if (quest.CheckIfCompleted(entities))
        {
            for (int i = 0; i < gridSlots.Length; i++)
            {
                if (gridSlots[i].tile == null)
                {
                    return false;
                }
            }
            for (int i = 0; i < pathPoints.Length; i++)
            {
                if (!pathPoints[i].CheckIfPathOnTopValid())
                {
                    return false;
                }
            }
            return true;
        }
        return false;
    }
}
