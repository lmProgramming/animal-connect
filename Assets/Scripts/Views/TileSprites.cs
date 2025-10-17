using UnityEngine;
using Core.Models;

namespace AnimalConnect.Views
{
    /// <summary>
    /// ScriptableObject that maps tile types to their visual representations.
    /// This allows designers to configure tile sprites without code changes.
    /// </summary>
    [CreateAssetMenu(fileName = "TileSprites", menuName = "Animal Connect/Tile Sprites", order = 1)]
    public class TileSprites : ScriptableObject
    {
        [Header("Tile Sprites")]
        [SerializeField] private Sprite _curveSprite;
        [SerializeField] private Sprite _twoCurvesSprite;
        [SerializeField] private Sprite _intersectionSprite;
        [SerializeField] private Sprite _xIntersectionSprite;
        [SerializeField] private Sprite _bridgeSprite;

        [Header("Optional: Default Sprite")]
        [SerializeField] private Sprite _defaultSprite;

        /// <summary>
        /// Gets the sprite for a given tile type.
        /// </summary>
        /// <param name="type">The tile type</param>
        /// <returns>The sprite for that tile type, or default sprite if not found</returns>
        public Sprite GetSprite(TileType type)
        {
            switch (type)
            {
                case TileType.Curve:
                    return _curveSprite != null ? _curveSprite : _defaultSprite;
                
                case TileType.TwoCurves:
                    return _twoCurvesSprite != null ? _twoCurvesSprite : _defaultSprite;
                
                case TileType.Intersection:
                    return _intersectionSprite != null ? _intersectionSprite : _defaultSprite;
                
                case TileType.XIntersection:
                    return _xIntersectionSprite != null ? _xIntersectionSprite : _defaultSprite;
                
                case TileType.Bridge:
                    return _bridgeSprite != null ? _bridgeSprite : _defaultSprite;
                
                default:
                    Debug.LogWarning($"Unknown tile type: {type}. Using default sprite.");
                    return _defaultSprite;
            }
        }

        /// <summary>
        /// Validates that all required sprites are assigned.
        /// </summary>
        /// <returns>True if all sprites are assigned, false otherwise</returns>
        public bool ValidateSprites()
        {
            bool isValid = true;

            if (_curveSprite == null)
            {
                Debug.LogError("TileSprites: Curve sprite not assigned!");
                isValid = false;
            }

            if (_twoCurvesSprite == null)
            {
                Debug.LogError("TileSprites: TwoCurves sprite not assigned!");
                isValid = false;
            }

            if (_intersectionSprite == null)
            {
                Debug.LogError("TileSprites: Intersection sprite not assigned!");
                isValid = false;
            }

            if (_xIntersectionSprite == null)
            {
                Debug.LogError("TileSprites: XIntersection sprite not assigned!");
                isValid = false;
            }

            if (_bridgeSprite == null)
            {
                Debug.LogError("TileSprites: Bridge sprite not assigned!");
                isValid = false;
            }

            return isValid;
        }

        private void OnValidate()
        {
            // Validate in editor when values change
            ValidateSprites();
        }
    }
}
