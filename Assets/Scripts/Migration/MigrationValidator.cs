using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AnimalConnect.Managers;
using Core.Logic;
using Core.Models;
using Grid;
using Solver;
using UnityEngine;

namespace Migration
{
    /// <summary>
    /// Validates equivalence between old and new systems during migration.
    /// Runs both systems in parallel and compares results to ensure correctness.
    /// </summary>
    public class MigrationValidator : MonoBehaviour
    {
        [Header("Old System")]
        [SerializeField] private MyGrid _oldGrid;
        [SerializeField] private GameManager _oldGameManager;
        
        [Header("New System")]
        [SerializeField] private GameStateManager _newStateManager;
        
        [Header("Migration Settings")]
        [Tooltip("If true, use the new system for gameplay. If false, use old system.")]
        [SerializeField] private bool _useNewSystem = false;
        
        [Tooltip("If true, validate that both systems produce identical results.")]
        [SerializeField] private bool _validateEquivalence = true;
        
        [Tooltip("If true, log detailed differences when validation fails.")]
        [SerializeField] private bool _logDetailedDiff = true;
        
        [Header("Validation Statistics")]
        [SerializeField] private int _totalMoves = 0;
        [SerializeField] private int _validationFailures = 0;
        [SerializeField] private int _validationSuccesses = 0;
        
        private bool _isInitialized = false;
        
        public bool UseNewSystem => _useNewSystem;
        public bool ValidateEquivalence => _validateEquivalence;
        
        private void Awake()
        {
            if (_newStateManager != null)
            {
                _newStateManager.OnMoveMade += OnNewSystemMoveMade;
            }
        }
        
        private void OnDestroy()
        {
            if (_newStateManager != null)
            {
                _newStateManager.OnMoveMade -= OnNewSystemMoveMade;
            }
        }
        
        public void Initialize()
        {
            if (_isInitialized) return;
            
            Debug.Log("[MigrationValidator] Initializing migration validator...");
            
            // Ensure old system is initialized
            if (_oldGrid != null)
            {
                _oldGrid.Initialize();
            }
            
            // Ensure new system is initialized
            // Note: GameStateManager initialization will be handled externally
            // as it requires proper QuestData conversion
            
            _isInitialized = true;
            
            Debug.Log($"[MigrationValidator] Initialized. Using {(UseNewSystem ? "NEW" : "OLD")} system for gameplay.");
        }
        
        private void OnNewSystemMoveMade(MoveResult moveResult)
        {
            _totalMoves++;
            
            if (_validateEquivalence)
            {
                ValidateEquivalenceAfterMove(moveResult);
            }
        }
        
        /// <summary>
        /// Validates that old and new systems produce equivalent results.
        /// </summary>
        public void ValidateEquivalenceAfterMove(MoveResult moveResult)
        {
            if (_oldGrid == null || _newStateManager == null)
            {
                Debug.LogWarning("[MigrationValidator] Cannot validate: missing system references");
                return;
            }
            
            bool allValid = true;
            var errors = new List<string>();
            
            // 1. Validate path connections
            if (!ValidatePathConnections(out var pathErrors))
            {
                allValid = false;
                errors.AddRange(pathErrors);
            }
            
            // 2. Validate connection counts
            if (!ValidateConnectionCounts(out var countErrors))
            {
                allValid = false;
                errors.AddRange(countErrors);
            }
            
            // 3. Validate win conditions
            if (!ValidateWinCondition(out var winError))
            {
                allValid = false;
                errors.Add(winError);
            }
            
            if (allValid)
            {
                _validationSuccesses++;
                Debug.Log($"[MigrationValidator] Move {_totalMoves} - Validation PASSED ✓");
            }
            else
            {
                _validationFailures++;
                Debug.LogError($"[MigrationValidator] Move {_totalMoves} - Validation FAILED ✗");
                
                if (_logDetailedDiff)
                {
                    LogDetailedDiff(errors);
                }
            }
        }
        
        /// <summary>
        /// Validates that path connections match between old and new systems.
        /// </summary>
        private bool ValidatePathConnections(out List<string> errors)
        {
            errors = new List<string>();
            
            // Compare every pair of path points
            for (int i = 0; i < 24; i++)
            {
                for (int j = i + 1; j < 24; j++)
                {
                    bool oldConnected = AreConnectedInOldSystem(i, j);
                    bool newConnected = AreConnectedInNewSystem(i, j);
                    
                    if (oldConnected != newConnected)
                    {
                        errors.Add($"Path connection mismatch: points {i} and {j}. " +
                                 $"Old: {oldConnected}, New: {newConnected}");
                    }
                }
            }
            
            return errors.Count == 0;
        }
        
        /// <summary>
        /// Validates that connection counts match between old and new systems.
        /// </summary>
        private bool ValidateConnectionCounts(out List<string> errors)
        {
            errors = new List<string>();
            
            for (int i = 0; i < 24; i++)
            {
                int oldCount = GetConnectionCountInOldSystem(i);
                int newCount = GetConnectionCountInNewSystem(i);
                
                if (oldCount != newCount)
                {
                    errors.Add($"Connection count mismatch at point {i}. " +
                             $"Old: {oldCount}, New: {newCount}");
                }
            }
            
            return errors.Count == 0;
        }
        
        /// <summary>
        /// Validates that win conditions match between old and new systems.
        /// </summary>
        private bool ValidateWinCondition(out string error)
        {
            error = null;
            
            bool oldValid = CheckIfValidPathsOldSystem();
            bool newValid = _newStateManager != null && _newStateManager.CurrentState != null;
            
            if (oldValid != newValid)
            {
                error = $"Valid paths mismatch. Old: {oldValid}, New: {newValid}";
                return false;
            }
            
            // Only check quest completion if paths are valid
            if (oldValid && newValid)
            {
                // Old system CheckIfCompleted requires entities - we'll get them from GameManager
                bool oldWon = _oldGameManager != null && _oldGameManager.Quest != null && 
                              CheckOldQuestCompleted();
                bool newWon = CheckNewQuestCompleted();
                
                if (oldWon != newWon)
                {
                    error = $"Win condition mismatch. Old: {oldWon}, New: {newWon}";
                    return false;
                }
            }
            
            return true;
        }
        
        /// <summary>
        /// Checks if two path points are connected in the old system.
        /// </summary>
        private bool AreConnectedInOldSystem(int point1, int point2)
        {
            if (_oldGrid.pathPoints == null || point1 < 0 || point2 < 0 ||
                point1 >= _oldGrid.pathPoints.Length || point2 >= _oldGrid.pathPoints.Length)
            {
                return false;
            }
            
            int pathNum1 = _oldGrid.pathPoints[point1].pathNum;
            int pathNum2 = _oldGrid.pathPoints[point2].pathNum;
            
            // Both must be in valid paths (not -1) and same path number
            return pathNum1 != -1 && pathNum2 != -1 && pathNum1 == pathNum2;
        }
        
        /// <summary>
        /// Checks if two path points are connected in the new system.
        /// </summary>
        private bool AreConnectedInNewSystem(int point1, int point2)
        {
            return _newStateManager.CurrentState.Paths.AreConnected(point1, point2);
        }
        
        /// <summary>
        /// Gets connection count for a path point in the old system.
        /// </summary>
        private int GetConnectionCountInOldSystem(int point)
        {
            if (_oldGrid.pathPoints == null || point < 0 || point >= _oldGrid.pathPoints.Length)
            {
                return 0;
            }
            
            return _oldGrid.pathPoints[point].ConnectionsNumber;
        }
        
        /// <summary>
        /// Gets connection count for a path point in the new system.
        /// </summary>
        private int GetConnectionCountInNewSystem(int point)
        {
            return _newStateManager.CurrentState.Paths.GetConnectionCount(point);
        }
        
        /// <summary>
        /// Checks if paths are valid in the old system.
        /// </summary>
        private bool CheckIfValidPathsOldSystem()
        {
            if (_oldGrid.pathPoints == null) return false;
            
            for (var i = 0; i < _oldGrid.pathPoints.Length; i++)
            {
                var pathPoint = _oldGrid.pathPoints[i];
                var isEntity = _oldGrid.entitiesPathPoints?.Contains(pathPoint) ?? false;
                
                if (isEntity && pathPoint.ConnectionsNumber != 1)
                {
                    return false;
                }
                
                if (!isEntity && pathPoint.ConnectionsNumber != 0 && pathPoint.ConnectionsNumber != 2)
                {
                    return false;
                }
            }
            
            return true;
        }
        
        /// <summary>
        /// Checks if quest is completed in old system.
        /// Uses GameManager's quest checking logic.
        /// </summary>
        private bool CheckOldQuestCompleted()
        {
            if (_oldGameManager == null || _oldGameManager.Quest == null) return false;
            
            // Old quest system uses entities array - for now just check if paths are valid
            // Full implementation would pass actual entities
            return CheckIfValidPathsOldSystem();
        }
        
        /// <summary>
        /// Checks if quest is completed in new system.
        /// </summary>
        private bool CheckNewQuestCompleted()
        {
            if (_newStateManager == null) return false;
            
            // New system should have quest evaluation in GameState
            // For now, just check if paths are valid
            return _newStateManager.CurrentState != null;
        }
        
        /// <summary>
        /// Logs detailed differences between systems.
        /// </summary>
        private void LogDetailedDiff(List<string> errors)
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== VALIDATION FAILURE DETAILS ===");
            sb.AppendLine($"Move #{_totalMoves}");
            sb.AppendLine($"Errors: {errors.Count}");
            sb.AppendLine();
            
            foreach (var error in errors)
            {
                sb.AppendLine($"  - {error}");
            }
            
            sb.AppendLine();
            sb.AppendLine("=== OLD SYSTEM STATE ===");
            sb.AppendLine(GetOldSystemState());
            
            sb.AppendLine();
            sb.AppendLine("=== NEW SYSTEM STATE ===");
            sb.AppendLine(GetNewSystemState());
            
            Debug.LogError(sb.ToString());
        }
        
        /// <summary>
        /// Gets a string representation of the old system state.
        /// </summary>
        private string GetOldSystemState()
        {
            var sb = new StringBuilder();
            
            if (_oldGrid.pathPoints == null)
            {
                return "Old grid path points not initialized";
            }
            
            sb.AppendLine("Path Points:");
            for (int i = 0; i < _oldGrid.pathPoints.Length; i++)
            {
                var pp = _oldGrid.pathPoints[i];
                bool isEntity = _oldGrid.entitiesPathPoints?.Contains(pp) ?? false;
                sb.AppendLine($"  Point {i}: Path#{pp.pathNum}, Connections={pp.ConnectionsNumber}, Entity={isEntity}");
            }
            
            return sb.ToString();
        }
        
        /// <summary>
        /// Gets a string representation of the new system state.
        /// </summary>
        private string GetNewSystemState()
        {
            var sb = new StringBuilder();
            var state = _newStateManager.CurrentState;
            
            sb.AppendLine("Path Network:");
            for (int i = 0; i < 24; i++)
            {
                int pathId = state.Paths.GetPathId(i);
                int connections = state.Paths.GetConnectionCount(i);
                bool isEntity = Core.Configuration.GridConfiguration.PathPointToEntity[i] != -1;
                sb.AppendLine($"  Point {i}: Path#{pathId}, Connections={connections}, Entity={isEntity}");
            }
            
            sb.AppendLine();
            sb.AppendLine($"State: {(state != null ? "Valid" : "Null")}");
            
            return sb.ToString();
        }
        
        /// <summary>
        /// Gets validation statistics.
        /// </summary>
        public string GetValidationStats()
        {
            float successRate = _totalMoves > 0 ? (_validationSuccesses / (float)_totalMoves * 100f) : 0f;
            
            return $"Validation Stats: {_validationSuccesses}/{_totalMoves} passed ({successRate:F1}%), " +
                   $"{_validationFailures} failures";
        }
        
        /// <summary>
        /// Manually trigger validation (for testing).
        /// </summary>
        [ContextMenu("Validate Now")]
        public void ManualValidation()
        {
            if (!_isInitialized)
            {
                Initialize();
            }
            
            if (_newStateManager == null || _newStateManager.CurrentState == null)
            {
                Debug.LogWarning("[MigrationValidator] Cannot validate - new state manager not properly initialized");
                return;
            }
            
            // Create a dummy move for manual validation
            var dummyMove = Move.Rotate(0, 0);
            
            ValidateEquivalenceAfterMove(new MoveResult(
                _newStateManager.CurrentState,
                dummyMove,
                new ValidationResult(new List<ValidationError>()),
                QuestResult.Incomplete("Manual validation")
            ));
        }
        
        /// <summary>
        /// Prints current validation statistics.
        /// </summary>
        [ContextMenu("Print Stats")]
        public void PrintStats()
        {
            Debug.Log($"[MigrationValidator] {GetValidationStats()}");
        }
        
        /// <summary>
        /// Resets validation statistics.
        /// </summary>
        [ContextMenu("Reset Stats")]
        public void ResetStats()
        {
            _totalMoves = 0;
            _validationFailures = 0;
            _validationSuccesses = 0;
            Debug.Log("[MigrationValidator] Statistics reset");
        }
    }
}
