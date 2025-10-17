using Core.Models;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace AnimalConnect.Views
{
    /// <summary>
    ///     Visual representation of a tile.
    ///     Handles rendering, rotation, and position updates.
    ///     No game logic - purely presentation layer.
    /// </summary>
    [RequireComponent(typeof(Image))]
    public class TileView : MonoBehaviour
    {
        [Header("Visual Components")]
        [SerializeField] private Image _image;

        [SerializeField] private TileSprites _sprites;

        [Header("Animation Settings")]
        [SerializeField] private float _rotationDuration = 0.3f;

        [SerializeField] private float _moveDuration = 0.3f;
        [SerializeField] private Ease _rotationEase = Ease.OutCubic;
        [SerializeField] private Ease _moveEase = Ease.OutCubic;

        [Header("Scale Effects")]
        [SerializeField] private bool _scaleOnInteraction = true;

        [SerializeField] private float _hoverScale = 1.1f;
        [SerializeField] private float _pressScale = 0.95f;
        [SerializeField] private float _scaleDuration = 0.1f;
        private Tween _currentTween;

        private Vector3 _normalScale = Vector3.one;

        public TileType CurrentType { get; private set; }

        public int CurrentRotation { get; private set; }

        public int SlotIndex { get; set; } = -1;

        private void Awake()
        {
            if (_image == null) _image = GetComponent<Image>();

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
            if (_image == null) _image = GetComponent<Image>();
        }
#endif

        /// <summary>
        ///     Sets the tile type and updates the sprite.
        /// </summary>
        public void SetType(TileType type, bool immediate = false)
        {
            CurrentType = type;

            if (_sprites != null)
                _image.sprite = _sprites.GetSprite(type);
            else
                Debug.LogWarning($"TileView: No TileSprites assigned! Cannot display type {type}");

            if (immediate) _image.SetNativeSize();
        }

        /// <summary>
        ///     Sets the tile rotation (0-3 for 90-degree increments).
        /// </summary>
        public void SetRotation(int rotation, bool animate = false)
        {
            rotation %= 4; // Ensure 0-3 range
            CurrentRotation = rotation;

            var targetAngle = -rotation * 90f; // Negative for clockwise

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
            _currentTween?.Kill();

            _currentTween = transform
                .DORotate(new Vector3(0, 0, targetAngle), _rotationDuration)
                .SetEase(_rotationEase);
        }

        private void AnimateMove(Vector2 targetPosition)
        {
            _currentTween?.Kill();

            _currentTween = transform
                .DOMove(targetPosition, _moveDuration)
                .SetEase(_moveEase);
        }

        /// <summary>
        ///     Plays a hover effect when the tile is hovered over.
        /// </summary>
        public void PlayHoverEffect()
        {
            if (!_scaleOnInteraction) return;

            _currentTween?.Kill();
            _currentTween = transform
                .DOScale(_normalScale * _hoverScale, _scaleDuration)
                .SetEase(Ease.OutCubic);
        }

        /// <summary>
        ///     Plays a press effect when the tile is clicked.
        /// </summary>
        public void PlayPressEffect()
        {
            if (!_scaleOnInteraction) return;

            _currentTween?.Kill();
            _currentTween = transform
                .DOScale(_normalScale * _pressScale, _scaleDuration)
                .SetEase(Ease.OutCubic);
        }

        /// <summary>
        ///     Resets the tile to normal appearance.
        /// </summary>
        public void ResetEffect()
        {
            if (!_scaleOnInteraction) return;

            _currentTween?.Kill();
            _currentTween = transform
                .DOScale(_normalScale, _scaleDuration)
                .SetEase(Ease.OutCubic);
        }

        /// <summary>
        ///     Plays a "pop" effect for visual feedback.
        /// </summary>
        public void PlayPopEffect()
        {
            _currentTween?.Kill();

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
            if (_image != null) _image.color = highlighted ? new Color(1f, 1f, 0.8f) : Color.white;
        }

        /// <summary>
        ///     Sets the tile's alpha transparency.
        /// </summary>
        public void SetAlpha(float alpha)
        {
            if (_image != null)
            {
                var color = _image.color;
                color.a = Mathf.Clamp01(alpha);
                _image.color = color;
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