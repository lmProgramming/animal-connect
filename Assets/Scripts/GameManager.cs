using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [SerializeField]
    Entity[] entities;

    [SerializeField]
    Quest quest;

    [SerializeField]
    Grid grid;

    [SerializeField]
    TilesSetup tilesSetup;

    public Quest Quest { get => quest; private set => quest = value; }

    void Awake()
    {
        Instance = this;
    }

    public void SetupQuest(Quest quest)
    {
        Quest = quest;

        StartPuzzle();
    }

    public void StartPuzzle()
    {
        UIManager.Instance.questVisualizer.GenerateVisualization(Quest);

        tilesSetup.Setup();
    }

    public bool CheckIfWon()
    {
        if (grid.ChechIfValidPaths())
        {
            return Quest.CheckIfCompleted(entities);
        }
        return false;
    }

    public void MoveMade()
    {
        grid.RecalculatePathConnections();

        if (CheckIfWon())
        {
            Won();
        }
    }

    public void Won()
    {
        Debug.Log("WON");

        ReloadScene();
    }

    public void ReloadScene()
    {
        SceneManager.LoadScene("MainGame");
    }
}
