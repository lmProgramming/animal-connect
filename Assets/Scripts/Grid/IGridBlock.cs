using System.Collections.Generic;

namespace Grid
{
    /// <summary>
    /// Interface for GridBlock to enable testing
    /// </summary>
    public interface IGridBlock
    {
        List<List<int>> Connections { get; }
    }
}
