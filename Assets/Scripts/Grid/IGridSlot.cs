using Solver;
using UnityEngine;

namespace Grid
{
    /// <summary>
    ///     Interface for GridSlot to enable testing
    /// </summary>
    public interface IGridSlot
    {
        IPathPoint[] PathPoints { get; set; }
        ITile GetTile();
        void UpdateTile(ITile newTile);
        void RemovedTile();
        Vector2Int GetGridPosition();
        Vector2 GetPosition();
    }
}