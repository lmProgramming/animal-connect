using System;
using System.Linq;
using AnimalConnect.Managers;
using Core.Logic;
using Core.Models;
using Grid;
using UnityEngine;

namespace Migration
{
    /// <summary>
    /// Adapter that provides a unified interface for both old and new systems.
    /// Allows seamless switching between implementations during migration.
    /// </summary>
    public class SystemAdapter : MonoBehaviour
    {
        [Header("System References")]
        [SerializeField] private MyGrid _oldGrid;
        [SerializeField] private GameManager _oldGameManager;
        [SerializeField] private GameStateManager _newStateManager;
        [SerializeField] private MigrationValidator _migrationValidator;
        
        [Header("Active System")]
        [SerializeField] private bool _useNewSystem = false;
        
        // Events that work with both systems
        public event Action OnGameInitialized;
        public event Action OnMoveCompleted;
        public event Action OnGameWon;
        public event Action OnValidationFailed;
        
        private bool _isInitialized = false;
        
        public bool UseNewSystem
        {
            get => _useNewSystem;
            set
            {
                if (_useNewSystem != value)
                {
                    _useNewSystem = value;
                    Debug.Log($"[SystemAdapter] Switched to {(value ? "NEW" : "OLD")} system");
                }
            }
        }
        
        private void Awake()
        {
            // Subscribe to new system events
            if (_newStateManager != null)
            {
                _newStateManager.OnMoveMade += OnNewSystemMoveMade;
                _newStateManager.OnGameWon += OnNewSystemGameWon;
                _newStateManager.OnStateChanged += OnNewSystemStateChanged;
            }
        }
        
        private void OnDestroy()
        {
            if (_newStateManager != null)
            {
                _newStateManager.OnMoveMade -= OnNewSystemMoveMade;
                _newStateManager.OnGameWon -= OnNewSystemGameWon;
                _newStateManager.OnStateChanged -= OnNewSystemStateChanged;
            }
        }
        
        /// <summary>
        /// Initializes the active system.
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized) return;
            
            Debug.Log($"[SystemAdapter] Initializing with {(UseNewSystem ? "NEW" : "OLD")} system");
            
            // Initialize migration validator
            if (_migrationValidator != null)
            {
                _migrationValidator.Initialize();
            }
            
            // Initialize appropriate system
            if (UseNewSystem)
            {
                InitializeNewSystem();
            }
            else
            {
                InitializeOldSystem();
            }
            
            _isInitialized = true;
            OnGameInitialized?.Invoke();
        }
        
        private void InitializeOldSystem()
        {
            if (_oldGrid != null)
            {
                _oldGrid.Initialize();
            }
            
            Debug.Log("[SystemAdapter] Old system initialized");
        }
        
        private void InitializeNewSystem()
        {
            if (_newStateManager != null && _oldGameManager != null && _oldGameManager.Quest != null)
            {
                // TODO: Convert old Quest to QuestData when needed
                // For now, skip initialization or create default QuestData
                // _newStateManager.Initialize(questData);
                Debug.LogWarning("[SystemAdapter] New system initialization without quest conversion - implement when needed");
            }
            else
            {
                Debug.LogError("[SystemAdapter] Cannot initialize new system - missing dependencies");
            }
        }
        
        /// <summary>
        /// Processes a tile rotation through the active system.
        /// </summary>
        public void RotateTile(int slotIndex)
        {
            if (UseNewSystem)
            {
                RotateTileNewSystem(slotIndex);
            }
            else
            {
                RotateTileOldSystem(slotIndex);
            }
        }
        
        private void RotateTileOldSystem(int slotIndex)
        {
            if (_oldGrid == null || slotIndex < 0 || slotIndex >= _oldGrid.gridSlots.Length)
            {
                Debug.LogError($"[SystemAdapter] Cannot rotate tile in old system: invalid slot {slotIndex}");
                return;
            }
            
            var slot = _oldGrid.gridSlots[slotIndex];
            if (slot != null && slot.Tile != null)
            {
                slot.Tile.Rotate();
                _oldGrid.RecalculatePathConnections();
                OnMoveCompleted?.Invoke();
                
                // Check win condition - simplified for now
                if (_oldGameManager != null && _oldGrid.CheckIfValidPaths())
                {
                    // TODO: Implement proper quest completion check
                    // OnGameWon?.Invoke();
                }
            }
        }
        
        private void RotateTileNewSystem(int slotIndex)
        {
            if (_newStateManager == null)
            {
                Debug.LogError("[SystemAdapter] Cannot rotate tile in new system: missing state manager");
                return;
            }
            
            var currentTile = _newStateManager.CurrentState.Grid.GetTile(slotIndex);
            if (!currentTile.HasValue)
            {
                Debug.LogError($"[SystemAdapter] Cannot rotate tile: no tile at slot {slotIndex}");
                return;
            }
            
            int newRotation = (currentTile.Value.Rotation + 1) % 4;
            var move = Move.Rotate(slotIndex, newRotation);
            _newStateManager.ProcessMove(move);
        }
        
        /// <summary>
        /// Processes a tile swap through the active system.
        /// </summary>
        public void SwapTiles(int slot1, int slot2)
        {
            if (UseNewSystem)
            {
                SwapTilesNewSystem(slot1, slot2);
            }
            else
            {
                SwapTilesOldSystem(slot1, slot2);
            }
        }
        
        private void SwapTilesOldSystem(int slot1, int slot2)
        {
            if (_oldGrid == null || 
                slot1 < 0 || slot1 >= _oldGrid.gridSlots.Length ||
                slot2 < 0 || slot2 >= _oldGrid.gridSlots.Length)
            {
                Debug.LogError($"[SystemAdapter] Cannot swap tiles in old system: invalid slots {slot1}, {slot2}");
                return;
            }
            
            var gridSlot1 = _oldGrid.gridSlots[slot1];
            var gridSlot2 = _oldGrid.gridSlots[slot2];
            
            if (gridSlot1 != null && gridSlot2 != null)
            {
                // Swap tiles using UpdateTile method
                var tempTile = gridSlot1.Tile;
                var tile2 = gridSlot2.Tile;
                
                if (tempTile != null && tile2 != null)
                {
                    gridSlot1.UpdateTile(tile2);
                    gridSlot2.UpdateTile(tempTile);
                    
                    // Update positions
                    tile2.transform.position = gridSlot1.transform.position;
                    tempTile.transform.position = gridSlot2.transform.position;
                }
                
                _oldGrid.RecalculatePathConnections();
                OnMoveCompleted?.Invoke();
                
                // Check win condition - simplified for now
                if (_oldGameManager != null && _oldGrid.CheckIfValidPaths())
                {
                    // TODO: Implement proper quest completion check
                    // OnGameWon?.Invoke();
                }
            }
        }
        
        private void SwapTilesNewSystem(int slot1, int slot2)
        {
            if (_newStateManager == null)
            {
                Debug.LogError("[SystemAdapter] Cannot swap tiles in new system: missing state manager");
                return;
            }
            
            var move = Move.Swap(slot1, slot2);
            _newStateManager.ProcessMove(move);
        }
        
        /// <summary>
        /// Checks if the current game state is a winning state.
        /// </summary>
        public bool IsGameWon()
        {
            if (UseNewSystem)
            {
                // TODO: Implement proper victory check in new system
                return _newStateManager != null && _newStateManager.CurrentState != null;
            }
            else
            {
                // TODO: Implement proper quest completion check for old system
                return _oldGameManager != null && 
                       _oldGrid != null &&
                       _oldGrid.CheckIfValidPaths();
            }
        }
        
        /// <summary>
        /// Checks if the current path configuration is valid.
        /// </summary>
        public bool IsValid()
        {
            if (UseNewSystem)
            {
                // TODO: Implement proper validation check in new system
                return _newStateManager != null && _newStateManager.CurrentState != null;
            }
            else
            {
                return _oldGrid != null && _oldGrid.CheckIfValidPaths();
            }
        }
        
        /// <summary>
        /// Gets the current state as a string (for debugging).
        /// </summary>
        public string GetStateDescription()
        {
            if (UseNewSystem)
            {
                if (_newStateManager == null) return "New system not available";
                
                var state = _newStateManager.CurrentState;
                // Count non-null tiles
                int tileCount = 0;
                if (state?.Grid != null)
                {
                    for (int i = 0; i < GridState.TotalSlots; i++)
                    {
                        if (state.Grid.GetTile(i) != null) tileCount++;
                    }
                }
                return $"New System - State exists: {state != null}, Tiles: {tileCount}";
            }
            else
            {
                if (_oldGrid == null) return "Old system not available";
                
                int tileCount = _oldGrid.gridSlots.Count(slot => slot.Tile != null);
                bool valid = _oldGrid.CheckIfValidPaths();
                // TODO: Add proper quest completion check with entities
                bool won = false;
                
                return $"Old System - Valid: {valid}, Victory: {won}, Tiles: {tileCount}";
            }
        }
        
        // Event handlers for new system
        private void OnNewSystemMoveMade(MoveResult moveResult)
        {
            OnMoveCompleted?.Invoke();
            
            if (!moveResult.Validation.IsValid && _migrationValidator != null && _migrationValidator.ValidateEquivalence)
            {
                OnValidationFailed?.Invoke();
            }
        }
        
        private void OnNewSystemGameWon(GameState finalState)
        {
            OnGameWon?.Invoke();
        }
        
        private void OnNewSystemStateChanged(GameState newState)
        {
            // Could update UI or other systems here
        }
        
        /// <summary>
        /// Switches to the new system at runtime.
        /// </summary>
        [ContextMenu("Switch to New System")]
        public void SwitchToNewSystem()
        {
            UseNewSystem = true;
        }
        
        /// <summary>
        /// Switches to the old system at runtime.
        /// </summary>
        [ContextMenu("Switch to Old System")]
        public void SwitchToOldSystem()
        {
            UseNewSystem = false;
        }
        
        /// <summary>
        /// Prints current system status.
        /// </summary>
        [ContextMenu("Print Status")]
        public void PrintStatus()
        {
            Debug.Log($"[SystemAdapter] {GetStateDescription()}");
        }
    }
}
