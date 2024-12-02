using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EntityWithSprite : MonoBehaviour
{
    public string entityName;

    public Entity entity;

    void Awake()
    {
        entity.pathPoint = GetComponent<PathPoint>();
    }
}
