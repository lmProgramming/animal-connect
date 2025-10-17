namespace Solver
{
    /// <summary>
    /// Interface for PathPoint to enable testing without Unity dependencies
    /// </summary>
    public interface IPathPoint
    {
        int PathNum { get; set; }
        int EntityIndex { get; set; }
        
        void UpdatePathNum(int newNum);
        void Setup();
        void ResetConectionsNumber();
        void RaiseConnectionsNumber();
        bool CheckIfPathOnTopValid();
    }
}
