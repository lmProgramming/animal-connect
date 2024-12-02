using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    public QuestVisualizer questVisualizer;

    private void Awake()
    {
        Instance = this;
    }
}
