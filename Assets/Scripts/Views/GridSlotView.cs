using UnityEngine;

namespace AnimalConnect.Views
{
    /// <summary>
    /// Represents a single slot position in the grid.
    /// This is a pure visual component with no game logic.
    /// </summary>
    public class GridSlotView : MonoBehaviour
    {
        [Header("Slot Configuration")]
        [SerializeField] private int _slotIndex = -1;
        
        [Header("Visual Feedback (Optional)")]
        [SerializeField] private SpriteRenderer _backgroundRenderer;
        [SerializeField] private Color _normalColor = new Color(1f, 1f, 1f, 0.1f);
        [SerializeField] private Color _highlightColor = new Color(1f, 1f, 0f, 0.3f);
        [SerializeField] private Color _occupiedColor = new Color(0.5f, 0.5f, 0.5f, 0.1f);

        private bool _isHighlighted;
        private bool _isOccupied;

        public int SlotIndex
        {
            get => _slotIndex;
            set => _slotIndex = value;
        }

        public Vector2 Position => transform.position;
        public Vector3 WorldPosition => transform.position;

        private void Awake()
        {
            if (_slotIndex < 0 || _slotIndex > 8)
            {
                Debug.LogWarning($"GridSlotView on {gameObject.name} has invalid slot index: {_slotIndex}");
            }

            UpdateVisuals();
        }

        /// <summary>
        /// Sets whether this slot is occupied by a tile.
        /// </summary>
        public void SetOccupied(bool occupied)
        {
            _isOccupied = occupied;
            UpdateVisuals();
        }

        /// <summary>
        /// Sets whether this slot is highlighted (e.g., during drag operations).
        /// </summary>
        public void SetHighlight(bool highlighted)
        {
            _isHighlighted = highlighted;
            UpdateVisuals();
        }

        /// <summary>
        /// Updates the visual appearance based on current state.
        /// </summary>
        private void UpdateVisuals()
        {
            if (_backgroundRenderer == null) return;

            if (_isHighlighted)
            {
                _backgroundRenderer.color = _highlightColor;
            }
            else if (_isOccupied)
            {
                _backgroundRenderer.color = _occupiedColor;
            }
            else
            {
                _backgroundRenderer.color = _normalColor;
            }
        }

        /// <summary>
        /// Checks if a world position is within this slot's bounds.
        /// </summary>
        public bool ContainsPoint(Vector2 worldPoint)
        {
            if (_backgroundRenderer != null)
            {
                return _backgroundRenderer.bounds.Contains(worldPoint);
            }

            // Fallback: simple distance check
            float distance = Vector2.Distance(Position, worldPoint);
            return distance < 0.5f; // Adjust threshold as needed
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Auto-name in editor for clarity
            if (_slotIndex >= 0 && _slotIndex <= 8)
            {
                gameObject.name = $"GridSlot_{_slotIndex}";
            }
        }

        private void OnDrawGizmos()
        {
            // Draw slot bounds in editor
            Gizmos.color = _isHighlighted ? Color.yellow : Color.gray;
            Gizmos.DrawWireCube(transform.position, Vector3.one * 0.9f);
            
            // Draw slot index
            UnityEditor.Handles.Label(
                transform.position + Vector3.up * 0.6f,
                _slotIndex.ToString(),
                new GUIStyle() { normal = new GUIStyleState() { textColor = Color.white } }
            );
        }
#endif
    }
}
