using UnityEngine;

namespace Grid
{
    public interface IGrid
    {
        void Initialize();
        IGridSlot GetGridSlot(Vector2Int gridPos);
        ITile GetTile(Vector2Int gridPos);
        bool CheckIfValidPaths();
        void RecalculatePathConnections();
        int GetNewPathNumber();
        void MergePaths(int pathNum1, int pathNum2);
    }
}