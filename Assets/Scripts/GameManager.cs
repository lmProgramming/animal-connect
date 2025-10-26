using Core.Models;
using Managers;
using TileInput;
using UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using Views;

public sealed class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    private static int _stateChangeCallCount;

    [Header("Core Systems")]
    [SerializeField] private GameStateManager stateManager;

    [SerializeField]
    private GridView gridView;

    [SerializeField]
    private TileInputHandler inputHandler;

    [Header("Setup")]
    [SerializeField] private TilesSetup tilesSetup;

    [Header("Quest")]
    [field: SerializeField]
    public Quest.Quest Quest { get; private set; }

    private void Awake()
    {
        Instance = this;

        // Subscribe to state manager events
        if (stateManager != null)
        {
            stateManager.OnStateChanged += OnGameStateChanged;
            stateManager.OnGameWon += OnGameWon;
        }
        else
        {
            Debug.LogError("GameManager: GameStateManager not assigned!");
        }

        // Subscribe to input handler events
        if (inputHandler != null)
            inputHandler.OnMoveRequested += OnMoveRequested;
        else
            Debug.LogError("GameManager: TileInputHandler not assigned!");
    }

    private void OnDestroy()
    {
        // Unsubscribe from events
        if (stateManager != null)
        {
            stateManager.OnStateChanged -= OnGameStateChanged;
            stateManager.OnGameWon -= OnGameWon;
        }

        if (inputHandler != null) inputHandler.OnMoveRequested -= OnMoveRequested;
    }

    public void SetupQuest(Quest.Quest quest)
    {
        Quest = quest;
        StartPuzzle();
    }

    private void StartPuzzle()
    {
        // Convert Quest to QuestData
        var questData = ConvertQuestToQuestData(Quest);

        // Setup tiles and initialize game state
        if (tilesSetup)
            tilesSetup.Setup(stateManager, questData);
        else
            Debug.LogError("GameManager: TilesSetup not assigned!");

        // Update UI
        if (UIManager.Instance) UIManager.Instance.questVisualizer.GenerateVisualization(Quest);
    }

    private static QuestData ConvertQuestToQuestData(Quest.Quest quest)
    {
        return quest.ToQuestData();
    }

    private void OnMoveRequested(Move move)
    {
        _stateChangeCallCount = 0;
        if (stateManager) stateManager.ProcessMove(move);
    }

    private void OnGameStateChanged(GameState newState)
    {
        _stateChangeCallCount++;

        if (_stateChangeCallCount > 100)
        {
            Debug.LogError(
                $"INFINITE LOOP DETECTED: {_stateChangeCallCount} calls - OnGameStateChanged called more than 100 times!");
            return;
        }

        // Update grid view
        if (gridView) gridView.UpdateFromState(newState);
    }

    private static void OnGameWon(GameState winningState)
    {
        ReloadScene();
    }

    private static void ReloadScene()
    {
        SceneManager.LoadScene("MainGame");
    }
}