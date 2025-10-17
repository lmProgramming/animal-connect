namespace Solver
{
    /// <summary>
    /// Interface for Entity to enable testing without Unity dependencies
    /// </summary>
    public interface IEntity
    {
        int EntityIndex { get; }
        IPathPoint PathPoint { get; }
    }
}
