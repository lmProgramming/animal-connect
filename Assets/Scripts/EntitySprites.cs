using UnityEngine;

[CreateAssetMenu(fileName = "EntitySprites", menuName = "Animal Connect/Entity Sprites", order = 2)]
public class EntitySprites : ScriptableObject
{
    [field: SerializeField]
    public Sprite[] Sprites { get; private set; } = new Sprite[12];
}