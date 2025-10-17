using System.Collections.Generic;
using UnityEngine;
using Core.Models;

namespace AnimalConnect.Views
{
    /// <summary>
    /// Manages the visual representation of the entire grid.
    /// Syncs with GameState but contains no game logic.
    /// Responsible for creating, updating, and removing tile views.
    /// </summary>
    public class GridView : MonoBehaviour
    {
        [Header("Grid Configuration")]
        [SerializeField] private GridSlotView[] _slotViews = new GridSlotView[9];
        
        [Header("Tile Configuration")]
        [SerializeField] private TileView _tilePrefab;
        [SerializeField] private TileSprites _tileSprites;
        [SerializeField] private Transform _tileContainer;

        [Header("Animation Settings")]
        [SerializeField] private bool _animateTileChanges = true;
        [SerializeField] private bool _animateInitialPlacement = false;

        private Dictionary<int, TileView> _tileViews = new Dictionary<int, TileView>();

        public IReadOnlyDictionary<int, TileView> TileViews => _tileViews;

        private void Awake()
        {
            Initialize();
        }

        /// <summary>
        /// Initializes the grid view.
        /// </summary>
        public void Initialize()
        {
            // Validate slot views
            if (_slotViews == null || _slotViews.Length != 9)
            {
                Debug.LogError("GridView: Must have exactly 9 slot views!");
                return;
            }

            // Set slot indices if not already set
            for (int i = 0; i < _slotViews.Length; i++)
            {
                if (_slotViews[i] != null && _slotViews[i].SlotIndex != i)
                {
                    _slotViews[i].SlotIndex = i;
                }
            }

            // Use this GameObject as container if not set
            if (_tileContainer == null)
            {
                _tileContainer = transform;
            }

            // Validate prefab
            if (_tilePrefab == null)
            {
                Debug.LogError("GridView: Tile prefab not assigned!");
            }

            // Clear any existing tiles
            ClearAllTiles();

            Debug.Log("GridView initialized");
        }

        /// <summary>
        /// Updates the entire grid view from a game state.
        /// This is the main synchronization method.
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
        /// Updates the grid view from a grid state.
        /// </summary>
        public void UpdateFromGrid(GridState gridState)
        {
            if (gridState == null)
            {
                Debug.LogWarning("GridView: Cannot update from null grid state");
                return;
            }

            for (int slot = 0; slot < 9; slot++)
            {
                var tileData = gridState.GetTile(slot);

                if (tileData.HasValue)
                {
                    UpdateTileView(slot, tileData.Value);
                }
                else
                {
                    RemoveTileView(slot);
                }
            }
        }

        /// <summary>
        /// Updates a single tile view at the specified slot.
        /// Creates a new tile view if one doesn't exist.
        /// </summary>
        private void UpdateTileView(int slot, TileData tileData)
        {
            if (slot < 0 || slot > 8)
            {
                Debug.LogError($"GridView: Invalid slot index {slot}");
                return;
            }

            // Get or create tile view
            if (!_tileViews.TryGetValue(slot, out var tileView))
            {
                tileView = CreateTileView(slot);
                _tileViews[slot] = tileView;
            }

            // Update tile view
            var position = GetSlotPosition(slot);
            tileView.SetType(tileData.Type, immediate: !_animateTileChanges);
            tileView.SetRotation(tileData.Rotation, animate: _animateTileChanges);
            tileView.SetPosition(position, animate: false); // Position set immediately for newly created tiles
            tileView.SlotIndex = slot;

            // Update slot occupied state
            if (_slotViews[slot] != null)
            {
                _slotViews[slot].SetOccupied(true);
            }
        }

        /// <summary>
        /// Removes a tile view from the specified slot.
        /// </summary>
        private void RemoveTileView(int slot)
        {
            if (_tileViews.TryGetValue(slot, out var tileView))
            {
                Destroy(tileView.gameObject);
                _tileViews.Remove(slot);

                // Update slot occupied state
                if (_slotViews[slot] != null)
                {
                    _slotViews[slot].SetOccupied(false);
                }
            }
        }

        /// <summary>
        /// Creates a new tile view at the specified slot.
        /// </summary>
        private TileView CreateTileView(int slot)
        {
            if (_tilePrefab == null)
            {
                Debug.LogError("GridView: Cannot create tile - prefab not assigned!");
                return null;
            }

            var position = GetSlotPosition(slot);
            var tileView = Instantiate(_tilePrefab, position, Quaternion.identity, _tileContainer);
            tileView.name = $"Tile_{slot}";
            tileView.SlotIndex = slot;

            // Assign sprites if available
            if (_tileSprites != null && tileView.GetComponent<UnityEngine.UI.Image>() != null)
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
        /// Removes all tile views from the grid.
        /// </summary>
        public void ClearAllTiles()
        {
            foreach (var kvp in _tileViews)
            {
                if (kvp.Value != null)
                {
                    Destroy(kvp.Value.gameObject);
                }
            }

            _tileViews.Clear();

            // Update all slots as unoccupied
            for (int i = 0; i < _slotViews.Length; i++)
            {
                if (_slotViews[i] != null)
                {
                    _slotViews[i].SetOccupied(false);
                }
            }
        }

        /// <summary>
        /// Gets the world position of a slot.
        /// </summary>
        public Vector2 GetSlotPosition(int slot)
        {
            if (slot < 0 || slot > 8 || _slotViews[slot] == null)
            {
                Debug.LogWarning($"GridView: Invalid slot {slot}, returning zero position");
                return Vector2.zero;
            }

            return _slotViews[slot].Position;
        }

        /// <summary>
        /// Gets the tile view at the specified slot, if any.
        /// </summary>
        public TileView GetTileAt(int slot)
        {
            _tileViews.TryGetValue(slot, out var tileView);
            return tileView;
        }

        /// <summary>
        /// Gets the slot index at a world position.
        /// Returns -1 if no slot is at that position.
        /// </summary>
        public int GetSlotAtPosition(Vector2 worldPosition)
        {
            for (int i = 0; i < _slotViews.Length; i++)
            {
                if (_slotViews[i] != null && _slotViews[i].ContainsPoint(worldPosition))
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>
        /// Highlights a specific slot.
        /// </summary>
        public void HighlightSlot(int slot, bool highlight)
        {
            if (slot >= 0 && slot < _slotViews.Length && _slotViews[slot] != null)
            {
                _slotViews[slot].SetHighlighted(highlight);
            }
        }

        /// <summary>
        /// Clears all slot highlights.
        /// </summary>
        public void ClearAllHighlights()
        {
            for (int i = 0; i < _slotViews.Length; i++)
            {
                if (_slotViews[i] != null)
                {
                    _slotViews[i].SetHighlighted(false);
                }
            }
        }

        /// <summary>
        /// Gets all currently displayed tile views.
        /// </summary>
        public IEnumerable<TileView> GetAllTileViews()
        {
            return _tileViews.Values;
        }

        /// <summary>
        /// Checks if a slot is currently occupied.
        /// </summary>
        public bool IsSlotOccupied(int slot)
        {
            return _tileViews.ContainsKey(slot);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Validate in editor
            if (_slotViews != null && _slotViews.Length != 9)
            {
                Debug.LogWarning("GridView: Slot views array should have exactly 9 elements!");
            }

            // Auto-assign indices if slots are assigned
            if (_slotViews != null)
            {
                for (int i = 0; i < _slotViews.Length && i < 9; i++)
                {
                    if (_slotViews[i] != null && _slotViews[i].SlotIndex != i)
                    {
                        _slotViews[i].SlotIndex = i;
                    }
                }
            }
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

            _slotViews = new GridSlotView[9];
            
            foreach (var slot in slots)
            {
                int index = slot.SlotIndex;
                if (index >= 0 && index < 9)
                {
                    _slotViews[index] = slot;
                }
            }

            Debug.Log($"Auto-assigned {slots.Length} slot views");
        }
#endif
    }
}
