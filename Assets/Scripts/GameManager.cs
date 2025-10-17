using System.Collections.Generic;
using UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using AnimalConnect.Managers;
using AnimalConnect.Views;
using Core.Models;

public sealed class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Core Systems")]
    [SerializeField] private GameStateManager _stateManager;
    [SerializeField] private GridView _gridView;
    
    [Header("Setup")]
    [SerializeField] private TilesSetup _tilesSetup;
    
    [Header("Quest")]
    [field: SerializeField]
    public Quest.Quest Quest { get; private set; }

    private void Awake()
    {
        Instance = this;
        
        // Subscribe to state manager events
        if (_stateManager != null)
        {
            _stateManager.OnStateChanged += OnGameStateChanged;
            _stateManager.OnGameWon += OnGameWon;
        }
        else
        {
            Debug.LogError("GameManager: GameStateManager not assigned!");
        }
    }
    
    private void OnDestroy()
    {
        // Unsubscribe from events
        if (_stateManager != null)
        {
            _stateManager.OnStateChanged -= OnGameStateChanged;
            _stateManager.OnGameWon -= OnGameWon;
        }
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
        if (_tilesSetup != null)
        {
            _tilesSetup.Setup(_stateManager, questData);
        }
        else
        {
            Debug.LogError("GameManager: TilesSetup not assigned!");
        }
        
        // Update UI
        if (UIManager.Instance != null)
        {
            UIManager.Instance.questVisualizer.GenerateVisualization(Quest);
        }
    }
    
    private QuestData ConvertQuestToQuestData(Quest.Quest quest)
    {
        // Convert Quest to QuestData using entity groups
        var entityGroups = new List<EntityGroup>();
        
        // TODO: This needs to be implemented based on your Quest structure
        // For now, create a simple example quest
        // You'll need to extract the actual entity requirements from your Quest object
        
        // Example: Connect entities 0, 1, 2 together
        entityGroups.Add(new EntityGroup(new[] { 0, 1, 2 }, false));
        
        return new QuestData(entityGroups);
    }
    
    private void OnGameStateChanged(GameState newState)
    {
        // Update grid view
        if (_gridView != null)
        {
            _gridView.UpdateFromState(newState);
        }
        
        Debug.Log($"State updated - Move {newState.MoveCount}");
    }
    
    private void OnGameWon(GameState winningState)
    {
        Debug.Log("🎉 GAME WON!");
        ReloadScene();
    }

    private static void ReloadScene()
    {
        SceneManager.LoadScene("MainGame");
    }
}