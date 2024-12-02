using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class PathPoint
{
    public int pathNum = -1;
    public int entityIndex = -1;

    [SerializeField]
    int connectionsNumber = 0;

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
        if (entityIndex != -1)
        {
            connectionsNumber = 1;
        }
    }

    public void ResetConectionsNumber()
    {
        if (entityIndex != -1)
        {
            connectionsNumber = 1;
        }
        else
        {
            connectionsNumber = 0;
        }
    }

    public void RaiseConnectionsNumber()
    {
        connectionsNumber++;
    }

    public bool CheckIfPathOnTopValid()
    {
        if (entityIndex != -1)
        {
            return true;
        }
        else
        {
            return connectionsNumber == 0 || connectionsNumber == 2;
        }
    }
}
