using System;
using System.Collections.Generic;
using Core.Logic;
using Core.Models;
using UnityEngine;

namespace Managers
{
    /// <summary>
    ///     Manages the authoritative game state.
    ///     Bridges between Unity components and core logic.
    ///     This adapter layer keeps Unity-specific code separate from game logic.
    /// </summary>
    public class GameStateManager : MonoBehaviour
    {
        [Header("Debug Options")]
        [SerializeField] private bool enableLogging = true;

        [SerializeField]
        private bool trackMoveHistory = true;

        // Prevent re-entrant initialization
        private bool _isInitializing;

        // Current state
        private List<MoveResult> _moveHistory;

        // Core logic components (no Unity dependencies)
        private MoveProcessor _moveProcessor;
        private PathCalculator _pathCalculator;
        private Stack<GameState> _stateStack; // For undo functionality

        // Properties
        public GameState CurrentState { get; private set; }

        public bool CanUndo => _stateStack is { Count: > 1 }; // Keep initial state
        public int MoveCount => _moveHistory?.Count ?? 0;
        public IReadOnlyList<MoveResult> MoveHistory => _moveHistory?.AsReadOnly();

        private void Awake()
        {
            InitializeLogicComponents();

            if (trackMoveHistory)
            {
                _moveHistory = new List<MoveResult>();
                _stateStack = new Stack<GameState>();
            }
        }

        // Events for loose coupling
        public event Action<GameState> OnStateChanged;
        public event Action<MoveResult> OnMoveMade;
        public event Action<GameState> OnGameWon;
        public event Action<ValidationResult> OnValidationChanged;

        /// <summary>
        ///     Initializes the core logic components.
        ///     These have no Unity dependencies and can be tested independently.
        /// </summary>
        private void InitializeLogicComponents()
        {
            _pathCalculator = new PathCalculator();

            // MoveProcessor creates its own instances
            _moveProcessor = new MoveProcessor();

            LogMessage("Game logic components initialized");
        }

        /// <summary>
        ///     Initializes the game with a quest and starting grid configuration.
        /// </summary>
        /// <param name="quest">The quest to complete</param>
        /// <param name="initialGrid">Optional initial grid state. If null, starts with empty grid.</param>
        public void Initialize(QuestData quest, GridState initialGrid = null)
        {
            LogMessage("GameStateManager.Initialize: START");

            // Prevent re-entrant calls
            if (_isInitializing)
            {
                LogMessage("GameStateManager.Initialize: Re-entrant call detected! Already initializing.");
                return;
            }

            _isInitializing = true;

            try
            {
                if (quest == null)
                {
                    Debug.LogError("Cannot initialize GameStateManager with null quest!");
                    return;
                }

                // Create initial grid if not provided
                if (initialGrid == null) initialGrid = new GridState();
                LogMessage("GameStateManager.Initialize: Grid created");

                // Calculate initial path network
                LogMessage("GameStateManager.Initialize: About to calculate path network...");
                var initialPaths = _pathCalculator.CalculatePathNetwork(initialGrid);
                LogMessage("GameStateManager.Initialize: Path network calculated");

                // Create initial game state
                LogMessage("GameStateManager.Initialize: Creating game state...");
                CurrentState = new GameState(initialGrid, initialPaths, quest);
                LogMessage("GameStateManager.Initialize: Game state created");

                // Initialize history tracking
                if (trackMoveHistory)
                {
                    _moveHistory.Clear();
                    _stateStack.Clear();
                    _stateStack.Push(CurrentState);
                }

                LogMessage("Game initialized with quest");

                // Notify listeners
                LogMessage("GameStateManager.Initialize: About to invoke OnStateChanged...");
                OnStateChanged?.Invoke(CurrentState);
                LogMessage("GameStateManager.Initialize: COMPLETE");
            }
            finally
            {
                _isInitializing = false;
            }
        }

        /// <summary>
        ///     Processes a player move and updates game state.
        /// </summary>
        /// <param name="move">The move to process</param>
        /// <returns>The result of the move including validation and quest status</returns>
        public MoveResult ProcessMove(Move move)
        {
            if (CurrentState == null)
            {
                Debug.LogError("Cannot process move: Game not initialized!");
                return default;
            }

            LogMessage($"Processing move: {move.Type} at slot {move.Slot}");

            // Process the move through core logic
            var result = _moveProcessor.ProcessMove(CurrentState, move);

            // Update current state
            CurrentState = result.NewState;

            // Track history
            if (trackMoveHistory)
            {
                _moveHistory.Add(result);
                _stateStack.Push(CurrentState);
            }

            // Fire events
            OnMoveMade?.Invoke(result);
            OnStateChanged?.Invoke(CurrentState);
            OnValidationChanged?.Invoke(result.Validation);

            // Check for win condition
            if (result.IsWinningMove)
            {
                LogMessage("WINNING MOVE! Quest completed!");
                OnGameWon?.Invoke(CurrentState);
            }
            else
            {
                LogMessage(
                    $"Move result - Valid: {result.Validation.IsValid}, Quest Complete: {result.QuestResult.IsComplete}");
            }

            return result;
        }

        /// <summary>
        ///     Attempts to process a move and returns whether it was successful.
        ///     Does not apply the move if it would be invalid.
        /// </summary>
        /// <param name="move">The move to validate and process</param>
        /// <returns>True if move was processed, false if invalid</returns>
        public bool TryProcessMove(Move move)
        {
            // Preview the move result
            var result = _moveProcessor.ProcessMove(CurrentState, move);

            // Only apply if the move doesn't create invalid paths
            if (result.Validation.HasErrors)
            {
                LogMessage($"Move rejected: {result.Validation.Errors.Count} validation errors");
                return false;
            }

            // Apply the move
            ProcessMove(move);
            return true;
        }

        /// <summary>
        ///     Previews what would happen if a move was made, without actually applying it.
        ///     Useful for AI, hints, or validating moves before committing.
        /// </summary>
        /// <param name="move">The move to preview</param>
        /// <returns>The result if the move were applied</returns>
        public MoveResult PreviewMove(Move move)
        {
            if (CurrentState == null)
            {
                Debug.LogError("Cannot preview move: Game not initialized!");
                return default;
            }

            return _moveProcessor.ProcessMove(CurrentState, move);
        }

        /// <summary>
        ///     Undoes the last move and restores previous state.
        /// </summary>
        /// <returns>True if undo was successful, false if no moves to undo</returns>
        public bool UndoLastMove()
        {
            if (!CanUndo)
            {
                LogMessage("Cannot undo: No moves to undo");
                return false;
            }

            // Pop current state
            _stateStack.Pop();

            // Restore previous state
            CurrentState = _stateStack.Peek();

            // Remove last move from history
            if (_moveHistory.Count > 0)
            {
                var undoneMove = _moveHistory[_moveHistory.Count - 1];
                _moveHistory.RemoveAt(_moveHistory.Count - 1);
                LogMessage($"Undid move: {undoneMove.Move.Type} at slot {undoneMove.Move.Slot}");
            }

            // Notify listeners
            OnStateChanged?.Invoke(CurrentState);

            return true;
        }

        /// <summary>
        ///     Resets the game to its initial state.
        /// </summary>
        public void ResetToInitialState()
        {
            if (_stateStack == null || _stateStack.Count == 0)
            {
                Debug.LogError("Cannot reset: No initial state stored");
                return;
            }

            // Clear history and restore initial state
            _moveHistory?.Clear();

            // Get initial state (bottom of stack)
            var statesArray = _stateStack.ToArray();
            var initialState = statesArray[statesArray.Length - 1];

            _stateStack.Clear();
            _stateStack.Push(initialState);
            CurrentState = initialState;

            LogMessage("Game reset to initial state");

            // Notify listeners
            OnStateChanged?.Invoke(CurrentState);
        }

        /// <summary>
        ///     Gets the current validation result for the current state.
        /// </summary>
        public ValidationResult GetCurrentValidation()
        {
            if (CurrentState == null) return new ValidationResult(new List<ValidationError>());

            var validator = new ConnectionValidator();
            return validator.ValidateConnections(CurrentState.Paths);
        }

        /// <summary>
        ///     Gets the current quest evaluation result.
        /// </summary>
        public QuestResult GetCurrentQuestResult()
        {
            if (CurrentState == null) return QuestResult.Incomplete("Game not initialized");

            var evaluator = new QuestEvaluator();
            return evaluator.EvaluateQuest(CurrentState.Quest, CurrentState.Paths);
        }

        /// <summary>
        ///     Gets the completion progress of the current quest (0.0 to 1.0).
        /// </summary>
        public float GetQuestProgress()
        {
            if (CurrentState == null) return 0f;

            var evaluator = new QuestEvaluator();
            return evaluator.GetCompletionProgress(CurrentState.Quest, CurrentState.Paths);
        }

        private void LogMessage(string message)
        {
            if (enableLogging) Debug.Log($"[GameStateManager] {message}");
        }

        #region Serialization Support (Future: Save/Load)

        /// <summary>
        ///     Serializes the current game state for saving.
        /// </summary>
        public string SerializeState()
        {
            if (CurrentState == null)
            {
                Debug.LogWarning("Cannot serialize: No current state");
                return null;
            }

            // TODO: Implement proper serialization
            // For now, return JSON representation
            return JsonUtility.ToJson(CurrentState);
        }

        /// <summary>
        ///     Deserializes and loads a saved game state.
        /// </summary>
        public bool DeserializeState(string serializedState)
        {
            if (string.IsNullOrEmpty(serializedState))
            {
                Debug.LogError("Cannot deserialize: Empty or null state data");
                return false;
            }

            try
            {
                // TODO: Implement proper deserialization
                var state = JsonUtility.FromJson<GameState>(serializedState);
                CurrentState = state;

                // Reset history
                if (trackMoveHistory)
                {
                    _moveHistory.Clear();
                    _stateStack.Clear();
                    _stateStack.Push(CurrentState);
                }

                OnStateChanged?.Invoke(CurrentState);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to deserialize state: {e.Message}");
                return false;
            }
        }

        #endregion
    }
}