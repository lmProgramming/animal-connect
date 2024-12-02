using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Tile : MonoBehaviour
{
    [SerializeField]
    GridBlock gridBlock;
    public GridSlot slot;
    public int rotations;


    Image uiImage;
    Color defaultColor;
    private void Awake()
    {
        uiImage = GetComponent<Image>();
        defaultColor = uiImage.color;
    }

    public enum TileType
    {
        Curve,
        TwoCurves,
        TIntersection,
        XIntersection,
        Bridge
    }

    public TileType type;

    int rotationsLimit = 4;

    public Vector2 GetRestingPosition()
    {
        return slot.GetPosition();
    }

    public void ResetPosition()
    {
        transform.position = slot.GetPosition();
    }

    int framesToResetPosition = 0;
    public void ResetPositionAfterXFrames(int x)
    {
        framesToResetPosition = x;
    }

    private void Update()
    {
        framesToResetPosition--;
        if (framesToResetPosition == 0)
        {
            ResetPosition();
        }
    }

    public void Lighten()
    {
        uiImage.color = new Color(defaultColor.r + 0.1f, defaultColor.g + 0.1f, defaultColor.b + 0.1f);
    }

    public void ResetColor()
    {
        uiImage.color = defaultColor;
    }

    void Start()
    {
        gridBlock = new GridBlock();

        switch (type)
        {
            case TileType.Curve:
                gridBlock.connections.Add(new List<int>() { 1, 2 });
                break;
            case TileType.TwoCurves:
                rotationsLimit = 2;
                gridBlock.connections.Add(new List<int>() { 0, 3 });
                gridBlock.connections.Add(new List<int>() { 1, 2 });
                break;
            case TileType.TIntersection:
                gridBlock.connections.Add(new List<int>() { 0, 1, 2 });
                break;
            case TileType.XIntersection:
                rotationsLimit = 1;
                gridBlock.connections.Add(new List<int>() { 0, 1, 2, 3 });
                break;
            case TileType.Bridge:
                rotationsLimit = 1;
                gridBlock.connections.Add(new List<int>() { 0, 2 });
                gridBlock.connections.Add(new List<int>() { 1, 3 });
                break;
            default:
                Debug.LogWarning("No connections");
                break;
        }
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

        if (rotations == rotationsLimit)
        {
            rotations = 0;
        }

        transform.rotation = Quaternion.Euler(0, 0, rotations * -90);

        return rotations;
    }

    void OnMouseDown()
    {
        TileDragger.Instance.GrabThisTile(this);
    }

    public void ChangeSprite(Sprite sprite)
    {
       uiImage.sprite = sprite; 
    }
}
