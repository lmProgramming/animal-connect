using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class EntitiesToConnect
{
    public List<int> entitiesIDs;
    // this means that this is only a group of objects that don't need to be connected together, but must be unconnected to something
    public bool onlyAClump = false;

    public EntitiesToConnect() 
    {
        entitiesIDs = new List<int>();
    }
}
