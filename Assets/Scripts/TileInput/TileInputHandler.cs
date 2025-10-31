using System;
using Core.Models;
using Managers;
using UnityEngine;
using Views;

namespace TileInput
{
    /// <summary>
    ///     Handles tile interaction and converts user input to Move objects.
    ///     No game logic - just input translation and event emission.
    ///     Supports both tap-to-rotate and drag-to-swap interactions.
    /// </summary>
    public class TileInputHandler : MonoBehaviour
    {
        private const float PointerUpCooldown = 0.1f; // Prevent immediate re-grab after release

        [Header("Dependencies")]
        [SerializeField] private GameStateManager stateManager;

        [SerializeField]
        private GridView gridView;

        [SerializeField]
        private Camera mainCamera;

        [Header("Input Settings")]
        [SerializeField] private float tapTimeThreshold = 0.2f; // Max time for tap vs drag

        [SerializeField]
        private float dragDistanceThreshold = 0.5f; // Min distance to be considered a drag

        [SerializeField]
        private bool enableVisualFeedback = true;

        [Header("Debug")]
        [SerializeField] private bool logInputEvents;

        private int _draggedSlot = -1;

        // Input state
        private TileView _draggedTile;
        private int? _hoveredSlot;
        private bool _isDragging;
        private float _lastPointerUpTime;
        private Vector2 _pointerDownPosition;
        private float _pointerDownTime;
        private int? _previouslyHoveredSlot;
        private TileView _previouslyHoveredTile;

        private void Awake()
        {
            if (mainCamera == null) mainCamera = Camera.main;

            if (stateManager == null) stateManager = FindFirstObjectByType<GameStateManager>();

            if (gridView == null) gridView = FindFirstObjectByType<GridView>();

            ValidateDependencies();
        }

        private void Update()
        {
            if (!mainCamera || !gridView) return;

            HandleInput();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (tapTimeThreshold < 0) tapTimeThreshold = 0;

            if (dragDistanceThreshold < 0) dragDistanceThreshold = 0;
        }
#endif

        // Events
        public event Action<Move> OnMoveRequested;
        public event Action<int> OnTileSelected;
        public event Action OnTileDeselected;

        private void ValidateDependencies()
        {
            if (stateManager == null) Debug.LogError("TileInputHandler: GameStateManager not assigned!");

            if (gridView == null) Debug.LogError("TileInputHandler: GridView not assigned!");

            if (mainCamera == null) Debug.LogError("TileInputHandler: No camera found!");
        }

        private void HandleInput()
        {
            if (Input.GetMouseButtonDown(0))
                HandlePointerDown();
            else if (Input.GetMouseButton(0) && _draggedTile != null)
                HandleDrag();
            else if (Input.GetMouseButtonUp(0) && _draggedTile != null)
                HandlePointerUp();
            else if (_draggedTile == null) HandleHover();
        }

        private void HandlePointerDown()
        {
            if (Time.time - _lastPointerUpTime < PointerUpCooldown)
            {
                LogInput("Pointer down: Too soon after pointer up, ignoring");
                return;
            }

            var worldPos = GetMouseWorldPosition();
            var slot = gridView.GetSlotAtPosition(worldPos);

            if (!slot.HasValue)
            {
                LogInput("Pointer down: No slot at position");
                return;
            }

            var tile = gridView.GetTileAt(slot.Value);
            if (!tile)
            {
                LogInput($"Pointer down: No tile at slot {slot}");
                return;
            }

            // Start interaction
            _draggedTile = tile;
            _draggedSlot = slot.Value;
            _pointerDownTime = Time.time;
            _pointerDownPosition = worldPos;
            _isDragging = false;

            LogInput($"Pointer down: Grabbed tile at slot {slot}");

            // Visual feedback
            if (enableVisualFeedback) _draggedTile.PlayPressEffect();

            OnTileSelected?.Invoke(slot.Value);
        }

        private void HandleDrag()
        {
            var currentPos = GetMouseWorldPosition();
            var dragDistance = Vector2.Distance(_pointerDownPosition, currentPos);

            // Check if we've moved far enough to be considered dragging
            if (!_isDragging && dragDistance > dragDistanceThreshold)
            {
                _isDragging = true;
                LogInput("Drag started");

                if (enableVisualFeedback) _draggedTile.SetAlpha(0.7f);
            }

            if (!_isDragging) return;

            // Update tile position to follow pointer
            _draggedTile.SetPosition(currentPos, false);

            // Check for visual swap with hovered tile
            var targetSlot = gridView.GetSlotAtPosition(currentPos);

            if (targetSlot == _hoveredSlot) return;

            // Reset previous hovered tile to its original position
            if (_previouslyHoveredTile && _previouslyHoveredSlot.HasValue)
            {
                var originalPos = gridView.GetSlotPosition(_previouslyHoveredSlot.Value);
                _previouslyHoveredTile.SetPosition(originalPos); // Animate back with DOTween
                _previouslyHoveredTile = null;
                _previouslyHoveredSlot = null;
            }

            gridView.ClearAllHighlights();

            if (targetSlot.HasValue && targetSlot != _draggedSlot)
            {
                gridView.HighlightSlot(targetSlot.Value, true);

                // Visually swap: move the hovered tile to dragged tile's original position
                var hoveredTile = gridView.GetTileAt(targetSlot.Value);
                if (hoveredTile)
                {
                    var draggedOriginalPos = gridView.GetSlotPosition(_draggedSlot);
                    hoveredTile.SetPosition(draggedOriginalPos);

                    // Track this tile so we can reset it later
                    _previouslyHoveredTile = hoveredTile;
                    _previouslyHoveredSlot = targetSlot;
                }
            }

            _hoveredSlot = targetSlot;
        }

        private void HandlePointerUp()
        {
            var interactionTime = Time.time - _pointerDownTime;
            var releasePos = GetMouseWorldPosition();

            Move move;
            var shouldCleanupHoveredTile = true;

            switch (_isDragging)
            {
                case false when interactionTime < tapTimeThreshold:
                    // Quick tap = rotate
                    move = CreateRotateMove(_draggedSlot);
                    LogInput($"Tap detected: Rotating tile at slot {_draggedSlot}");
                    break;
                case true:
                {
                    // Drag = swap
                    var targetSlot = gridView.GetSlotAtPosition(releasePos);

                    if (targetSlot.HasValue && targetSlot != _draggedSlot)
                    {
                        move = CreateSwapMove(_draggedSlot, targetSlot.Value);
                        LogInput($"Drag detected: Swapping slots {_draggedSlot} and {targetSlot}");

                        // For a valid swap, DON'T reset the hovered tile - it's already in the right visual position!
                        // The grid update will handle final positioning
                        shouldCleanupHoveredTile = false;
                    }
                    else
                    {
                        // Dragged but released on invalid target - cancel
                        LogInput("Drag cancelled: Invalid target");
                        ResetDraggedTile();
                        CleanupDragState(); // This will reset the hovered tile
                        return;
                    }

                    break;
                }
                default:
                    ResetDraggedTile();
                    CleanupDragState();
                    return;
            }

            // Reset tile visuals (alpha, scale effects)
            ResetDraggedTile(false);

            // Request the move (this may trigger immediate grid updates)
            OnMoveRequested?.Invoke(move);

            // Record the pointer up time for cooldown
            _lastPointerUpTime = Time.time;

            // Final cleanup - conditionally clean hovered tile state
            if (shouldCleanupHoveredTile)
            {
                CleanupDragState();
            }
            else
            {
                // For valid swaps, just clear references without resetting positions
                _previouslyHoveredTile = null;
                _previouslyHoveredSlot = null;
                _draggedTile = null;
                _draggedSlot = -1;
                _isDragging = false;
                gridView.ClearAllHighlights();
                _hoveredSlot = null;
                OnTileDeselected?.Invoke();
            }
        }

        private void HandleHover()
        {
            var worldPos = GetMouseWorldPosition();
            var slot = gridView.GetSlotAtPosition(worldPos);

            if (slot == _hoveredSlot) return;

            // Reset previous hovered tile
            if (_hoveredSlot.HasValue)
            {
                var prevTile = gridView.GetTileAt(_hoveredSlot.Value);
                if (prevTile && enableVisualFeedback) prevTile.ResetEffect();
            }

            // Highlight new tile
            _hoveredSlot = slot;
            if (slot.HasValue)
            {
                var tile = gridView.GetTileAt(slot.Value);
                if (tile && enableVisualFeedback) tile.PlayHoverEffect();
            }
        }

        private Move CreateRotateMove(int slot)
        {
            if (!stateManager || stateManager.CurrentState == null)
            {
                Debug.LogError("Cannot create rotate move: No current state");
                return default;
            }

            var currentTile = stateManager.CurrentState.Grid.GetTile(slot);
            if (!currentTile.HasValue)
            {
                Debug.LogError($"Cannot create rotate move: No tile at slot {slot}");
                return default;
            }

            // Get max rotations for this tile type and wrap around correctly
            var maxRotations = currentTile.Value.GetMaxRotations();
            var newRotation = (currentTile.Value.Rotation + 1) % maxRotations;

            return Move.Rotate(slot, newRotation);
        }

        private Move CreateSwapMove(int fromSlot, int toSlot)
        {
            return Move.Swap(fromSlot, toSlot);
        }

        private void ResetDraggedTile(bool resetPosition = true)
        {
            if (!_draggedTile) return;

            // Reset visuals
            if (enableVisualFeedback)
            {
                _draggedTile.SetAlpha(1f);
                _draggedTile.ResetEffect();
            }

            // Optionally reset position to original slot
            // For valid swaps, we skip this to avoid awkward movement back and forth
            if (resetPosition)
            {
                var originalPosition = gridView.GetSlotPosition(_draggedSlot);
                _draggedTile.SetPosition(originalPosition);
            }
        }

        private void CleanupVisualSwapState()
        {
            // Reset previously hovered tile to its original position
            // This must be called BEFORE the actual game state changes to avoid wrong positions
            if (_previouslyHoveredTile && _previouslyHoveredSlot.HasValue)
            {
                var originalPos = gridView.GetSlotPosition(_previouslyHoveredSlot.Value);
                _previouslyHoveredTile.SetPosition(originalPos, false); // No animation to avoid conflicts
                _previouslyHoveredTile = null;
                _previouslyHoveredSlot = null;
            }
        }

        private void CleanupDragState()
        {
            // Clean up any remaining visual swap state
            CleanupVisualSwapState();

            _draggedTile = null;
            _draggedSlot = -1;
            _isDragging = false;
            gridView.ClearAllHighlights();
            _hoveredSlot = null;

            OnTileDeselected?.Invoke();
        }

        private Vector2 GetMouseWorldPosition()
        {
            if (!mainCamera) return Vector2.zero;

            var mousePos = Input.mousePosition;
            return mainCamera.ScreenToWorldPoint(mousePos);
        }

        private void LogInput(string message)
        {
            if (logInputEvents) Debug.Log($"[TileInputHandler] {message}");
        }

        #region Public Control Methods

        /// <summary>
        ///     Enables or disables input handling.
        /// </summary>
        public void SetInputEnabled(bool newEnabled)
        {
            enabled = newEnabled;

            if (newEnabled) return;
            if (_draggedTile == null) return;

            // Clean up any ongoing interactions
            ResetDraggedTile();
            CleanupDragState();
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