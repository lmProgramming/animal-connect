using Solver;
using UnityEngine;

public class EntityVisual : MonoBehaviour
{
    public string entityName;

    public Entity entity;

    private void Awake()
    {
        entity.pathPoint = GetComponent<PathPoint>();
    }
}