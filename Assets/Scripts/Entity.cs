using Solver;
using UnityEngine;

public class EntityWithSprite : MonoBehaviour
{
    public string entityName;

    public Entity entity;

    private void Awake()
    {
        entity.pathPoint = GetComponent<PathPoint>();
    }
}