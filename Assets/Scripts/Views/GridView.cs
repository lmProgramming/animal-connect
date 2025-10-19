using System.Collections.Generic;
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
            if (slotViews == null || slotViews.Length != 9)
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
        public void UpdateFromGrid(GridState gridState)
        {
            if (gridState == null)
            {
                Debug.LogWarning("GridView: Cannot update from null grid state");
                return;
            }

            for (var slot = 0; slot < 9; slot++)
            {
                var tileData = gridState.GetTile(slot);

                if (tileData.HasValue)
                    UpdateTileView(slot, tileData.Value);
                else
                    RemoveTileView(slot);
            }
        }

        /// <summary>
        ///     Updates a single tile view at the specified slot.
        ///     Creates a new tile view if one doesn't exist.
        /// </summary>
        private void UpdateTileView(int slot, TileData tileData)
        {
            if (slot < 0 || slot > 8)
            {
                Debug.LogError($"GridView: Invalid slot index {slot}");
                return;
            }

            // Get or create tile view
            var isNewTile = !_tileViews.TryGetValue(slot, out var tileView);
            if (isNewTile)
            {
                tileView = CreateTileView(slot);
                _tileViews[slot] = tileView;
            }

            // Update tile view only if values have changed
            var position = GetSlotPosition(slot);

            // Only update type if it changed
            if (tileView.CurrentType != tileData.Type) tileView.SetType(tileData.Type, !animateTileChanges);

            // Only update rotation if it changed
            if (tileView.CurrentRotation != tileData.Rotation)
                tileView.SetRotation(tileData.Rotation, animateTileChanges);

            // Only update position if it changed or this is a new tile
            if (isNewTile || Vector2.Distance(tileView.transform.position, position) > 0.01f)
                tileView.SetPosition(position, !isNewTile && animateTileChanges);

            tileView.SlotIndex = slot;

            // Update slot occupied state
            if (slotViews[slot] != null) slotViews[slot].SetOccupied(true);
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
                if (slotViews[slot] != null) slotViews[slot].SetOccupied(false);
            }
        }

        /// <summary>
        ///     Creates a new tile view at the specified slot.
        /// </summary>
        private TileView CreateTileView(int slot)
        {
            if (tilePrefab == null)
            {
                Debug.LogError("GridView: Cannot create tile - prefab not assigned!");
                return null;
            }

            var position = GetSlotPosition(slot);
            var tileView = Instantiate(tilePrefab, position, Quaternion.identity, tileContainer);
            tileView.name = $"Tile_{slot}";
            tileView.SlotIndex = slot;

            // Assign sprites if available
            if (tileSprites != null && tileView.GetComponent<Image>() != null)
            {
                // TileSprites will be used by TileView itself
                // We just need to ensure it's set on the TileView component
                var tileViewComponent = tileView.GetComponent<TileView>();
                if (tileViewComponent != null)
                {
                    // The TileView will use the sprites from its own serialized field
                    // or we can pass it through initialization
                }
            }

            return tileView;
        }

        /// <summary>
        ///     Removes all tile views from the grid.
        /// </summary>
        public void ClearAllTiles()
        {
            foreach (var kvp in _tileViews)
                if (kvp.Value != null)
                    Destroy(kvp.Value.gameObject);

            _tileViews.Clear();

            // Update all slots as unoccupied
            for (var i = 0; i < slotViews.Length; i++)
                if (slotViews[i] != null)
                    slotViews[i].SetOccupied(false);
        }

        /// <summary>
        ///     Gets the world position of a slot.
        /// </summary>
        public Vector2 GetSlotPosition(int slot)
        {
            if (slot < 0 || slot > 8 || slotViews[slot] == null)
            {
                Debug.LogWarning($"GridView: Invalid slot {slot}, returning zero position");
                return Vector2.zero;
            }

            return slotViews[slot].Position;
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
                if (slotViews[i] != null && slotViews[i].ContainsPoint(worldPosition))
                    return i;

            return -1;
        }

        /// <summary>
        ///     Highlights a specific slot.
        /// </summary>
        public void HighlightSlot(int slot, bool highlight)
        {
            if (slot >= 0 && slot < slotViews.Length && slotViews[slot] != null)
                slotViews[slot].SetHighlight(highlight);
        }

        /// <summary>
        ///     Clears all slot highlights.
        /// </summary>
        public void ClearAllHighlights()
        {
            for (var i = 0; i < slotViews.Length; i++)
                if (slotViews[i] != null)
                    slotViews[i].SetHighlight(false);
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
            if (_tileViews.TryGetValue(slot, out var tileView)) return tileView;
            return null;
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
                if (index >= 0 && index < 9) slotViews[index] = slot;
            }

            Debug.Log($"Auto-assigned {slots.Length} slot views");
        }
#endif
    }
}