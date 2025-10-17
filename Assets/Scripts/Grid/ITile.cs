namespace Grid
{
    /// <summary>
    /// Interface for Tile to enable testing without Unity MonoBehaviour
    /// </summary>
    public interface ITile
    {
        int Rotations { get; }
        IGridBlock GetGridBlock();
        int Rotate();
    }
}
