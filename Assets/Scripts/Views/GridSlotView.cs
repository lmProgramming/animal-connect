using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

namespace Views
{
    /// <summary>
    ///     Represents a single slot position in the grid.
    ///     This is a pure visual component with no game logic.
    /// </summary>
    public class GridSlotView : MonoBehaviour
    {
        [FormerlySerializedAs("_slotIndex")]
        [Header("Slot Configuration")]
        [SerializeField] private int slotIndex = -1;

        [FormerlySerializedAs("_backgroundRenderer")]
        [Header("Visual Feedback (Optional)")]
        [SerializeField] private SpriteRenderer backgroundRenderer;

        [FormerlySerializedAs("_normalColor")] [SerializeField]
        private Color normalColor = new(1f, 1f, 1f, 0.1f);

        [FormerlySerializedAs("_highlightColor")] [SerializeField]
        private Color highlightColor = new(1f, 1f, 0f, 0.3f);

        [FormerlySerializedAs("_occupiedColor")] [SerializeField]
        private Color occupiedColor = new(0.5f, 0.5f, 0.5f, 0.1f);

        private bool _isHighlighted;
        private bool _isOccupied;

        public int SlotIndex
        {
            get => slotIndex;
            set => slotIndex = value;
        }

        public Vector2 Position => transform.position;
        public Vector3 WorldPosition => transform.position;

        private void Awake()
        {
            if (slotIndex < 0 || slotIndex > 8)
                Debug.LogWarning($"GridSlotView on {gameObject.name} has invalid slot index: {slotIndex}");

            UpdateVisuals();
        }

        /// <summary>
        ///     Sets whether this slot is occupied by a tile.
        /// </summary>
        public void SetOccupied(bool occupied)
        {
            _isOccupied = occupied;
            UpdateVisuals();
        }

        /// <summary>
        ///     Sets whether this slot is highlighted (e.g., during drag operations).
        /// </summary>
        public void SetHighlight(bool highlighted)
        {
            _isHighlighted = highlighted;
            UpdateVisuals();
        }

        /// <summary>
        ///     Updates the visual appearance based on current state.
        /// </summary>
        private void UpdateVisuals()
        {
            if (backgroundRenderer == null) return;

            if (_isHighlighted)
                backgroundRenderer.color = highlightColor;
            else if (_isOccupied)
                backgroundRenderer.color = occupiedColor;
            else
                backgroundRenderer.color = normalColor;
        }

        /// <summary>
        ///     Checks if a world position is within this slot's bounds.
        /// </summary>
        public bool ContainsPoint(Vector2 worldPoint)
        {
            if (backgroundRenderer != null) return backgroundRenderer.bounds.Contains(worldPoint);

            // Fallback: simple distance check
            var distance = Vector2.Distance(Position, worldPoint);
            return distance < 0.5f; // Adjust threshold as needed
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Auto-name in editor for clarity
            if (slotIndex >= 0 && slotIndex <= 8) gameObject.name = $"GridSlot_{slotIndex}";
        }

        private void OnDrawGizmos()
        {
            // Draw slot bounds in editor
            Gizmos.color = _isHighlighted ? Color.yellow : Color.gray;
            Gizmos.DrawWireCube(transform.position, Vector3.one * 0.9f);

            // Draw slot index
            Handles.Label(
                transform.position + Vector3.up * 0.6f,
                slotIndex.ToString(),
                new GUIStyle { normal = new GUIStyleState { textColor = Color.white } }
            );
        }
#endif
    }
}