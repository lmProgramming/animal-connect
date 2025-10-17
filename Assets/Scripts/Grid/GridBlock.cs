using System;
using System.Collections.Generic;

namespace Grid
{
    [Serializable]
    public class GridBlock : IGridBlock
    {
        public List<List<int>> Connections { get; set; }

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