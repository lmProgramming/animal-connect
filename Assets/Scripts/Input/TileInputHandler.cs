using System;
using AnimalConnect.Managers;
using AnimalConnect.Views;
using Core.Models;
using UnityEngine;

namespace AnimalConnect.Input
{
    /// <summary>
    ///     Handles tile interaction and converts user input to Move objects.
    ///     No game logic - just input translation and event emission.
    ///     Supports both tap-to-rotate and drag-to-swap interactions.
    /// </summary>
    public class TileInputHandler : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private GameStateManager _stateManager;

        [SerializeField] private GridView _gridView;
        [SerializeField] private Camera _mainCamera;

        [Header("Input Settings")]
        [SerializeField] private float _tapTimeThreshold = 0.2f; // Max time for tap vs drag

        [SerializeField] private float _dragDistanceThreshold = 0.5f; // Min distance to be considered a drag
        [SerializeField] private bool _enableVisualFeedback = true;

        [Header("Debug")]
        [SerializeField] private bool _logInputEvents;

        private int _draggedSlot = -1;

        // Input state
        private TileView _draggedTile;
        private int _hoveredSlot = -1;
        private bool _isDragging;
        private Vector2 _pointerDownPosition;
        private float _pointerDownTime;

        private void Awake()
        {
            if (_mainCamera == null) _mainCamera = Camera.main;

            if (_stateManager == null) _stateManager = FindFirstObjectByType<GameStateManager>();

            if (_gridView == null) _gridView = FindFirstObjectByType<GridView>();

            ValidateDependencies();
        }

        private void Update()
        {
            if (_mainCamera == null || _gridView == null) return;

            // Skip if pointer is over UI
            // if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

            HandleInput();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_tapTimeThreshold < 0) _tapTimeThreshold = 0;

            if (_dragDistanceThreshold < 0) _dragDistanceThreshold = 0;
        }
#endif

        // Events
        public event Action<Move> OnMoveRequested;
        public event Action<int> OnTileSelected;
        public event Action OnTileDeselected;

        private void ValidateDependencies()
        {
            if (_stateManager == null) Debug.LogError("TileInputHandler: GameStateManager not assigned!");

            if (_gridView == null) Debug.LogError("TileInputHandler: GridView not assigned!");

            if (_mainCamera == null) Debug.LogError("TileInputHandler: No camera found!");
        }

        private void HandleInput()
        {
            if (UnityEngine.Input.GetMouseButtonDown(0))
                HandlePointerDown();
            else if (UnityEngine.Input.GetMouseButton(0) && _draggedTile != null)
                HandleDrag();
            else if (UnityEngine.Input.GetMouseButtonUp(0) && _draggedTile != null)
                HandlePointerUp();
            else if (_draggedTile == null) HandleHover();
        }

        private void HandlePointerDown()
        {
            var worldPos = GetMouseWorldPosition();
            var slot = _gridView.GetSlotAtPosition(worldPos);

            if (slot == -1)
            {
                LogInput("Pointer down: No slot at position");
                return;
            }

            var tile = _gridView.GetTileAt(slot);
            if (tile == null)
            {
                LogInput($"Pointer down: No tile at slot {slot}");
                return;
            }

            // Start interaction
            _draggedTile = tile;
            _draggedSlot = slot;
            _pointerDownTime = Time.time;
            _pointerDownPosition = worldPos;
            _isDragging = false;

            LogInput($"Pointer down: Grabbed tile at slot {slot}");

            // Visual feedback
            if (_enableVisualFeedback) _draggedTile.PlayPressEffect();

            OnTileSelected?.Invoke(slot);
        }

        private void HandleDrag()
        {
            var currentPos = GetMouseWorldPosition();
            var dragDistance = Vector2.Distance(_pointerDownPosition, currentPos);

            // Check if we've moved far enough to be considered dragging
            if (!_isDragging && dragDistance > _dragDistanceThreshold)
            {
                _isDragging = true;
                LogInput("Drag started");

                if (_enableVisualFeedback) _draggedTile.SetAlpha(0.7f);
            }

            if (_isDragging)
            {
                // Update tile position to follow pointer
                _draggedTile.SetPosition(currentPos, false);

                // Highlight target slot
                var targetSlot = _gridView.GetSlotAtPosition(currentPos);
                if (targetSlot != _hoveredSlot)
                {
                    _gridView.ClearAllHighlights();
                    if (targetSlot != -1 && targetSlot != _draggedSlot) _gridView.HighlightSlot(targetSlot, true);
                    _hoveredSlot = targetSlot;
                }
            }
        }

        private void HandlePointerUp()
        {
            var interactionTime = Time.time - _pointerDownTime;
            var releasePos = GetMouseWorldPosition();

            Move move;

            if (!_isDragging && interactionTime < _tapTimeThreshold)
            {
                // Quick tap = rotate
                move = CreateRotateMove(_draggedSlot);
                LogInput($"Tap detected: Rotating tile at slot {_draggedSlot}");
            }
            else if (_isDragging)
            {
                // Drag = swap
                var targetSlot = _gridView.GetSlotAtPosition(releasePos);

                if (targetSlot != -1 && targetSlot != _draggedSlot)
                {
                    move = CreateSwapMove(_draggedSlot, targetSlot);
                    LogInput($"Drag detected: Swapping slots {_draggedSlot} and {targetSlot}");
                }
                else
                {
                    // Dragged but released on invalid target - cancel
                    LogInput("Drag cancelled: Invalid target");
                    ResetDraggedTile();
                    CleanupDragState();
                    return;
                }
            }
            else
            {
                // Held but not moved enough - treat as rotate
                move = CreateRotateMove(_draggedSlot);
                LogInput($"Hold detected: Rotating tile at slot {_draggedSlot}");
            }

            // Reset tile visuals before applying move
            ResetDraggedTile();

            // Request the move
            OnMoveRequested?.Invoke(move);

            // Cleanup
            CleanupDragState();
        }

        private void HandleHover()
        {
            var worldPos = GetMouseWorldPosition();
            var slot = _gridView.GetSlotAtPosition(worldPos);

            if (slot != _hoveredSlot)
            {
                // Reset previous hovered tile
                if (_hoveredSlot != -1)
                {
                    var prevTile = _gridView.GetTileAt(_hoveredSlot);
                    if (prevTile != null && _enableVisualFeedback) prevTile.ResetEffect();
                }

                // Highlight new tile
                _hoveredSlot = slot;
                if (slot != -1)
                {
                    var tile = _gridView.GetTileAt(slot);
                    if (tile != null && _enableVisualFeedback) tile.PlayHoverEffect();
                }
            }
        }

        private Move CreateRotateMove(int slot)
        {
            if (_stateManager == null || _stateManager.CurrentState == null)
            {
                Debug.LogError("Cannot create rotate move: No current state");
                return default;
            }

            var currentTile = _stateManager.CurrentState.Grid.GetTile(slot);
            if (!currentTile.HasValue)
            {
                Debug.LogError($"Cannot create rotate move: No tile at slot {slot}");
                return default;
            }

            // Get max rotations for this tile type and wrap around correctly
            int maxRotations = currentTile.Value.GetMaxRotations();
            var newRotation = (currentTile.Value.Rotation + 1) % maxRotations;

            return Move.Rotate(slot, newRotation);
        }

        private Move CreateSwapMove(int fromSlot, int toSlot)
        {
            return Move.Swap(fromSlot, toSlot);
        }

        private void ResetDraggedTile()
        {
            if (_draggedTile == null) return;

            // Reset visuals
            if (_enableVisualFeedback)
            {
                _draggedTile.SetAlpha(1f);
                _draggedTile.ResetEffect();
            }

            // Reset position to original slot
            var originalPosition = _gridView.GetSlotPosition(_draggedSlot);
            _draggedTile.SetPosition(originalPosition);
        }

        private void CleanupDragState()
        {
            _draggedTile = null;
            _draggedSlot = -1;
            _isDragging = false;
            _gridView.ClearAllHighlights();
            _hoveredSlot = -1;

            OnTileDeselected?.Invoke();
        }

        private Vector2 GetMouseWorldPosition()
        {
            if (_mainCamera == null) return Vector2.zero;

            var mousePos = UnityEngine.Input.mousePosition;
            return _mainCamera.ScreenToWorldPoint(mousePos);
        }

        private void LogInput(string message)
        {
            if (_logInputEvents) Debug.Log($"[TileInputHandler] {message}");
        }

        #region Public Control Methods

        /// <summary>
        ///     Enables or disables input handling.
        /// </summary>
        public void SetInputEnabled(bool enabled)
        {
            this.enabled = enabled;

            if (!enabled)
                // Clean up any ongoing interactions
                if (_draggedTile != null)
                {
                    ResetDraggedTile();
                    CleanupDragState();
                }
        }

        /// <summary>
        ///     Cancels any ongoing drag operation.
        /// </summary>
        public void CancelDrag()
        {
            if (_draggedTile != null)
            {
                ResetDraggedTile();
                CleanupDragState();
            }
        }

        #endregion
    }
}