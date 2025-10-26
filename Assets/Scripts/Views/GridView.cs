using System.Collections.Generic;
using System.Linq;
using Core.Models;
using UnityEngine;
using UnityEngine.UI;

namespace Views
{
    /// <summary>
    ///     Manages the visual representation of the entire grid.
    ///     Syncs with GameState but contains no game logic.
    ///     Responsible for creating, updating, and removing tile views.
    /// </summary>
    public class GridView : MonoBehaviour
    {
        [Header("Grid Configuration")]
        [SerializeField] private GridSlotView[] slotViews = new GridSlotView[9];

        [Header("Tile Configuration")]
        [SerializeField] private TileView tilePrefab;

        [SerializeField]
        private TileSprites tileSprites;

        [SerializeField]
        private Transform tileContainer;

        [Header("Animation Settings")]
        [SerializeField] private bool animateTileChanges = true;

        [SerializeField]
        private bool animateInitialPlacement;

        private readonly Dictionary<int, TileView> _tileViews = new();
        private GridState _lastGridState; // Track previous state to detect swaps

        public IReadOnlyDictionary<int, TileView> TileViews => _tileViews;

        private void Awake()
        {
            Initialize();
        }

        /// <summary>
        ///     Initializes the grid view.
        /// </summary>
        public void Initialize()
        {
            // Validate slot views
            if (slotViews is not { Length: 9 })
            {
                Debug.LogError("GridView: Must have exactly 9 slot views!");
                return;
            }

            // Set slot indices if not already set
            for (var i = 0; i < slotViews.Length; i++)
                if (slotViews[i] != null && slotViews[i].SlotIndex != i)
                    slotViews[i].SlotIndex = i;

            // Use this GameObject as container if not set
            if (tileContainer == null) tileContainer = transform;

            // Validate prefab
            if (tilePrefab == null) Debug.LogError("GridView: Tile prefab not assigned!");

            // Clear any existing tiles
            ClearAllTiles();

            Debug.Log("GridView initialized");
        }

        /// <summary>
        ///     Updates the entire grid view from a game state.
        ///     This is the main synchronization method.
        /// </summary>
        public void UpdateFromState(GameState state)
        {
            if (state == null)
            {
                Debug.LogWarning("GridView: Cannot update from null state");
                return;
            }

            UpdateFromGrid(state.Grid);
        }

        /// <summary>
        ///     Updates the grid view from a grid state.
        /// </summary>
        private void UpdateFromGrid(GridState gridState)
        {
            if (gridState == null)
            {
                Debug.LogWarning("GridView: Cannot update from null grid state");
                return;
            }

            // Detect swaps by comparing old and new states
            if (_lastGridState != null) DetectAndApplySwaps(_lastGridState, gridState);

            // Now update each slot
            for (var slot = 0; slot < 9; slot++)
            {
                var tileData = gridState.GetTile(slot);

                if (tileData.HasValue)
                {
                    if (_tileViews.TryGetValue(slot, out var existingTile))
                        // Tile exists at this slot - update its properties
                        UpdateTileProperties(existingTile, tileData.Value);
                    else
                        // No tile at this slot - create one
                        CreateTileAtSlot(slot, tileData.Value);
                }
                else
                {
                    // Slot should be empty
                    RemoveTileView(slot);
                }
            }

            _lastGridState = gridState;
        }

        /// <summary>
        ///     Detects swaps between two grid states and physically moves the GameObjects.
        /// </summary>
        private void DetectAndApplySwaps(GridState oldState, GridState newState)
        {
            // Find pairs of slots that swapped
            for (var i = 0; i < 9; i++)
            for (var j = i + 1; j < 9; j++)
            {
                var oldI = oldState.GetTile(i);
                var oldJ = oldState.GetTile(j);
                var newI = newState.GetTile(i);
                var newJ = newState.GetTile(j);

                // Check if slots i and j swapped their contents
                // They must have changed AND swapped
                if (TileDataEquals(oldI, newI) ||
                    TileDataEquals(oldJ, newJ) ||
                    !TileDataEquals(oldI, newJ) ||
                    !TileDataEquals(oldJ, newI)) continue;
                // Swap detected! Physically swap the GameObjects
                SwapTileViews(i, j);
                return; // Only one swap per update
            }
        }

        /// <summary>
        ///     Checks if two nullable TileData are equal.
        /// </summary>
        private static bool TileDataEquals(TileData? a, TileData? b)
        {
            if (!a.HasValue && !b.HasValue) return true;
            if (!a.HasValue || !b.HasValue) return false;
            return a.Value.Type == b.Value.Type && a.Value.Rotation == b.Value.Rotation;
        }

        /// <summary>
        ///     Physically swaps two tile GameObjects between slots.
        /// </summary>
        private void SwapTileViews(int slot1, int slot2)
        {
            var tile1 = _tileViews.ContainsKey(slot1) ? _tileViews[slot1] : null;
            var tile2 = _tileViews.ContainsKey(slot2) ? _tileViews[slot2] : null;

            if (tile1 && tile2)
            {
                // Both slots have tiles - swap them
                tile1.SetPosition(GetSlotPosition(slot2), animateTileChanges);
                tile1.SlotIndex = slot2;

                tile2.SetPosition(GetSlotPosition(slot1), animateTileChanges);
                tile2.SlotIndex = slot1;

                _tileViews[slot1] = tile2;
                _tileViews[slot2] = tile1;
            }
            else if (tile1)
            {
                // Only slot1 has a tile - move it to slot2
                tile1.SetPosition(GetSlotPosition(slot2), animateTileChanges);
                tile1.SlotIndex = slot2;

                _tileViews.Remove(slot1);
                _tileViews[slot2] = tile1;

                if (slotViews[slot1])
                    slotViews[slot1].SetOccupied(false);
                if (slotViews[slot2])
                    slotViews[slot2].SetOccupied(true);
            }
            else if (tile2)
            {
                // Only slot2 has a tile - move it to slot1
                tile2.SetPosition(GetSlotPosition(slot1), animateTileChanges);
                tile2.SlotIndex = slot1;

                _tileViews.Remove(slot2);
                _tileViews[slot1] = tile2;

                if (slotViews[slot2])
                    slotViews[slot2].SetOccupied(false);
                if (slotViews[slot1])
                    slotViews[slot1].SetOccupied(true);
            }
        }

        /// <summary>
        ///     Updates a tile's visual properties (type and rotation).
        /// </summary>
        private void UpdateTileProperties(TileView tile, TileData tileData)
        {
            // Update type if changed
            if (tile.CurrentType != tileData.Type)
                tile.SetType(tileData.Type, animateTileChanges);

            // Update rotation if changed
            if (tile.CurrentRotation != tileData.Rotation)
                tile.SetRotation(tileData.Rotation, animateTileChanges);
        }

        /// <summary>
        ///     Creates a new tile at the specified slot.
        /// </summary>
        private void CreateTileAtSlot(int slot, TileData tileData)
        {
            var tile = CreateTileView(slot);
            if (!tile) return;

            tile.Initialize(tileData.Type, tileData.Rotation, GetSlotPosition(slot), animateInitialPlacement);

            _tileViews[slot] = tile;

            if (slotViews[slot])
                slotViews[slot].SetOccupied(true);
        }

        /// <summary>
        ///     Removes a tile view from the specified slot.
        /// </summary>
        private void RemoveTileView(int slot)
        {
            if (_tileViews.TryGetValue(slot, out var tileView))
            {
                Destroy(tileView.gameObject);
                _tileViews.Remove(slot);

                // Update slot occupied state
                if (slotViews[slot]) slotViews[slot].SetOccupied(false);
            }
        }

        /// <summary>
        ///     Creates a new tile view at the specified slot.
        /// </summary>
        private TileView CreateTileView(int slot)
        {
            if (!tilePrefab)
            {
                Debug.LogError("GridView: Cannot create tile - prefab not assigned!");
                return null;
            }

            var position = GetSlotPosition(slot);
            var tileView = Instantiate(tilePrefab, position, Quaternion.identity, tileContainer);
            tileView.name = $"Tile_{slot}";
            tileView.SlotIndex = slot;

            if (!tileSprites || !tileView.GetComponent<Image>()) return tileView;

            // Assign sprites if available
            // TileSprites will be used by TileView itself
            // We just need to ensure it's set on the TileView component
            var tileViewComponent = tileView.GetComponent<TileView>();
            if (tileViewComponent)
            {
                // The TileView will use the sprites from its own serialized field
                // or we can pass it through initialization
            }

            return tileView;
        }

        /// <summary>
        ///     Removes all tile views from the grid.
        /// </summary>
        public void ClearAllTiles()
        {
            foreach (var kvp in _tileViews.Where(kvp => kvp.Value != null))
                Destroy(kvp.Value.gameObject);

            _tileViews.Clear();

            // Update all slots as unoccupied
            foreach (var slotView in slotViews)
                if (slotView != null)
                    slotView.SetOccupied(false);
        }

        /// <summary>
        ///     Gets the world position of a slot.
        /// </summary>
        public Vector2 GetSlotPosition(int slot)
        {
            if (slot is >= 0 and <= 8 && slotViews[slot]) return slotViews[slot].Position;

            Debug.LogWarning($"GridView: Invalid slot {slot}, returning zero position");
            return Vector2.zero;
        }

        /// <summary>
        ///     Gets the tile view at the specified slot, if any.
        /// </summary>
        public TileView GetTileAt(int slot)
        {
            _tileViews.TryGetValue(slot, out var tileView);
            return tileView;
        }

        /// <summary>
        ///     Gets the slot index at a world position.
        ///     Returns -1 if no slot is at that position.
        /// </summary>
        public int GetSlotAtPosition(Vector2 worldPosition)
        {
            for (var i = 0; i < slotViews.Length; i++)
                if (slotViews[i] && slotViews[i].ContainsPoint(worldPosition))
                    return i;

            return -1;
        }

        /// <summary>
        ///     Highlights a specific slot.
        /// </summary>
        public void HighlightSlot(int slot, bool highlight)
        {
            if (slot >= 0 && slot < slotViews.Length && slotViews[slot])
                slotViews[slot].SetHighlight(highlight);
        }

        /// <summary>
        ///     Clears all slot highlights.
        /// </summary>
        public void ClearAllHighlights()
        {
            foreach (var slotView in slotViews)
                if (slotView)
                    slotView.SetHighlight(false);
        }

        /// <summary>
        ///     Gets all currently displayed tile views.
        /// </summary>
        public IEnumerable<TileView> GetAllTileViews()
        {
            return _tileViews.Values;
        }

        /// <summary>
        ///     Checks if a slot is currently occupied.
        /// </summary>
        public bool IsSlotOccupied(int slot)
        {
            return _tileViews.ContainsKey(slot);
        }

        /// <summary>
        ///     Gets the GridSlotView at the specified index.
        /// </summary>
        public GridSlotView GetSlotView(int slot)
        {
            if (slot >= 0 && slot < slotViews.Length) return slotViews[slot];
            return null;
        }

        /// <summary>
        ///     Gets the TileView at the specified slot, if any.
        /// </summary>
        public TileView GetTileViewAtSlot(int slot)
        {
            return _tileViews.TryGetValue(slot, out var tileView) ? tileView : null;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Validate in editor
            if (slotViews != null && slotViews.Length != 9)
                Debug.LogWarning("GridView: Slot views array should have exactly 9 elements!");

            // Auto-assign indices if slots are assigned
            if (slotViews != null)
                for (var i = 0; i < slotViews.Length && i < 9; i++)
                    if (slotViews[i] != null && slotViews[i].SlotIndex != i)
                        slotViews[i].SlotIndex = i;
        }

        [ContextMenu("Auto-Find Slot Views")]
        private void AutoFindSlotViews()
        {
            var slots = GetComponentsInChildren<GridSlotView>();

            if (slots.Length == 0)
            {
                Debug.LogWarning("No GridSlotView components found in children!");
                return;
            }

            slotViews = new GridSlotView[9];

            foreach (var slot in slots)
            {
                var index = slot.SlotIndex;
                if (index is >= 0 and < 9) slotViews[index] = slot;
            }

            Debug.Log($"Auto-assigned {slots.Length} slot views");
        }
#endif
    }
}