using System;
using System.Collections.Generic;

namespace Grid
{
    [Serializable]
    public class GridBlock
    {
        public List<List<int>> Connections;

        public GridBlock()
        {
            Connections = new List<List<int>>();
        }

        public GridBlock(List<List<int>> connections)
        {
            Connections = connections;
        }
    }
}