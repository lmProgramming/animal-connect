using UnityEngine;
using UnityEngine.UI;

public class EntitySetup : MonoBehaviour
{
    [SerializeField] private EntitySprites sprites;

    [SerializeField] private Image[] entitiesSpriteRenderers = new Image[12];

    private void Start()
    {
        for (var i = 0; i < entitiesSpriteRenderers.Length; i++) entitiesSpriteRenderers[i].sprite = sprites.Sprites[i];
    }
}