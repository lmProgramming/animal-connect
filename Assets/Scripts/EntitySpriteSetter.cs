using UnityEngine;
using UnityEngine.UI;

public class EntitySpriteSetter : MonoBehaviour
{
    [field: SerializeField]
    public EntitySprites EntitySprites { get; private set; }

    [SerializeField]
    private Image[] entityImages = new Image[12];

    private void Start()
    {
        var sprites = EntitySprites.Sprites;

        for (var i = 0; i < sprites.Length; i++) entityImages[i].sprite = sprites[i];
    }
}