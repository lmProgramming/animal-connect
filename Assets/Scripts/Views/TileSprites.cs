using Core.Models;
using UnityEngine;
using UnityEngine.Serialization;

namespace Views
{
    /// <summary>
    ///     ScriptableObject that maps tile types to their visual representations.
    ///     This allows designers to configure tile sprites without code changes.
    /// </summary>
    [CreateAssetMenu(fileName = "TileSprites", menuName = "Animal Connect/Tile Sprites", order = 1)]
    public class TileSprites : ScriptableObject
    {
        [FormerlySerializedAs("_curveSprite")]
        [Header("Tile Sprites")]
        [SerializeField] private Sprite curveSprite;

        [FormerlySerializedAs("_twoCurvesSprite")] [SerializeField]
        private Sprite twoCurvesSprite;

        [FormerlySerializedAs("_intersectionSprite")] [SerializeField]
        private Sprite intersectionSprite;

        [FormerlySerializedAs("_xIntersectionSprite")] [SerializeField]
        private Sprite xIntersectionSprite;

        [FormerlySerializedAs("_bridgeSprite")] [SerializeField]
        private Sprite bridgeSprite;

        [FormerlySerializedAs("_defaultSprite")]
        [Header("Optional: Default Sprite")]
        [SerializeField] private Sprite defaultSprite;

        private void OnValidate()
        {
            // Validate in editor when values change
            ValidateSprites();
        }

        /// <summary>
        ///     Gets the sprite for a given tile type.
        /// </summary>
        /// <param name="type">The tile type</param>
        /// <returns>The sprite for that tile type, or default sprite if not found</returns>
        public Sprite GetSprite(TileType type)
        {
            switch (type)
            {
                case TileType.Curve:
                    return curveSprite != null ? curveSprite : defaultSprite;

                case TileType.TwoCurves:
                    return twoCurvesSprite != null ? twoCurvesSprite : defaultSprite;

                case TileType.Intersection:
                    return intersectionSprite != null ? intersectionSprite : defaultSprite;

                case TileType.XIntersection:
                    return xIntersectionSprite != null ? xIntersectionSprite : defaultSprite;

                case TileType.Bridge:
                    return bridgeSprite != null ? bridgeSprite : defaultSprite;

                default:
                    Debug.LogWarning($"Unknown tile type: {type}. Using default sprite.");
                    return defaultSprite;
            }
        }

        /// <summary>
        ///     Validates that all required sprites are assigned.
        /// </summary>
        /// <returns>True if all sprites are assigned, false otherwise</returns>
        public bool ValidateSprites()
        {
            var isValid = true;

            if (curveSprite == null)
            {
                Debug.LogError("TileSprites: Curve sprite not assigned!");
                isValid = false;
            }

            if (twoCurvesSprite == null)
            {
                Debug.LogError("TileSprites: TwoCurves sprite not assigned!");
                isValid = false;
            }

            if (intersectionSprite == null)
            {
                Debug.LogError("TileSprites: Intersection sprite not assigned!");
                isValid = false;
            }

            if (xIntersectionSprite == null)
            {
                Debug.LogError("TileSprites: XIntersection sprite not assigned!");
                isValid = false;
            }

            if (bridgeSprite == null)
            {
                Debug.LogError("TileSprites: Bridge sprite not assigned!");
                isValid = false;
            }

            return isValid;
        }
    }
}