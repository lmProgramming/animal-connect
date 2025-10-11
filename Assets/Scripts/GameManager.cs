using Grid;
using Solver;
using UI;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [SerializeField] private Entity[] entities;

    [SerializeField] private MyGrid grid;

    [SerializeField] private TilesSetup tilesSetup;

    [field: SerializeField]
    public Quest.Quest Quest { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    public void SetupQuest(Quest.Quest quest)
    {
        Quest = quest;

        StartPuzzle();
    }

    private void StartPuzzle()
    {
        UIManager.Instance.questVisualizer.GenerateVisualization(Quest);

        tilesSetup.Setup();
    }

    public bool CheckIfWon()
    {
        return grid.ChechIfValidPaths() && Quest.CheckIfCompleted(entities);
    }

    public void MoveMade()
    {
        grid.RecalculatePathConnections();

        if (CheckIfWon()) Won();
    }

    public void Won()
    {
        Debug.Log("WON");

        ReloadScene();
    }

    private static void ReloadScene()
    {
        SceneManager.LoadScene("MainGame");
    }
}