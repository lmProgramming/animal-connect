using System.Collections.Generic;
using Grid;
using UnityEngine;
using UnityEngine.UI;

public class Tile : MonoBehaviour
{
    public enum TileType
    {
        Curve,
        TwoCurves,
        Intersection,
        XIntersection,
        Bridge
    }

    [SerializeField] private GridBlock gridBlock;
    public GridSlot slot;
    public int rotations;

    public TileType type;
    private Color _defaultColor;

    private int _framesToResetPosition;

    private int _rotationsLimit = 4;


    private Image _uiImage;

    private void Awake()
    {
        _uiImage = GetComponent<Image>();
        _defaultColor = _uiImage.color;
    }

    private void Start()
    {
        gridBlock = new GridBlock();

        switch (type)
        {
            case TileType.Curve:
                gridBlock.Connections.Add(new List<int> { 1, 2 });
                break;
            case TileType.TwoCurves:
                _rotationsLimit = 2;
                gridBlock.Connections.Add(new List<int> { 0, 3 });
                gridBlock.Connections.Add(new List<int> { 1, 2 });
                break;
            case TileType.Intersection:
                gridBlock.Connections.Add(new List<int> { 0, 1, 2 });
                break;
            case TileType.XIntersection:
                _rotationsLimit = 1;
                gridBlock.Connections.Add(new List<int> { 0, 1, 2, 3 });
                break;
            case TileType.Bridge:
                _rotationsLimit = 1;
                gridBlock.Connections.Add(new List<int> { 0, 2 });
                gridBlock.Connections.Add(new List<int> { 1, 3 });
                break;
            default:
                Debug.LogWarning("No connections");
                break;
        }
    }

    private void Update()
    {
        _framesToResetPosition--;
        if (_framesToResetPosition == 0) ResetPosition();
    }

    private void OnMouseDown()
    {
        TileDragger.Instance.GrabThisTile(this);
    }

    public Vector2 GetRestingPosition()
    {
        return slot.GetPosition();
    }

    public void ResetPosition()
    {
        transform.position = slot.GetPosition();
    }

    public void ResetPositionAfterXFrames(int x)
    {
        _framesToResetPosition = x;
    }

    public void Lighten()
    {
        _uiImage.color = new Color(_defaultColor.r + 0.1f, _defaultColor.g + 0.1f, _defaultColor.b + 0.1f);
    }

    public void ResetColor()
    {
        _uiImage.color = _defaultColor;
    }

    public void SetGridBlock(GridBlock newGridBlock)
    {
        gridBlock = newGridBlock;
    }

    public GridBlock GetGridBlock()
    {
        return gridBlock;
    }

    public int Rotate()
    {
        rotations += 1;

        if (rotations == _rotationsLimit) rotations = 0;

        transform.rotation = Quaternion.Euler(0, 0, rotations * -90);

        return rotations;
    }

    public void ChangeSprite(Sprite sprite)
    {
        _uiImage.sprite = sprite;
    }
}