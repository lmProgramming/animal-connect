using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class GridBlock
{
    public List<List<int>> connections;

    public GridBlock()
    {
        connections = new List<List<int>>();
    }

    public GridBlock(List<List<int>> connections)
    {
        this.connections = connections;
    }
}
