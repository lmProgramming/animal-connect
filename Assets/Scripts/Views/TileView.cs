using Core.Models;
using DG.Tweening;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.UI;

namespace Views
{
    /// <summary>
    ///     Visual representation of a tile.
    ///     Handles rendering, rotation, and position updates.
    ///     No game logic - purely presentation layer.
    /// </summary>
    [RequireComponent(typeof(Image))]
    public class TileView : MonoBehaviour
    {
        private const float MoveDuration = 0.3f;

        private const Ease RotationEase = Ease.OutCubic;

        [Header("Visual Components")]
        [SerializeField] private Image image;

        [SerializeField]
        private TileSprites sprites;

        [Header("Animation Settings")]
        [SerializeField] private float rotationDuration = 0.3f;

        [SerializeField]
        private Ease moveEase = Ease.OutCubic;

        [Header("Scale Effects")]
        [SerializeField] private bool scaleOnInteraction = true;

        [SerializeField]
        private float hoverScale = 1.1f;

        [SerializeField]
        private float pressScale = 0.95f;

        [SerializeField]
        private float scaleDuration = 0.1f;

        [Header("Logic Debug")]
        [field: SerializeField]
        public int SlotIndex { get; set; } = -1;

        [field: SerializeField]
        public int CurrentRotation { get; private set; }

        [field: SerializeField]
        public TileType CurrentType { get; private set; }

        [CanBeNull] private Tween _currentTween;

        private Vector3 _normalScale = Vector3.one;

        private void Awake()
        {
            if (image == null) image = GetComponent<Image>();

            _normalScale = transform.localScale;
        }

        private void OnDestroy()
        {
            // Clean up any active tweens
            _currentTween?.Kill();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (image == null) image = GetComponent<Image>();
        }
#endif

        /// <summary>
        ///     Sets the tile type and updates the sprite.
        /// </summary>
        public void SetType(TileType type, bool immediate = false)
        {
            CurrentType = type;

            if (sprites != null)
                image.sprite = sprites.GetSprite(type);
            else
                Debug.LogWarning($"TileView: No TileSprites assigned! Cannot display type {type}");

            if (immediate) image.SetNativeSize();
        }

        /// <summary>
        ///     Sets the tile rotation (0-3 for 90-degree increments).
        /// </summary>
        public void SetRotation(int newRotation, bool animate = false)
        {
            newRotation %= 4; // Ensure 0-3 range
            CurrentRotation = newRotation;

            var targetAngle = -newRotation * 90f; // Negative for clockwise

            if (animate && Application.isPlaying)
                AnimateRotation(targetAngle);
            else
                transform.rotation = Quaternion.Euler(0, 0, targetAngle);
        }

        /// <summary>
        ///     Rotates the tile by 90 degrees clockwise.
        /// </summary>
        public void RotateClockwise(bool animate = true)
        {
            SetRotation(CurrentRotation + 1, animate);
        }

        /// <summary>
        ///     Sets the tile position.
        /// </summary>
        public void SetPosition(Vector2 position, bool animate = true)
        {
            if (animate && Application.isPlaying)
                AnimateMove(position);
            else
                transform.position = position;
        }

        /// <summary>
        ///     Sets the tile position using a Vector3.
        /// </summary>
        public void SetPosition(Vector3 position, bool animate = true)
        {
            SetPosition((Vector2)position, animate);
        }

        /// <summary>
        ///     Initializes the tile with type, rotation, and position.
        /// </summary>
        public void Initialize(TileType type, int rotation, Vector2 position)
        {
            SetType(type, true);
            SetRotation(rotation);
            SetPosition(position, false);
        }

        #region Animation Methods

        private void AnimateRotation(float targetAngle)
        {
            _currentTween?.Complete();

            _currentTween = transform
                .DORotate(new Vector3(0, 0, targetAngle), rotationDuration)
                .SetEase(RotationEase);
        }

        private void AnimateMove(Vector2 targetPosition)
        {
            _currentTween?.Complete();

            _currentTween = transform
                .DOMove(targetPosition, MoveDuration)
                .SetEase(moveEase);
        }

        /// <summary>
        ///     Plays a hover effect when the tile is hovered over.
        /// </summary>
        public void PlayHoverEffect()
        {
            if (_currentTween?.active ?? false) return;

            _currentTween?.Complete();
            _currentTween = transform
                .DOScale(_normalScale * hoverScale, scaleDuration)
                .SetEase(Ease.OutCubic);
        }

        /// <summary>
        ///     Plays a press effect when the tile is clicked.
        /// </summary>
        public void PlayPressEffect()
        {
            if (!scaleOnInteraction) return;

            _currentTween?.Complete();
            _currentTween = transform
                .DOScale(_normalScale * pressScale, scaleDuration)
                .SetEase(Ease.OutCubic);
        }

        /// <summary>
        ///     Resets the tile to normal appearance.
        /// </summary>
        public void ResetEffect()
        {
            if (!scaleOnInteraction) return;

            _currentTween?.Complete();
            _currentTween = transform
                .DOScale(_normalScale, scaleDuration)
                .SetEase(Ease.OutCubic);
        }

        /// <summary>
        ///     Plays a "pop" effect for visual feedback.
        /// </summary>
        public void PlayPopEffect()
        {
            _currentTween?.Complete();

            var sequence = DOTween.Sequence();
            sequence.Append(transform.DOScale(_normalScale * 1.2f, 0.1f));
            sequence.Append(transform.DOScale(_normalScale, 0.1f));

            _currentTween = sequence;
        }

        #endregion

        #region Visual Feedback

        /// <summary>
        ///     Sets whether the tile appears selected/highlighted.
        /// </summary>
        public void SetHighlighted(bool highlighted)
        {
            if (image != null) image.color = highlighted ? new Color(1f, 1f, 0.8f) : Color.white;
        }

        /// <summary>
        ///     Sets the tile's alpha transparency.
        /// </summary>
        public void SetAlpha(float alpha)
        {
            if (image != null)
            {
                var color = image.color;
                color.a = Mathf.Clamp01(alpha);
                image.color = color;
            }
        }

        #endregion

        #region Drag Interaction Support

        /// <summary>
        ///     Called when drag starts on this tile.
        /// </summary>
        public void OnDragStart()
        {
            PlayPressEffect();
            SetAlpha(0.8f);
        }

        /// <summary>
        ///     Called when drag ends on this tile.
        /// </summary>
        public void OnDragEnd()
        {
            ResetEffect();
            SetAlpha(1f);
        }

        /// <summary>
        ///     Resets tile position to its assigned slot.
        /// </summary>
        public void ResetPosition()
        {
            // This will be called by GridView to snap back to slot position
            // The actual position is managed by GridView's UpdateFromState
        }

        #endregion
    }
}