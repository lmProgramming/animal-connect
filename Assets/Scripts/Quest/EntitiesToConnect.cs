using System;
using System.Collections.Generic;

namespace Quest
{
    [Serializable]
    public class EntitiesToConnect
    {
        public List<int> entitiesIDs = new();

        // this means that this is only a group of objects that don't need to be connected together, but must be unconnected to something
        public bool onlyAClump;
    }
}