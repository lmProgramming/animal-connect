using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class EntitySetup : MonoBehaviour
{
    [SerializeField]
    EntitySprites sprites;

    [SerializeField]
    SpriteRenderer[] entitiesSpriteRenderers = new SpriteRenderer[12];

    [ExecuteInEditMode]
    private void Start()
    {
        for (int i = 0; i < entitiesSpriteRenderers.Length; i++)
        {
            entitiesSpriteRenderers[i].sprite = sprites.sprites[i];
        }
    }
}
