using Grid;
using Solver;
using UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

public sealed class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [SerializeField] private Entity[] entities;

    [FormerlySerializedAs("grid")] [SerializeField]
    private MyGrid myGrid;

    [SerializeField] private TilesSetup tilesSetup;

    [field: SerializeField]
    public Quest.Quest Quest { get; private set; }

    private void Awake()
    {
        Instance = this;

        // Ensure grid is initialized before anything else
        if (myGrid != null) myGrid.Initialize();
    }

    private void Start()
    {
        SetupEntities();
    }

    private void SetupEntities()
    {
        var pathPoints = myGrid.PathPoints;

        // Ensure entities array is properly sized
        if (entities == null || entities.Length != 12) entities = new Entity[12];

        // Initialize entities at their starting path points
        // Using proper entity IDs (not all 0 like the initial solver version)
        entities[0] = new Entity(0, pathPoints[0]);
        entities[1] = new Entity(1, pathPoints[1]);
        entities[2] = new Entity(2, pathPoints[2]);
        entities[3] = new Entity(3, pathPoints[15]);
        entities[4] = new Entity(4, pathPoints[19]);
        entities[5] = new Entity(5, pathPoints[23]);
        entities[6] = new Entity(6, pathPoints[11]);
        entities[7] = new Entity(7, pathPoints[10]);
        entities[8] = new Entity(8, pathPoints[9]);
        entities[9] = new Entity(9, pathPoints[20]);
        entities[10] = new Entity(10, pathPoints[16]);
        entities[11] = new Entity(11, pathPoints[12]);

        // Mark entity path points with their entity indices
        pathPoints[0].entityIndex = 0;
        pathPoints[1].entityIndex = 1;
        pathPoints[2].entityIndex = 2;
        pathPoints[15].entityIndex = 3;
        pathPoints[19].entityIndex = 4;
        pathPoints[23].entityIndex = 5;
        pathPoints[11].entityIndex = 6;
        pathPoints[10].entityIndex = 7;
        pathPoints[9].entityIndex = 8;
        pathPoints[20].entityIndex = 9;
        pathPoints[16].entityIndex = 10;
        pathPoints[12].entityIndex = 11;
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
        return myGrid.CheckIfValidPaths() && Quest.CheckIfCompleted(entities);
    }

    public void MoveMade()
    {
        myGrid.RecalculatePathConnections();

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