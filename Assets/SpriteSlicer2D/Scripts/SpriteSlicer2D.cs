// ReSharper disable All
//#define TK2D_SLICING_ENABLED

//#define IGNORE_RAYCAST_TRIGGERS

using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SpriteSlicer2D.Scripts
{
    /// <summary>
    ///     Helper class to pass information about sliced sprites to the calling method
    /// </summary>
    public class SpriteSlicer2DSliceInfo
    {
        public GameObject SlicedObject { get; set; }
        public Vector2 SliceEnterWorldPosition { get; set; }
        public Vector2 SliceExitWorldPosition { get; set; }
        public List<GameObject> ChildObjects { get; set; } = new();
    }

    /// <summary>
    ///     Main sprite slicer class, provides static functions to slice sprites
    /// </summary>
    public static class SpriteSlicer2D
    {
        // Enable or disable debug logging

        // Child sprite vertices may not be centered around the pivot point of the Unity gameobject, causing unexpected
        // behaviour if you wish to manually rotate or move them after slicing. At the cost of some extra processing, you
        // can enable this flag in order to correctly position them. Set to false by default, which assumes that the user will
        // just let the physics system handle movement.
        public const bool SCentreChildSprites = false;

        // Helper to allow ExplodeSprite to slice sub parts that violate the same sprite id or game object rules
        private static List<SpriteSlicer2DSliceInfo> _sSubSlicesCont = new();

        // Static lists to help concave polygon slicing
        private static readonly List<LinkedPolygonPoint> SConcavePolygonPoints = new();
        private static readonly List<LinkedPolygonPoint> SConcavePolygonIntersectionPoints = new();
        private static readonly List<Polygon> SConcaveSlicePolygonResults = new();

        // Vector sorting function
        private static readonly VectorComparer SVectorComparer = new();
        public static bool DebugLoggingEnabled { get; set; }

        /// <summary>
        ///     Get the sprite bounds from the given game object
        /// </summary>
        private static bool GetSpriteBounds(GameObject sprite, out Bounds spriteBounds)
        {
            spriteBounds = new Bounds();
            var boundsValid = false;

#if TK2D_SLICING_ENABLED
		tk2dSprite parenttk2dSprite = sprite.GetComponent<tk2dSprite>();
		
		if(parenttk2dSprite)
		{
			spriteBounds = parenttk2dSprite.GetBounds();
			boundsValid = true;
		}
		else
#endif
            {
                var parentUnitySprite = sprite.GetComponent<SpriteRenderer>();

                if (parentUnitySprite)
                {
                    spriteBounds = parentUnitySprite.sprite.bounds;
                }
                else
                {
                    var parentSlicedSprite = sprite.GetComponent<SlicedSprite>();

                    if (parentSlicedSprite == null) return false;
                    spriteBounds = parentSlicedSprite.SpriteBounds;
                }
            }

            return true;
        }

        /// <summary>
        ///     Slice any sprite with an attached PolygonCollider2D that intersects the given ray
        /// </summary>
        /// <param name="worldStartPoint">Cut world start point.</param>
        /// <param name="worldEndPoint">Cut world end point.</param>
        /// <param name="spriteObject">The specific sprite to cut - pass null to cut any sprite</param>
        /// <param name="spriteInstanceID">The specific sprite unique ID to cut - pass 0 to cut any sprite</param>
        /// <param name="destroySlicedObjects">
        ///     Whether to automatically destroy the parent object - if false, the calling code must
        ///     be responsible
        /// </param>
        /// <param name="maxCutDepth">
        ///     Max cut depth - prevents a sprite from being subdivided too many times. Pass -1 to divide
        ///     infinitely.
        /// </param>
        /// <param name="slicedObjectInfo">A list of information regarding the sliced objects, cut locations etc.</param>
        private static void SliceSpritesInternal(Vector3 worldStartPoint, Vector3 worldEndPoint,
            GameObject spriteObject,
            int spriteInstanceID, bool destroySlicedObjects, int maxCutDepth,
            ref List<SpriteSlicer2DSliceInfo> slicedObjectInfo, LayerMask layerMask, string tag)
        {
#if IGNORE_RAYCAST_TRIGGERS
        bool queriesHitTriggers = Physics2D.queriesHitTriggers;
        Physics2D.queriesHitTriggers = false;
#endif

            var direction = Vector3.Normalize(worldEndPoint - worldStartPoint);
            var length = Vector3.Distance(worldStartPoint, worldEndPoint);
            var cutStartResults = Physics2D.RaycastAll(worldStartPoint, direction, length, layerMask.value);
            var cutEndResults = Physics2D.RaycastAll(worldEndPoint, -direction, length, layerMask.value);

            if (cutStartResults.Length == cutEndResults.Length)
                for (var cutResultIndex = 0;
                     cutResultIndex < cutStartResults.Length && cutResultIndex < cutEndResults.Length;
                     cutResultIndex++)
                {
                    var cutEnter = cutStartResults[cutResultIndex];

                    var cutExitIndex = -1;

                    // Find the matching cut end point in the cut end results
                    for (var endResultIndex = 0; endResultIndex < cutEndResults.Length; endResultIndex++)
                        if (cutEndResults[endResultIndex].collider == cutEnter.collider)
                        {
                            cutExitIndex = endResultIndex;
                            break;
                        }

                    if (cutExitIndex == -1) continue;

                    var cutExit = cutEndResults[cutExitIndex];

                    if (cutEnter.rigidbody == cutExit.rigidbody)
                    {
                        var parentRigidBody = cutEnter.rigidbody;
                        var parentTransform = cutEnter.transform;
                        Color32 spriteColor = Color.white;

                        if (!parentRigidBody) continue;

                        if (parentRigidBody.gameObject.isStatic) continue;

                        if (spriteObject != null && parentRigidBody.gameObject != spriteObject) continue;

                        if (tag != null && parentRigidBody.tag != tag) continue;

#if TK2D_SLICING_ENABLED
					tk2dSprite parenttk2dSprite = parentRigidBody.GetComponent<tk2dSprite>();
#endif

                        SlicedSprite parentSlicedSprite = null;
                        SpriteRenderer parentUnitySprite = null;

                        // The object we're cutting must either be a unity sprite, a tk2D sprite, or a previously sliced sprite
#if TK2D_SLICING_ENABLED
					if(parenttk2dSprite == null)
#endif
                        {
                            parentUnitySprite = parentRigidBody.GetComponent<SpriteRenderer>();

                            if (parentUnitySprite == null)
                            {
                                parentSlicedSprite = parentRigidBody.GetComponent<SlicedSprite>();

                                if (parentSlicedSprite == null || (maxCutDepth >= 0 &&
                                                                   parentSlicedSprite.CutsSinceParentObject >=
                                                                   maxCutDepth))
                                    continue;
                            }
                            else
                            {
                                spriteColor = parentUnitySprite.color;
                            }
                        }

                        // If we've passed in a specific spriteInstanceID, then only that specific object or sliced
                        // objects derived from it can be cut
                        if (spriteInstanceID != 0 && parentRigidBody.gameObject.GetInstanceID() != spriteInstanceID)
                            if (parentSlicedSprite == null || parentSlicedSprite.ParentInstanceID != spriteInstanceID)
                                continue;

                        var cutStartLocalPoint = parentTransform.InverseTransformPoint(worldStartPoint);
                        var cutEndLocalPoint = parentTransform.InverseTransformPoint(worldEndPoint);

                        var cutEnterLocalPoint = parentTransform.InverseTransformPoint(cutEnter.point);
                        var cutExitLocalPoint = parentTransform.InverseTransformPoint(cutExit.point);

                        List<Vector2> polygonPoints = null;
                        var polygonCollider = parentRigidBody.GetComponent<PolygonCollider2D>();
                        PhysicsMaterial2D physicsMaterial = null;

                        if (polygonCollider)
                        {
                            polygonPoints = new List<Vector2>(polygonCollider.points);
                            physicsMaterial = polygonCollider.sharedMaterial;
                        }
                        else
                        {
                            var boxCollider = parentRigidBody.GetComponent<BoxCollider2D>();

                            if (boxCollider)
                            {
                                polygonPoints = new List<Vector2>(4);

#if UNITY_5
                            Vector2 colliderCenter = boxCollider.offset;
#else
                                var colliderCenter = boxCollider.offset;
#endif

                                polygonPoints.Add(colliderCenter +
                                                  new Vector2(-boxCollider.size.x * 0.5f, -boxCollider.size.y * 0.5f));
                                polygonPoints.Add(colliderCenter +
                                                  new Vector2(boxCollider.size.x * 0.5f, -boxCollider.size.y * 0.5f));
                                polygonPoints.Add(colliderCenter +
                                                  new Vector2(boxCollider.size.x * 0.5f, boxCollider.size.y * 0.5f));
                                polygonPoints.Add(colliderCenter +
                                                  new Vector2(-boxCollider.size.x * 0.5f, boxCollider.size.y * 0.5f));
                                physicsMaterial = boxCollider.sharedMaterial;
                            }
                            else
                            {
                                var circleCollider = parentRigidBody.GetComponent<CircleCollider2D>();

                                if (circleCollider)
                                {
                                    var numSteps = 32;
                                    var angleStepRate = Mathf.PI * 2 / numSteps;
                                    polygonPoints = new List<Vector2>(numSteps);

#if UNITY_5
                                Vector2 colliderCenter = circleCollider.offset;
#else
                                    var colliderCenter = circleCollider.offset;
#endif

                                    for (var loop = 0; loop < numSteps; loop++)
                                    {
                                        var angle = angleStepRate * loop;
                                        polygonPoints.Add(colliderCenter +
                                                          new Vector2(Mathf.Sin(angle), Mathf.Cos(angle)) *
                                                          circleCollider.radius);
                                    }

                                    physicsMaterial = circleCollider.sharedMaterial;
                                }
                            }
                        }

                        if (polygonPoints != null)
                        {
                            // Collision rays must travel through the whole object - if the ray either starts or
                            // ends inside the object then it is considered invalid
                            if (IsPointInsidePolygon(cutStartLocalPoint, ref polygonPoints) ||
                                IsPointInsidePolygon(cutEndLocalPoint, ref polygonPoints))
                            {
                                if (DebugLoggingEnabled && spriteObject != null)
                                    Debug.LogWarning("Failed to slice " + parentRigidBody.gameObject.name +
                                                     " - start or end cut point is inside the collision mesh");

                                continue;
                            }

                            var sliceInfo = new SpriteSlicer2DSliceInfo();
                            sliceInfo.SlicedObject = parentRigidBody.gameObject;
                            sliceInfo.SliceEnterWorldPosition = cutEnter.point;
                            sliceInfo.SliceExitWorldPosition = cutExit.point;
                            var parentArea = Mathf.Abs(Area(ref polygonPoints));

                            if (IsConvex(ref polygonPoints))
                            {
                                var numPoints = polygonPoints.Count;
                                var childSprite1VerticesUnsorted = new List<Vector2>(numPoints);
                                var childSprite2VerticesUnsorted = new List<Vector2>(numPoints);

                                childSprite1VerticesUnsorted.Add(cutEnterLocalPoint);
                                childSprite1VerticesUnsorted.Add(cutExitLocalPoint);

                                childSprite2VerticesUnsorted.Add(cutEnterLocalPoint);
                                childSprite2VerticesUnsorted.Add(cutExitLocalPoint);

                                for (var vertex = 0; vertex < numPoints; vertex++)
                                {
                                    var point = polygonPoints[vertex];
                                    var determinant =
                                        CalculateDeterminant2X3(cutStartLocalPoint, cutEndLocalPoint, point);

                                    if (determinant > 0)
                                        childSprite1VerticesUnsorted.Add(point);
                                    else
                                        childSprite2VerticesUnsorted.Add(point);
                                }

                                var childSprite1Vertices = ArrangeVertices(ref childSprite1VerticesUnsorted);
                                var childSprite2Vertices = ArrangeVertices(ref childSprite2VerticesUnsorted);

                                var child1Area = GetArea(ref childSprite1Vertices);
                                var child2Area = GetArea(ref childSprite2Vertices);

                                if (!AreVerticesAcceptable(ref childSprite1Vertices, child1Area, true) ||
                                    !AreVerticesAcceptable(ref childSprite2Vertices, child2Area, true)) continue;

                                SlicedSprite childSprite1, childSprite2;
                                PolygonCollider2D child1Collider, child2Collider;

                                CreateChildSprite(parentRigidBody, physicsMaterial, ref childSprite1Vertices,
                                    parentArea,
                                    child1Area, out childSprite1, out child1Collider);
                                childSprite1.gameObject.name = parentRigidBody.gameObject.name + "_child1";

                                CreateChildSprite(parentRigidBody, physicsMaterial, ref childSprite2Vertices,
                                    parentArea,
                                    child2Area, out childSprite2, out child2Collider);
                                childSprite2.gameObject.name = parentRigidBody.gameObject.name + "_child2";

#if TK2D_SLICING_ENABLED
								if(parenttk2dSprite)
								{
									childSprite1.InitFromTK2DSprite(parenttk2dSprite, ref child1Collider, ref childSprite1Vertices, false);
									childSprite2.InitFromTK2DSprite(parenttk2dSprite, ref child2Collider, ref childSprite2Vertices, false);
								}
								else
#endif
                                if (parentSlicedSprite)
                                {
                                    childSprite1.InitFromSlicedSprite(parentSlicedSprite, ref child1Collider,
                                        ref childSprite1Vertices, false);
                                    childSprite2.InitFromSlicedSprite(parentSlicedSprite, ref child2Collider,
                                        ref childSprite2Vertices, false);
                                }
                                else if (parentUnitySprite)
                                {
                                    childSprite1.InitFromUnitySprite(parentUnitySprite, ref child1Collider,
                                        ref childSprite1Vertices, false, spriteColor);
                                    childSprite2.InitFromUnitySprite(parentUnitySprite, ref child2Collider,
                                        ref childSprite2Vertices, false, spriteColor);
                                }

                                sliceInfo.ChildObjects.Add(childSprite1.gameObject);
                                sliceInfo.ChildObjects.Add(childSprite2.gameObject);
                            }
                            else // Use concave slicing method
                            {
                                var polygon = new Polygon();
                                var line = new SpriteSlicerLine(cutStartLocalPoint, cutEndLocalPoint);
                                polygon.Points = polygonPoints;
                                SliceConcave(polygon, line);

                                for (var childIndex = 0; childIndex < SConcaveSlicePolygonResults.Count; childIndex++)
                                {
                                    var childSpriteVertices = SConcaveSlicePolygonResults[childIndex].Points.ToArray();
                                    var childArea = GetArea(ref childSpriteVertices);

                                    if (AreVerticesAcceptable(ref childSpriteVertices, childArea, false))
                                    {
                                        SlicedSprite childSprite;
                                        PolygonCollider2D childCollider;

                                        CreateChildSprite(parentRigidBody, physicsMaterial, ref childSpriteVertices,
                                            parentArea, childArea, out childSprite, out childCollider);
                                        childSprite.gameObject.name =
                                            parentRigidBody.gameObject.name + "_child" + childIndex;

#if TK2D_SLICING_ENABLED
									if(parenttk2dSprite)
									{
										childSprite.InitFromTK2DSprite(parenttk2dSprite, ref childCollider, ref childSpriteVertices, true);
									}
									else
#endif
                                        if (parentSlicedSprite)
                                            childSprite.InitFromSlicedSprite(parentSlicedSprite, ref childCollider,
                                                ref childSpriteVertices, true);
                                        else if (parentUnitySprite)
                                            childSprite.InitFromUnitySprite(parentUnitySprite, ref childCollider,
                                                ref childSpriteVertices, true, spriteColor);

                                        sliceInfo.ChildObjects.Add(childSprite.gameObject);
                                    }
                                }
                            }

                            if (sliceInfo.ChildObjects.Count > 0)
                            {
                                // Send an OnSpriteSliced message to the sliced object
                                parentRigidBody.gameObject.SendMessage("OnSpriteSliced", sliceInfo,
                                    SendMessageOptions.DontRequireReceiver);

                                if (slicedObjectInfo != null)
                                {
                                    // Need to null the parent object as we're about to destroy it
                                    if (destroySlicedObjects) sliceInfo.SlicedObject = null;

                                    slicedObjectInfo.Add(sliceInfo);
                                }

                                if (destroySlicedObjects)
                                    GameObject.Destroy(parentRigidBody.gameObject);
                                else
                                    parentRigidBody.gameObject.SetActive(false);
                            }
                        }
                    }
                }

#if IGNORE_RAYCAST_TRIGGERS
        Physics2D.queriesHitTriggers = queriesHitTriggers;
#endif
        }

        /// <summary>
        ///     Create a child sprite from the given parent sprite, using the provided vertices
        /// </summary>
        private static void CreateChildSprite(Rigidbody2D parentRigidBody, PhysicsMaterial2D physicsMaterial,
            ref Vector2[] spriteVertices, float parentArea, float childArea, out SlicedSprite slicedSprite,
            out PolygonCollider2D polygonCollider)
        {
            var childObject = new GameObject();
            slicedSprite = childObject.AddComponent<SlicedSprite>();

            // Child sprites should inherit the rigid body behaviour of their parents
            var childRigidBody = slicedSprite.GetComponent<Rigidbody2D>();
            childRigidBody.mass = parentRigidBody.mass * (childArea / parentArea);
            childRigidBody.linearDamping = parentRigidBody.linearDamping;
            childRigidBody.angularDamping = parentRigidBody.angularDamping;
            childRigidBody.gravityScale = parentRigidBody.gravityScale;
            childRigidBody.constraints = parentRigidBody.constraints;

            childRigidBody.isKinematic = parentRigidBody.isKinematic;
            childRigidBody.interpolation = parentRigidBody.interpolation;
            childRigidBody.sleepMode = parentRigidBody.sleepMode;
            childRigidBody.collisionDetectionMode = parentRigidBody.collisionDetectionMode;
            childRigidBody.linearVelocity = parentRigidBody.linearVelocity;
            childRigidBody.angularVelocity = parentRigidBody.angularVelocity;

            polygonCollider = slicedSprite.GetComponent<PolygonCollider2D>();
            polygonCollider.SetPath(0, spriteVertices);
            polygonCollider.sharedMaterial = physicsMaterial;
        }

        #region SLICING_METHODS

        /// <summary>
        ///     Slices any sprites that are intersected by the given vector
        /// </summary>
        /// <param name="worldStartPoint">Slice start point in world coordinates.</param>
        /// <param name="worldEndPoint">Slice end point in world coordinates.</param>
        public static void SliceAllSprites(Vector3 worldStartPoint, Vector3 worldEndPoint)
        {
            LayerMask layerMask = -1;
            List<SpriteSlicer2DSliceInfo> slicedObjectInfo = null;
            SliceSpritesInternal(worldStartPoint, worldEndPoint, null, 0, true, -1, ref slicedObjectInfo, layerMask,
                null);
        }

        /// <summary>
        ///     Slices any sprites that are intersected by the given vector
        /// </summary>
        /// <param name="worldStartPoint">Slice start point in world coordinates.</param>
        /// <param name="worldEndPoint">Slice end point in world coordinates.</param>
        /// <param name="layerMask">Layermask to use in raycast operations.</param>
        public static void SliceAllSprites(Vector3 worldStartPoint, Vector3 worldEndPoint, LayerMask layerMask)
        {
            List<SpriteSlicer2DSliceInfo> slicedObjectInfo = null;
            SliceSpritesInternal(worldStartPoint, worldEndPoint, null, 0, true, -1, ref slicedObjectInfo, layerMask,
                null);
        }

        /// <summary>
        ///     Slices any sprites that are intersected by the given vector
        /// </summary>
        /// <param name="worldStartPoint">Slice start point in world coordinates.</param>
        /// <param name="worldEndPoint">Slice end point in world coordinates.</param>
        /// <param name="layerMask">Layermask to use in raycast operations.</param>
        /// <param name="tag">Only sprites with the given tag can be cut.</param>
        public static void SliceAllSprites(Vector3 worldStartPoint, Vector3 worldEndPoint, string tag)
        {
            LayerMask layerMask = -1;
            List<SpriteSlicer2DSliceInfo> slicedObjectInfo = null;
            SliceSpritesInternal(worldStartPoint, worldEndPoint, null, 0, true, -1, ref slicedObjectInfo, layerMask,
                tag);
        }

        /// <summary>
        ///     Slices any sprites that are intersected by the given vector
        /// </summary>
        /// <param name="worldStartPoint">Slice start point in world coordinates.</param>
        /// <param name="worldEndPoint">Slice end point in world coordinates.</param>
        /// <param name="destroySlicedObjects">
        ///     Controls whether the parent objects are automatically destroyed. Set to false if you
        ///     need to perform additional processing on them after slicing
        /// </param>
        /// <param name="slicedObjectInfo">
        ///     A list of SpriteSlicer2DSliceInfo that will be fill out with details about slice
        ///     locations, slcied objects, and created child objects.
        /// </param>
        public static void SliceAllSprites(Vector3 worldStartPoint, Vector3 worldEndPoint, bool destroySlicedObjects,
            ref List<SpriteSlicer2DSliceInfo> slicedObjectInfo)
        {
            LayerMask layerMask = -1;
            SliceSpritesInternal(worldStartPoint, worldEndPoint, null, 0, destroySlicedObjects, -1,
                ref slicedObjectInfo,
                layerMask, null);
        }

        /// <summary>
        ///     Slices any sprites that are intersected by the given vector
        /// </summary>
        /// <param name="worldStartPoint">Slice start point in world coordinates.</param>
        /// <param name="worldEndPoint">Slice end point in world coordinates.</param>
        /// <param name="destroySlicedObjects">
        ///     Controls whether the parent objects are automatically destroyed. Set to false if you
        ///     need to perform additional processing on them after slicing
        /// </param>
        /// <param name="slicedObjectInfo">
        ///     A list of SpriteSlicer2DSliceInfo that will be fill out with details about slice
        ///     locations, slcied objects, and created child objects.
        /// </param>
        /// <param name="tag">Only sprites with the given tag can be cut.</param>
        public static void SliceAllSprites(Vector3 worldStartPoint, Vector3 worldEndPoint, bool destroySlicedObjects,
            ref List<SpriteSlicer2DSliceInfo> slicedObjectInfo, string tag)
        {
            LayerMask layerMask = -1;
            SliceSpritesInternal(worldStartPoint, worldEndPoint, null, 0, destroySlicedObjects, -1,
                ref slicedObjectInfo,
                layerMask, tag);
        }

        /// <summary>
        ///     Slices any sprites that are intersected by the given vector
        /// </summary>
        /// <param name="worldStartPoint">Slice start point in world coordinates.</param>
        /// <param name="worldEndPoint">Slice end point in world coordinates.</param>
        /// <param name="destroySlicedObjects">
        ///     Controls whether the parent objects are automatically destroyed. Set to false if you
        ///     need to perform additional processing on them after slicing
        /// </param>
        /// <param name="slicedObjectInfo">
        ///     A list of SpriteSlicer2DSliceInfo that will be fill out with details about slice
        ///     locations, slcied objects, and created child objects.
        /// </param>
        /// <param name="layerMask">Layermask to use in raycast operations.</param>
        public static void SliceAllSprites(Vector3 worldStartPoint, Vector3 worldEndPoint, bool destroySlicedObjects,
            ref List<SpriteSlicer2DSliceInfo> slicedObjectInfo, LayerMask layerMask)
        {
            SliceSpritesInternal(worldStartPoint, worldEndPoint, null, 0, destroySlicedObjects, -1,
                ref slicedObjectInfo,
                layerMask, null);
        }

        /// <summary>
        ///     Slices any sprites that are intersected by the given vector
        /// </summary>
        /// <param name="worldStartPoint">Slice start point in world coordinates.</param>
        /// <param name="worldEndPoint">Slice end point in world coordinates.</param>
        /// <param name="destroySlicedObjects">
        ///     Controls whether the parent objects are automatically destroyed. Set to false if you
        ///     need to perform additional processing on them after slicing
        /// </param>
        /// <param name="maxCutDepth">The maximum number of times that any sprite can be subdivided</param>
        /// <param name="slicedObjectInfo">
        ///     A list of SpriteSlicer2DSliceInfo that will be fill out with details about slice
        ///     locations, slcied objects, and created child objects.
        /// </param>
        public static void SliceAllSprites(Vector3 worldStartPoint, Vector3 worldEndPoint, bool destroySlicedObjects,
            int maxCutDepth, ref List<SpriteSlicer2DSliceInfo> slicedObjectInfo)
        {
            LayerMask layerMask = -1;
            SliceSpritesInternal(worldStartPoint, worldEndPoint, null, 0, destroySlicedObjects, maxCutDepth,
                ref slicedObjectInfo, layerMask, null);
        }

        /// <summary>
        ///     Slices any sprites that are intersected by the given vector
        /// </summary>
        /// <param name="worldStartPoint">Slice start point in world coordinates.</param>
        /// <param name="worldEndPoint">Slice end point in world coordinates.</param>
        /// <param name="destroySlicedObjects">
        ///     Controls whether the parent objects are automatically destroyed. Set to false if you
        ///     need to perform additional processing on them after slicing
        /// </param>
        /// <param name="maxCutDepth">The maximum number of times that any sprite can be subdivided</param>
        /// <param name="slicedObjectInfo">
        ///     A list of SpriteSlicer2DSliceInfo that will be fill out with details about slice
        ///     locations, slcied objects, and created child objects.
        /// </param>
        /// <param name="layerMask">Layermask to use in raycast operations.</param>
        public static void SliceAllSprites(Vector3 worldStartPoint, Vector3 worldEndPoint, bool destroySlicedObjects,
            int maxCutDepth, ref List<SpriteSlicer2DSliceInfo> slicedObjectInfo, LayerMask layerMask)
        {
            SliceSpritesInternal(worldStartPoint, worldEndPoint, null, 0, destroySlicedObjects, maxCutDepth,
                ref slicedObjectInfo, layerMask, null);
        }

        /// <summary>
        ///     Slices a specific sprite if it is intersected by the given vector
        /// </summary>
        /// <param name="worldStartPoint">Slice start point in world coordinates.</param>
        /// <param name="worldEndPoint">Slice end point in world coordinates.</param>
        /// <param name="sprite">The sprite to cut</param>
        public static void SliceSprite(Vector3 worldStartPoint, Vector3 worldEndPoint, GameObject sprite)
        {
            if (sprite)
            {
                LayerMask layerMask = -1;
                List<SpriteSlicer2DSliceInfo> slicedObjectInfo = null;
                SliceSpritesInternal(worldStartPoint, worldEndPoint, sprite, 0, true, -1, ref slicedObjectInfo,
                    layerMask,
                    null);
            }
        }

        /// <summary>
        ///     Slices a specific sprite if it is intersected by the given vector
        /// </summary>
        /// <param name="worldStartPoint">Slice start point in world coordinates.</param>
        /// <param name="worldEndPoint">Slice end point in world coordinates.</param>
        /// <param name="sprite">The sprite to cut</param>
        /// <param name="destroySlicedObjects">
        ///     Controls whether the parent objects are automatically destroyed. Set to false if you
        ///     need to perform additional processing on them after slicing
        /// </param>
        /// <param name="slicedObjectInfo">
        ///     A list of SpriteSlicer2DSliceInfo that will be fill out with details about slice
        ///     locations, slcied objects, and created child objects.
        /// </param>
        public static void SliceSprite(Vector3 worldStartPoint, Vector3 worldEndPoint, GameObject sprite,
            bool destroySlicedObjects, ref List<SpriteSlicer2DSliceInfo> slicedObjectInfo)
        {
            if (sprite)
            {
                LayerMask layerMask = -1;
                SliceSpritesInternal(worldStartPoint, worldEndPoint, sprite, 0, destroySlicedObjects, -1,
                    ref slicedObjectInfo, layerMask, null);
            }
        }

        /// <summary>
        ///     Slices a specific sprite if it is intersected by the given vector
        /// </summary>
        /// <param name="worldStartPoint">Slice start point in world coordinates.</param>
        /// <param name="worldEndPoint">Slice end point in world coordinates.</param>
        /// <param name="sprite">The sprite to cut</param>
        /// <param name="destroySlicedObjects">
        ///     Controls whether the parent objects are automatically destroyed. Set to false if you
        ///     need to perform additional processing on them after slicing
        /// </param>
        /// <param name="maxCutDepth">The maximum number of times that any sprite can be subdivided</param>
        /// <param name="slicedObjectInfo">
        ///     A list of SpriteSlicer2DSliceInfo that will be fill out with details about slice
        ///     locations, slcied objects, and created child objects.
        /// </param>
        public static void SliceSprite(Vector3 worldStartPoint, Vector3 worldEndPoint, GameObject sprite,
            bool destroySlicedObjects, int maxCutDepth, ref List<SpriteSlicer2DSliceInfo> slicedObjectInfo)
        {
            if (sprite)
            {
                LayerMask layerMask = -1;
                SliceSpritesInternal(worldStartPoint, worldEndPoint, sprite, 0, destroySlicedObjects, maxCutDepth,
                    ref slicedObjectInfo, layerMask, null);
            }
        }

        /// <summary>
        ///     Explode a sprite by cutting it multiple times through the centre and then applying a force away from the center
        /// </summary>
        /// <param name="sprite">The sprite to cut</param>
        /// <param name="numCuts">How many random cuts to create.</param>
        /// <param name="explosionForce">The explosive force that will be applied to the newly created objects.</param>
        /// <param name="maxExplosionDepth">How many times an individual sprite can be exploded.</param>
        public static void ExplodeSprite(GameObject sprite, int numCuts, float explosionForce,
            int maxExplosionDepth = -1)
        {
            if (sprite)
            {
                List<SpriteSlicer2DSliceInfo> slicedObjectInfo = null;

                // Need to remember our objects if we're applying forces to them
                if (explosionForce != 0.0f) slicedObjectInfo = new List<SpriteSlicer2DSliceInfo>();

                ExplodeSprite(sprite, numCuts, explosionForce, true, ref slicedObjectInfo, maxExplosionDepth);
            }
        }

        /// <summary>
        ///     Explode a sprite by cutting it multiple times and then applying a force away from the center
        /// </summary>
        /// <param name="sprite">The sprite to cut</param>
        /// <param name="numCuts">How many random cuts to create.</param>
        /// <param name="explosionForce">The explosive force that will be applied to the newly created objects.</param>
        /// <param name="destroySlicedObjects">
        ///     Controls whether the parent objects are automatically destroyed. Set to false if you
        ///     need to perform additional processing on them after slicing
        /// </param>
        /// <param name="slicedObjectInfo">
        ///     A list of SpriteSlicer2DSliceInfo that will be fill out with details about slice
        ///     locations, slcied objects, and created child objects.
        /// </param>
        /// <param name="maxExplosionDepth">How many times an individual sprite can be exploded.</param>
        public static void ExplodeSprite(GameObject sprite, int numCuts, float explosionForce,
            bool destroySlicedObjects,
            ref List<SpriteSlicer2DSliceInfo> slicedObjectInfo, int maxExplosionDepth = -1)
        {
            if (!sprite) return;

            Bounds spriteBounds;

            if (GetSpriteBounds(sprite, out spriteBounds))
            {
                LayerMask layerMask = -1;
                var parentSpriteID = sprite.GetInstanceID();
                var centre = sprite.transform.position;
                var maxRadius = spriteBounds.size.x + spriteBounds.size.y;
                var explosionsSinceParentObject = 0;
                var parentSlicedSpriteComponent = sprite.GetComponent<SlicedSprite>();

                if (parentSlicedSpriteComponent)
                    explosionsSinceParentObject = parentSlicedSpriteComponent.ExplosionsSinceParentObject;

                // Max explosion depth reached, not valid to explode
                if (maxExplosionDepth >= 0 && explosionsSinceParentObject >= maxExplosionDepth) return;

                for (var loop = 0; loop < numCuts; loop++)
                {
                    var randomAngle = Random.Range(0, Mathf.PI * 2);
                    var angleOffset = new Vector3(Mathf.Sin(randomAngle), Mathf.Cos(randomAngle), 0.0f) * maxRadius;

                    randomAngle = Random.Range(0, Mathf.PI * 2);

                    // Randomly apply jitter to ensure that every slice doesn't go through the centre
                    var jitterOffset = new Vector3(Mathf.Sin(randomAngle) * (spriteBounds.size.x * 0.25f),
                        Mathf.Cos(randomAngle) * (spriteBounds.size.y * 0.25f), 0.0f);

                    var worldStartPoint = centre + angleOffset + jitterOffset;
                    var worldEndPoint = centre - angleOffset + jitterOffset;
                    SliceSpritesInternal(worldStartPoint, worldEndPoint, null, parentSpriteID, destroySlicedObjects, -1,
                        ref slicedObjectInfo, layerMask, null);

                    _sSubSlicesCont.Clear();

                    // Without this we only end up slicing the original object twice regardless of the slices requested
                    for (var i = 0; i < slicedObjectInfo.Count; i++)
                    for (var j = 0; j < slicedObjectInfo[i].ChildObjects.Count; j++)
                        SliceSpritesInternal(worldStartPoint, worldEndPoint, slicedObjectInfo[i].ChildObjects[j], 0,
                            destroySlicedObjects, -1, ref _sSubSlicesCont, layerMask, null);

                    slicedObjectInfo.AddRange(_sSubSlicesCont);
                }

                if (slicedObjectInfo != null)
                    for (var slice = 0; slice < slicedObjectInfo.Count; slice++)
                    for (var child = 0; child < slicedObjectInfo[slice].ChildObjects.Count; child++)
                    {
                        var slicedSpriteComponent =
                            slicedObjectInfo[slice].ChildObjects[child].GetComponent<SlicedSprite>();

                        if (slicedSpriteComponent)
                            slicedSpriteComponent.ExplosionsSinceParentObject = explosionsSinceParentObject + 1;

                        if (explosionForce != 0.0f)
                        {
                            var childRigidBody =
                                slicedObjectInfo[slice].ChildObjects[child].GetComponent<Rigidbody2D>();

                            if (childRigidBody)
                                childRigidBody.AddForceAtPosition(new Vector2(0.0f, 1.0f) * explosionForce, centre);
                        }
                    }
            }
        }

        /// <summary>
        ///     Shatters a sprite into its constituent polygons and applies an optional force
        /// </summary>
        /// <param name="sprite">The sprite to cut</param>
        /// <param name="explosionForce">The explosive force that will be applied to the newly created objects.</param>
        public static void ShatterSprite(GameObject spriteObject, float explosionForce)
        {
            List<SpriteSlicer2DSliceInfo> slicedObjectInfo = null;
            ShatterSprite(spriteObject, explosionForce, true, ref slicedObjectInfo);
        }

        /// <summary>
        ///     Shatters a sprite into its constituent polygons and applies an optional force
        /// </summary>
        /// <param name="sprite">The sprite to cut</param>
        /// <param name="explosionForce">The explosive force that will be applied to the newly created objects.</param>
        /// <param name="destroySlicedObjects">
        ///     Controls whether the parent objects are automatically destroyed. Set to false if you
        ///     need to perform additional processing on them after slicing
        /// </param>
        /// <param name="slicedObjectInfo">
        ///     A list of SpriteSlicer2DSliceInfo that will be fill out with details about slice
        ///     locations, slcied objects, and created child objects.
        /// </param>
        public static void ShatterSprite(GameObject spriteObject, float explosionForce, bool destroySlicedObjects,
            ref List<SpriteSlicer2DSliceInfo> slicedObjectInfo)
        {
            var parentRigidBody = spriteObject.GetComponent<Rigidbody2D>();

            if (!parentRigidBody)
            {
                Debug.LogWarning("Could not shatter sprite - no attached rigidbody");
                return;
            }

#if TK2D_SLICING_ENABLED
		tk2dSprite parenttk2dSprite = parentRigidBody.GetComponent<tk2dSprite>();
#endif

            SlicedSprite parentSlicedSprite = null;
            SpriteRenderer parentUnitySprite = null;

            // The object we're cutting must either be a unity sprite, a tk2D sprite, or a previously sliced sprite
#if TK2D_SLICING_ENABLED
		if(parenttk2dSprite == null)
#endif
            {
                parentUnitySprite = spriteObject.GetComponent<SpriteRenderer>();

                if (parentUnitySprite == null)
                {
                    parentSlicedSprite = spriteObject.GetComponent<SlicedSprite>();

                    if (parentSlicedSprite == null) return;
                }
            }

            List<Vector2> polygonPoints = null;
            var polygonCollider = spriteObject.GetComponent<PolygonCollider2D>();
            PhysicsMaterial2D physicsMaterial = null;

            if (polygonCollider)
            {
                polygonPoints = new List<Vector2>(polygonCollider.points);
                physicsMaterial = polygonCollider.sharedMaterial;
            }
            else
            {
                var boxCollider = spriteObject.GetComponent<BoxCollider2D>();

                if (boxCollider)
                {
                    polygonPoints = new List<Vector2>(4);
                    polygonPoints.Add(new Vector2(-boxCollider.size.x * 0.5f, -boxCollider.size.y * 0.5f));
                    polygonPoints.Add(new Vector2(boxCollider.size.x * 0.5f, -boxCollider.size.y * 0.5f));
                    polygonPoints.Add(new Vector2(boxCollider.size.x * 0.5f, boxCollider.size.y * 0.5f));
                    polygonPoints.Add(new Vector2(-boxCollider.size.x * 0.5f, boxCollider.size.y * 0.5f));
                    physicsMaterial = boxCollider.sharedMaterial;
                }
                else
                {
                    var circleCollider = spriteObject.GetComponent<CircleCollider2D>();

                    if (circleCollider)
                    {
                        var numSteps = 32;
                        var angleStepRate = Mathf.PI * 2 / numSteps;
                        polygonPoints = new List<Vector2>(numSteps);

                        for (var loop = 0; loop < numSteps; loop++)
                        {
                            var angle = angleStepRate * loop;
                            polygonPoints.Add(new Vector2(Mathf.Sin(angle), Mathf.Cos(angle)) * circleCollider.radius);
                        }

                        physicsMaterial = circleCollider.sharedMaterial;
                    }
                }
            }

            if (polygonPoints != null && polygonPoints.Count > 3)
            {
                SpriteSlicer2DSliceInfo sliceInfo = null;

                if (slicedObjectInfo != null)
                {
                    sliceInfo = new SpriteSlicer2DSliceInfo();
                    slicedObjectInfo.Add(sliceInfo);

                    if (!destroySlicedObjects) sliceInfo.SlicedObject = spriteObject;
                }

                Vector2 parentCentre = spriteObject.transform.position;
                var triangles = Triangulate(ref polygonPoints);
                var points = new List<Vector2>(3);
                points.Add(Vector2.zero);
                points.Add(Vector2.zero);
                points.Add(Vector2.zero);

                var parentArea = Mathf.Abs(Area(ref polygonPoints));

                for (var loop = 0; loop < triangles.Length; loop += 3)
                {
                    points[0] = polygonPoints[triangles[loop]];
                    points[1] = polygonPoints[triangles[loop + 1]];
                    points[2] = polygonPoints[triangles[loop + 2]];

                    SlicedSprite childSprite;
                    PolygonCollider2D childCollider;

                    var childSpriteVertices = points.ToArray();
                    ;
                    var childArea = GetArea(ref childSpriteVertices);
                    CreateChildSprite(parentRigidBody, physicsMaterial, ref childSpriteVertices, parentArea, childArea,
                        out childSprite, out childCollider);
                    childSprite.gameObject.name = spriteObject.name + "_child";

#if TK2D_SLICING_ENABLED
				if(parenttk2dSprite)
				{
					childSprite.InitFromTK2DSprite(parenttk2dSprite, ref childCollider, ref childSpriteVertices, false);
				}
				else
#endif
                    if (parentSlicedSprite)
                        childSprite.InitFromSlicedSprite(parentSlicedSprite, ref childCollider, ref childSpriteVertices,
                            false);
                    else if (parentUnitySprite)
                        childSprite.InitFromUnitySprite(parentUnitySprite, ref childCollider, ref childSpriteVertices,
                            false, parentUnitySprite.color);

                    var centrePosition = parentCentre +
                                         (childSpriteVertices[0] + childSpriteVertices[1] + childSpriteVertices[2]) *
                                         0.33f;
                    var forceDirection = centrePosition - parentCentre;
                    forceDirection.Normalize();
                    childSprite.GetComponent<Rigidbody2D>()
                        .AddForceAtPosition(forceDirection * explosionForce, parentCentre);

                    if (sliceInfo != null) sliceInfo.ChildObjects.Add(childSprite.gameObject);
                }

                if (destroySlicedObjects)
                    GameObject.Destroy(parentRigidBody.gameObject);
                else
                    parentRigidBody.gameObject.SetActive(false);
            }
        }

        #endregion

        #region "HELPER_FUNCTIONS"

        private enum LineSide
        {
            Left,
            Right,
            On
        }

        private struct SpriteSlicerLine
        {
            public SpriteSlicerLine(Vector2 a, Vector2 b)
            {
                P1 = a;
                P2 = b;
            }

            public readonly Vector2 P1;
            public readonly Vector2 P2;
        }

        public class Polygon
        {
            public List<Vector2> Points = new();
        }

        private class LinkedPolygonPoint
        {
            public readonly Vector2 Position;
            public readonly LineSide SliceSide;
            public float DistOnLine;
            public LinkedPolygonPoint Next;
            public LinkedPolygonPoint Prev;
            public bool Visited;

            public LinkedPolygonPoint(Vector2 startPos, LineSide side)
            {
                Position = startPos;
                SliceSide = side;
            }
        }

        private static LineSide GetSideOfLine(SpriteSlicerLine line, Vector2 pt)
        {
            var d = (pt.x - line.P1.x) * (line.P2.y - line.P1.y) - (pt.y - line.P1.y) * (line.P2.x - line.P1.x);
            return d > float.Epsilon ? LineSide.Right : d < -float.Epsilon ? LineSide.Left : LineSide.On;
        }

        private static float CalculateDeterminant2X3(Vector2 start, Vector2 end, Vector2 point)
        {
            return start.x * end.y + end.x * point.y + point.x * start.y - start.y * end.x - end.y * point.x -
                   point.y * start.x;
        }

        public static float CalculateDeterminant2X2(Vector2 vectorA, Vector2 vectorB)
        {
            return vectorA.x * vectorB.y - vectorA.y * vectorB.x;
        }

        public static float DotProduct(Vector2 lineStart, Vector2 lineEnd, Vector2 point)
        {
            return (point.x - lineStart.x) * (lineEnd.x - lineStart.x) +
                   (point.y - lineStart.y) * (lineEnd.y - lineStart.y);
        }

        // Trianglulate the given polygon
        public static int[] Triangulate(ref List<Vector2> points)
        {
            var indices = new List<int>();

            var n = points.Count;
            if (n < 3)
                return indices.ToArray();

            var V = new int[n];

            if (Area(ref points) > 0)
                for (var v = 0; v < n; v++)
                    V[v] = v;
            else
                for (var v = 0; v < n; v++)
                    V[v] = n - 1 - v;

            var nv = n;
            var count = 2 * nv;
            for (int m = 0, v = nv - 1; nv > 2;)
            {
                if (count-- <= 0)
                    return indices.ToArray();

                var u = v;
                if (nv <= u)
                    u = 0;
                v = u + 1;
                if (nv <= v)
                    v = 0;
                var w = v + 1;
                if (nv <= w)
                    w = 0;

                if (Snip(ref points, u, v, w, nv, V))
                {
                    int s, t;
                    var a = V[u];
                    var b = V[v];
                    var c = V[w];
                    indices.Add(a);
                    indices.Add(b);
                    indices.Add(c);
                    m++;
                    for (s = v, t = v + 1; t < nv; s++, t++)
                        V[s] = V[t];
                    nv--;
                    count = 2 * nv;
                }
            }

            indices.Reverse();
            return indices.ToArray();
        }

        // Get the area of the given polygon
        private static float Area(ref List<Vector2> points)
        {
            var n = points.Count;
            var a = 0.0f;

            for (int p = n - 1, q = 0; q < n; p = q++)
            {
                var pval = points[p];
                var qval = points[q];
                a += pval.x * qval.y - qval.x * pval.y;
            }

            return a * 0.5f;
        }

        private static bool Snip(ref List<Vector2> points, int u, int v, int w, int n, int[] V)
        {
            int p;
            var a = points[V[u]];
            var b = points[V[v]];
            var c = points[V[w]];

            if (Mathf.Epsilon > (b.x - a.x) * (c.y - a.y) - (b.y - a.y) * (c.x - a.x)) return false;

            for (p = 0; p < n; p++)
            {
                if (p == u || p == v || p == w) continue;

                var P = points[V[p]];

                if (InsideTriangle(a, b, c, P)) return false;
            }

            return true;
        }

        // Check if a point is inside a given triangle
        private static bool InsideTriangle(Vector2 a, Vector2 b, Vector2 c, Vector2 P)
        {
            var ax = c.x - b.x;
            var ay = c.y - b.y;
            var bx = a.x - c.x;
            var by = a.y - c.y;
            var cx = b.x - a.x;
            var cy = b.y - a.y;
            var apx = P.x - a.x;
            var apy = P.y - a.y;
            var bpx = P.x - b.x;
            var bpy = P.y - b.y;
            var cpx = P.x - c.x;
            var cpy = P.y - c.y;

            var aCrosSbp = ax * bpy - ay * bpx;
            var cCrosSap = cx * apy - cy * apx;
            var bCrosScp = bx * cpy - by * cpx;

            return aCrosSbp >= 0.0f && bCrosScp >= 0.0f && cCrosSap >= 0.0f;
        }

        // Helper class to sort vertices in ascending X coordinate order
        public class VectorComparer : IComparer<Vector2>
        {
            public int Compare(Vector2 vectorA, Vector2 vectorB)
            {
                if (vectorA.x > vectorB.x) return 1;

                if (vectorA.x < vectorB.x) return -1;

                return 0;
            }
        }

        // Helper class to sort polygon edges
        private class EdgeComparer : IComparer<LinkedPolygonPoint>
        {
            private readonly SpriteSlicerLine _line;

            public EdgeComparer(SpriteSlicerLine ln)
            {
                _line = ln;
            }

            public int Compare(LinkedPolygonPoint edgeA, LinkedPolygonPoint edgeB)
            {
                var dotA = DotProduct(_line.P1, _line.P2, edgeA.Position);
                var dotB = DotProduct(_line.P1, _line.P2, edgeB.Position);

                if (dotA < dotB) return -1;

                return 1;
            }
        }

        /// <summary>
        ///     Sort the vertices into a counter clockwise order
        /// </summary>
        private static Vector2[] ArrangeVertices(ref List<Vector2> vertices)
        {
            float determinant;
            var numVertices = vertices.Count;
            var counterClockWiseIndex = 1;
            var clockWiseIndex = numVertices - 1;
            vertices.Sort(SVectorComparer);
            var sortedVertices = new List<Vector2>(numVertices);

            for (var vertex = 0; vertex < numVertices; vertex++) sortedVertices.Add(Vector2.zero);

            var startPoint = vertices[0];
            var endPoint = vertices[numVertices - 1];
            sortedVertices[0] = startPoint;

            for (var vertex = 1; vertex < numVertices - 1; vertex++)
            {
                determinant = CalculateDeterminant2X3(startPoint, endPoint, vertices[vertex]);

                if (determinant < 0)
                    sortedVertices[counterClockWiseIndex++] = vertices[vertex];
                else
                    sortedVertices[clockWiseIndex--] = vertices[vertex];
            }

            sortedVertices[counterClockWiseIndex] = endPoint;
            return sortedVertices.ToArray();
        }

        /// <summary>
        ///     Work out the area defined by the vertices
        /// </summary>
        private static float GetArea(ref Vector2[] vertices)
        {
            // Check that the total area isn't stupidly small
            var numVertices = vertices.Length;
            var area = vertices[0].y * (vertices[numVertices - 1].x - vertices[1].x);

            for (var i = 1; i < numVertices; i++)
                area += vertices[i].y * (vertices[i - 1].x - vertices[(i + 1) % numVertices].x);

            return Mathf.Abs(area * 0.5f);
        }

        /// <summary>
        ///     Check if this list of points defines a convex shape
        /// </summary>
        public static bool IsConvex(ref Vector2[] vertices)
        {
            var numVertices = vertices.Length;
            float determinant;
            Vector3 v1 = vertices[0] - vertices[numVertices - 1];
            Vector3 v2 = vertices[1] - vertices[0];
            var referenceDeterminant = CalculateDeterminant2X2(v1, v2);

            for (var i = 1; i < numVertices - 1; i++)
            {
                v1 = v2;
                v2 = vertices[i + 1] - vertices[i];
                determinant = CalculateDeterminant2X2(v1, v2);

                if (referenceDeterminant * determinant < 0.0f) return false;
            }

            v1 = v2;
            v2 = vertices[0] - vertices[numVertices - 1];
            determinant = CalculateDeterminant2X2(v1, v2);

            if (referenceDeterminant * determinant < 0.0f) return false;

            return true;
        }

        /// <summary>
        ///     Check if this list of points defines a convex shape
        /// </summary>
        public static bool IsConvex(ref List<Vector2> vertices)
        {
            var numVertices = vertices.Count;
            float determinant;
            Vector3 v1 = vertices[0] - vertices[numVertices - 1];
            Vector3 v2 = vertices[1] - vertices[0];
            var referenceDeterminant = CalculateDeterminant2X2(v1, v2);

            for (var i = 1; i < numVertices - 1; i++)
            {
                v1 = v2;
                v2 = vertices[i + 1] - vertices[i];
                determinant = CalculateDeterminant2X2(v1, v2);

                if (referenceDeterminant * determinant < 0.0f) return false;
            }

            v1 = v2;
            v2 = vertices[0] - vertices[numVertices - 1];
            determinant = CalculateDeterminant2X2(v1, v2);

            if (referenceDeterminant * determinant < 0.0f) return false;

            return true;
        }

        /// <summary>
        ///     Verify if the list of vertices are suitable to create a new 2D collider shape
        private static bool AreVerticesAcceptable(ref Vector2[] vertices, float area, bool failOnConcave)
        {
            // Polygons need to at least have 3 vertices, not be convex, and have a vaguely sensible total area
            if (vertices.Length < 3)
            {
                if (DebugLoggingEnabled) Debug.LogWarning("Vertices rejected - insufficient vertices");

                return false;
            }

            if (area < 0.0001f)
            {
                if (DebugLoggingEnabled) Debug.LogWarning("Vertices rejected - below minimum area");

                return false;
            }

            if (failOnConcave && !IsConvex(ref vertices))
            {
                if (DebugLoggingEnabled) Debug.LogWarning("Vertices rejected - shape is not convex");

                return false;
            }

            return true;
        }

        private static void SliceConcave(Polygon poly, SpriteSlicerLine line)
        {
            SplitEdges(poly, line);
            SortEdges(line);
            SplitPolygon();
            CollectPolys();
        }

        private static Vector2 LineIntersectionPoint(Vector2 ps1, Vector2 pe1, Vector2 ps2, Vector2 pe2)
        {
            var a1 = pe1.y - ps1.y;
            var b1 = ps1.x - pe1.x;
            var c1 = a1 * ps1.x + b1 * ps1.y;
            var a2 = pe2.y - ps2.y;
            var b2 = ps2.x - pe2.x;
            var c2 = a2 * ps2.x + b2 * ps2.y;
            var delta = a1 * b2 - a2 * b1;

            if (delta == 0) Debug.LogError("Lines are parallel!");

            var inverseDelta = 1.0f / delta;
            return new Vector2((b2 * c1 - b1 * c2) * inverseDelta, (a1 * c2 - a2 * c1) * inverseDelta);
        }

        /// <summary>
        ///     Generate polygon edge points based on the slice line
        /// </summary>
        private static void SplitEdges(Polygon poly, SpriteSlicerLine line)
        {
            SConcavePolygonPoints.Clear();
            SConcavePolygonIntersectionPoints.Clear();

            for (var i = 0; i < poly.Points.Count; i++)
            {
                var edge = new SpriteSlicerLine(poly.Points[i], poly.Points[(i + 1) % poly.Points.Count]);
                var edgeStartSide = GetSideOfLine(line, edge.P1);
                var edgeEndSide = GetSideOfLine(line, edge.P2);
                SConcavePolygonPoints.Add(new LinkedPolygonPoint(poly.Points[i], edgeStartSide));

                if (edgeStartSide == LineSide.On)
                {
                    SConcavePolygonIntersectionPoints.Add(SConcavePolygonPoints[SConcavePolygonPoints.Count - 1]);
                }
                else if (edgeStartSide != edgeEndSide && edgeEndSide != LineSide.On)
                {
                    var ip = LineIntersectionPoint(edge.P1, edge.P2, line.P1, line.P2);
                    SConcavePolygonPoints.Add(new LinkedPolygonPoint(ip, LineSide.On));
                    SConcavePolygonIntersectionPoints.Add(SConcavePolygonPoints[SConcavePolygonPoints.Count - 1]);
                }
            }

            for (var loop = 0; loop < SConcavePolygonPoints.Count - 1; loop++)
            {
                var nextIndex = (loop + 1) % SConcavePolygonPoints.Count;
                var current = SConcavePolygonPoints[loop];
                var next = SConcavePolygonPoints[nextIndex];
                current.Next = next;
                next.Prev = current;
            }

            SConcavePolygonPoints[SConcavePolygonPoints.Count - 1].Next = SConcavePolygonPoints[0];
            SConcavePolygonPoints[0].Prev = SConcavePolygonPoints[SConcavePolygonPoints.Count - 1];
        }

        private static void SortEdges(SpriteSlicerLine line)
        {
            var edgeComparer = new EdgeComparer(line);
            SConcavePolygonIntersectionPoints.Sort(edgeComparer);

            for (var i = 1; i < SConcavePolygonIntersectionPoints.Count; i++)
                SConcavePolygonIntersectionPoints[i].DistOnLine = (SConcavePolygonIntersectionPoints[i].Position -
                                                                   SConcavePolygonIntersectionPoints[0].Position)
                    .magnitude;
        }

        /// <summary>
        ///     Splits the polygon along the cut line
        /// </summary>
        private static void SplitPolygon()
        {
            LinkedPolygonPoint useSrc = null;

            for (var i = 0; i < SConcavePolygonIntersectionPoints.Count; i++)
            {
                var srcEdge = useSrc;
                useSrc = null;

                for (; srcEdge == null && i < SConcavePolygonIntersectionPoints.Count; i++)
                {
                    var curEdge = SConcavePolygonIntersectionPoints[i];
                    var curSide = curEdge.SliceSide;
                    var prevSide = curEdge.Prev.SliceSide;
                    var nextSide = curEdge.Next.SliceSide;

                    if (curSide != LineSide.On) Debug.LogError("Current side should be ON");

                    if ((prevSide == LineSide.Left && nextSide == LineSide.Right) ||
                        (prevSide == LineSide.Left && nextSide == LineSide.On &&
                         curEdge.Next.DistOnLine < curEdge.DistOnLine) ||
                        (prevSide == LineSide.On && nextSide == LineSide.Right &&
                         curEdge.Prev.DistOnLine < curEdge.DistOnLine))
                        srcEdge = curEdge;
                }

                // find destination
                LinkedPolygonPoint dstEdge = null;

                for (; dstEdge == null && i < SConcavePolygonIntersectionPoints.Count;)
                {
                    var curEdge = SConcavePolygonIntersectionPoints[i];
                    var curSide = curEdge.SliceSide;
                    var prevSide = curEdge.Prev.SliceSide;
                    var nextSide = curEdge.Next.SliceSide;

                    if (curSide != LineSide.On) Debug.LogError("Current side should be ON");

                    if ((prevSide == LineSide.Right && nextSide == LineSide.Left) ||
                        (prevSide == LineSide.On && nextSide == LineSide.Left) ||
                        (prevSide == LineSide.Right && nextSide == LineSide.On) ||
                        (prevSide == LineSide.Right && nextSide == LineSide.Right) ||
                        (prevSide == LineSide.Left && nextSide == LineSide.Left))
                        dstEdge = curEdge;
                    else
                        i++;
                }

                if (srcEdge != null && dstEdge != null)
                {
                    LinkPoints(srcEdge, dstEdge);
                    VerifyPolygons();

                    if (srcEdge.Prev.Prev.SliceSide == LineSide.Left)
                        useSrc = srcEdge.Prev;
                    else if (dstEdge.Next.SliceSide == LineSide.Right) useSrc = dstEdge;
                }
            }
        }

        /// <summary>
        ///     Sort polygon points into their appropriate polygons
        /// </summary>
        private static void CollectPolys()
        {
            SConcaveSlicePolygonResults.Clear();

            foreach (var e in SConcavePolygonPoints)
                if (!e.Visited)
                {
                    var splitPoly = new Polygon();
                    var curSide = e;

                    do
                    {
                        curSide.Visited = true;
                        splitPoly.Points.Add(curSide.Position);
                        curSide = curSide.Next;
                    } while (curSide != e);

                    SConcaveSlicePolygonResults.Add(splitPoly);
                }
        }

        /// <summary>
        ///     Verifies the polygons are correct and correctly wrap around
        /// </summary>
        private static void VerifyPolygons()
        {
            foreach (var edge in SConcavePolygonPoints)
            {
                var curSide = edge;
                var count = 0;

                do
                {
                    if (count >= SConcavePolygonPoints.Count)
                    {
                        Debug.LogError("Invalid polygon cycle detected");
                        break;
                    }

                    curSide = curSide.Next;
                    count++;
                } while (curSide != edge);
            }
        }

        /// <summary>
        ///     Link two points on the polygon by creating a link between them in each direction
        /// </summary>
        private static void LinkPoints(LinkedPolygonPoint srcEdge, LinkedPolygonPoint dstEdge)
        {
            SConcavePolygonPoints.Add(new LinkedPolygonPoint(srcEdge.Position, srcEdge.SliceSide));
            var a = SConcavePolygonPoints[SConcavePolygonPoints.Count - 1];
            SConcavePolygonPoints.Add(new LinkedPolygonPoint(dstEdge.Position, dstEdge.SliceSide));
            var b = SConcavePolygonPoints[SConcavePolygonPoints.Count - 1];
            a.Next = dstEdge;
            a.Prev = srcEdge.Prev;
            b.Next = srcEdge;
            b.Prev = dstEdge.Prev;
            srcEdge.Prev.Next = a;
            srcEdge.Prev = b;
            dstEdge.Prev.Next = b;
            dstEdge.Prev = a;
        }

        /// <summary>
        ///     Use the polygon winding algorithm to check whether a point is inside the given polygon
        /// </summary>
        private static bool IsPointInsidePolygon(Vector2 pos, ref List<Vector2> polygonPoints)
        {
            var winding = 0;
            var numPoints = polygonPoints.Count;

            for (var vertexIndex = 0; vertexIndex < numPoints; vertexIndex++)
            {
                var nextIndex = vertexIndex + 1;

                if (nextIndex >= numPoints) nextIndex = 0;

                var thisPoint = polygonPoints[vertexIndex];
                var nextPoint = polygonPoints[nextIndex];

                if (thisPoint.y <= pos.y)
                {
                    if (nextPoint.y > pos.y)
                    {
                        var isLeft = (nextPoint.x - thisPoint.x) * (pos.y - thisPoint.y) -
                                     (pos.x - thisPoint.x) * (nextPoint.y - thisPoint.y);

                        if (isLeft > 0) winding++;
                    }
                }
                else
                {
                    if (nextPoint.y <= pos.y)
                    {
                        var isLeft = (nextPoint.x - thisPoint.x) * (pos.y - thisPoint.y) -
                                     (pos.x - thisPoint.x) * (nextPoint.y - thisPoint.y);

                        if (isLeft < 0) winding--;
                    }
                }
            }

            return winding != 0;
        }

        #endregion
    }

#if UNITY_EDITOR
    public static class SpriteSlicerConvexHelper
    {
        [MenuItem("Tools/Sprite Slicer 2D/Add Sliced Sprite Component")]
        private static void AddSlicedSpriteComponent()
        {
            for (var loop = 0; loop < Selection.gameObjects.Length; loop++)
                Selection.gameObjects[loop].AddComponent<SlicedSprite>();
        }

        [MenuItem("Tools/Sprite Slicer 2D/Make Convex")]
        private static void MakeConvex()
        {
            for (var loop = 0; loop < Selection.gameObjects.Length; loop++)
            {
                var polyCollider = Selection.gameObjects[loop].GetComponent<PolygonCollider2D>();

                if (polyCollider)
                {
                    var vertices = new List<Vector2>(polyCollider.points);
                    var originalNumVertices = vertices.Count;
                    var iterations = 0;

                    if (SpriteSlicer2D.IsConvex(ref vertices))
                        Debug.Log(Selection.gameObjects[loop].name + " is already convex - no work to do");
                    else
                        do
                        {
                            float determinant;
                            Vector3 v1 = vertices[0] - vertices[vertices.Count - 1];
                            Vector3 v2 = vertices[1] - vertices[0];
                            var referenceDeterminant = SpriteSlicer2D.CalculateDeterminant2X2(v1, v2);

                            for (var i = 1; i < vertices.Count - 1;)
                            {
                                v1 = v2;
                                v2 = vertices[i + 1] - vertices[i];
                                determinant = SpriteSlicer2D.CalculateDeterminant2X2(v1, v2);

                                if (referenceDeterminant * determinant < 0.0f)
                                    vertices.RemoveAt(i);
                                else
                                    i++;
                            }

                            v1 = v2;
                            v2 = vertices[0] - vertices[vertices.Count - 1];
                            determinant = SpriteSlicer2D.CalculateDeterminant2X2(v1, v2);

                            if (referenceDeterminant * determinant < 0.0f) vertices.RemoveAt(vertices.Count - 1);

                            iterations++;
                        } while (!SpriteSlicer2D.IsConvex(ref vertices) && iterations < 25);

                    if (SpriteSlicer2D.IsConvex(ref vertices))
                    {
                        polyCollider.SetPath(0, vertices.ToArray());
                        Debug.Log(Selection.gameObjects[loop].name + " points reduced to " + vertices.Count + " from " +
                                  originalNumVertices);
                    }
                    else
                    {
                        Debug.Log(Selection.gameObjects[loop].name +
                                  " could not be made convex, please adjust shape manually");
                    }
                }
            }
        }
    }
#endif

    /// <summary>
    ///     Simple class that takes a sprite and a polygon collider, and creates
    ///     a render mesh that exactly fits the collider.
    /// </summary>
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    [RequireComponent(typeof(Rigidbody2D), typeof(PolygonCollider2D))]
    public class SlicedSprite : MonoBehaviour
    {
        public MeshRenderer MeshRenderer { get; private set; }

        public Vector2 MinCoords { get; private set; }

        public Vector2 MaxCoords { get; private set; }

        public Bounds SpriteBounds { get; private set; }

        public int ParentInstanceID { get; private set; }

        public int CutsSinceParentObject { get; private set; }

        public int ExplosionsSinceParentObject { get; set; }

        public bool Rotated { get; private set; }

        public bool HFlipped { get; private set; }

        public bool VFlipped { get; private set; }

        private MeshFilter _mMeshFilter;
        private Transform _mTransform;

        private Vector2 _mCentroid;
        private Vector2 _mUVOffset;
        private Color32 _mColor;

        /// <summary>
        ///     Called when the object is created
        /// </summary>
        private void Awake()
        {
            _mTransform = transform;
            _mMeshFilter = GetComponent<MeshFilter>();
            MeshRenderer = GetComponent<MeshRenderer>();
            ParentInstanceID = gameObject.GetInstanceID();
            MinCoords = new Vector2(0.0f, 0.0f);
            MaxCoords = new Vector2(1.0f, 1.0f);

            if (_mMeshFilter.mesh) SpriteBounds = _mMeshFilter.mesh.bounds;
        }

#if TK2D_SLICING_ENABLED
	/// <summary>
	/// Initialise the sliced sprite using an existing tk2dsprite
	/// </summary>
	public void InitFromTK2DSprite(tk2dSprite sprite, ref PolygonCollider2D polygon, ref Vector2[] polygonPoints, bool isConcave)
	{
        MeshRenderer meshRenderer = sprite.GetComponent<MeshRenderer>();
        Bounds bounds = meshRenderer.bounds;
        Vector2 position = sprite.transform.position;
        Vector2 min = bounds.min;
        Vector2 max = bounds.max;
        Vector2 size = bounds.size;

        Vector3 lossyScale = sprite.transform.lossyScale;
        Vector2 offsetOfAbsolutePositionRelativelyToMinOfBounds = Vector2.zero;

        // Adjust pivot position calculation if sprite has been flipped on the x axis
        if(Mathf.Sign(lossyScale.x) < 0.0f)
        {
            offsetOfAbsolutePositionRelativelyToMinOfBounds.x = max.x - position.x;
        }
        else 
        {
            offsetOfAbsolutePositionRelativelyToMinOfBounds.x = position.x - min.x;
        }
        
        // Adjust pivot position calculation if sprite has been flipped on the y axis
        if(Mathf.Sign(lossyScale.y) < 0.0f)
        {
            offsetOfAbsolutePositionRelativelyToMinOfBounds.y = max.y - position.y;
        }
        else
        {
            offsetOfAbsolutePositionRelativelyToMinOfBounds.y = position.y - min.y;
        }
        
        Vector2 pivotVector = new Vector2(
            offsetOfAbsolutePositionRelativelyToMinOfBounds.x / size.x,
            offsetOfAbsolutePositionRelativelyToMinOfBounds.y / size.y
            );
        
        pivotVector -= new Vector2(0.5f, 0.5f);

		tk2dSpriteDefinition spriteDefinition = sprite.GetCurrentSpriteDef();
		bool isRotated = spriteDefinition.flipped != tk2dSpriteDefinition.FlipMode.None;
		bool isHFlipped = sprite.FlipX;
		bool isVFlipped = sprite.FlipY;
        InitSprite(sprite.gameObject, sprite.GetComponent<MeshRenderer>(), ref polygon, ref polygonPoints, spriteDefinition.uvs[0], spriteDefinition.uvs[spriteDefinition.uvs.Length - 1], spriteDefinition.GetBounds(), meshRenderer.sharedMaterial, isRotated, isHFlipped, isVFlipped, Vector2.zero, pivotVector, isConcave, Color.white);
		m_ParentInstanceID = sprite.gameObject.GetInstanceID();
	}
#endif

        /// <summary>
        ///     Initialise this sliced sprite using an existing SlicedSprite
        /// </summary>
        public void InitFromSlicedSprite(SlicedSprite slicedSprite, ref PolygonCollider2D polygon,
            ref Vector2[] polygonPoints, bool isConcave)
        {
            var block = new MaterialPropertyBlock();
            slicedSprite.MeshRenderer.GetPropertyBlock(block);
            MeshRenderer.SetPropertyBlock(block);

            InitSprite(slicedSprite.gameObject, slicedSprite.MeshRenderer, ref polygon, ref polygonPoints,
                slicedSprite.MinCoords, slicedSprite.MaxCoords, slicedSprite.SpriteBounds,
                slicedSprite.MeshRenderer.sharedMaterial, slicedSprite.Rotated, slicedSprite.HFlipped,
                slicedSprite.VFlipped, slicedSprite._mCentroid, slicedSprite._mUVOffset, isConcave,
                slicedSprite._mColor);
            ParentInstanceID = slicedSprite.GetInstanceID();
            CutsSinceParentObject = slicedSprite.CutsSinceParentObject + 1;
        }

        /// <summary>
        ///     Initialise using a unity sprite
        /// </summary>
        public void InitFromUnitySprite(SpriteRenderer unitySprite, ref PolygonCollider2D polygon,
            ref Vector2[] polygonPoints, bool isConcave, Color32 spriteColor)
        {
            var sprite = unitySprite.sprite;

            var bounds = unitySprite.bounds;
            Vector2 position = unitySprite.transform.position;
            Vector2 min = bounds.min;
            Vector2 max = bounds.max;
            Vector2 size = bounds.size;
            var offsetOfAbsolutePositionRelativelyToMinOfBounds = Vector2.zero;
            var lossyScale = unitySprite.transform.lossyScale;

            // Adjust pivot position calculation if sprite has been flipped on the x axis
            if (Mathf.Sign(lossyScale.x) < 0.0f)
                offsetOfAbsolutePositionRelativelyToMinOfBounds.x = max.x - position.x;
            else
                offsetOfAbsolutePositionRelativelyToMinOfBounds.x = position.x - min.x;

            // Adjust pivot position calculation if sprite has been flipped on the y axis
            if (Mathf.Sign(lossyScale.y) < 0.0f)
                offsetOfAbsolutePositionRelativelyToMinOfBounds.y = max.y - position.y;
            else
                offsetOfAbsolutePositionRelativelyToMinOfBounds.y = position.y - min.y;

            var pivotVector = new Vector2(
                offsetOfAbsolutePositionRelativelyToMinOfBounds.x / size.x,
                offsetOfAbsolutePositionRelativelyToMinOfBounds.y / size.y
            );

            pivotVector -= new Vector2(0.5f, 0.5f);

            var spriteTexture = sprite.texture;
            var textureSize = new Vector2(spriteTexture.width, spriteTexture.height);

            var material = unitySprite.sharedMaterial;
            var block = new MaterialPropertyBlock();
            block.SetTexture("_MainTex", spriteTexture);
            MeshRenderer.SetPropertyBlock(block);

            var textureRect = sprite.textureRect;
            var minTextureCoords = new Vector2(textureRect.xMin / textureSize.x, textureRect.yMin / textureSize.y);
            var maxTextureCoords = new Vector2(textureRect.xMax / textureSize.x, textureRect.yMax / textureSize.y);

            InitSprite(unitySprite.gameObject, unitySprite.GetComponent<Renderer>(), ref polygon, ref polygonPoints,
                minTextureCoords, maxTextureCoords, unitySprite.sprite.bounds, material, false, false, false,
                Vector2.zero,
                pivotVector, isConcave, spriteColor);
            ParentInstanceID = unitySprite.gameObject.GetInstanceID();
        }

        /// <summary>
        ///     Initialise this sprite using the given polygon definition
        /// </summary>
        private void InitSprite(GameObject parentObject, Renderer parentRenderer, ref PolygonCollider2D polygon,
            ref Vector2[] polygonPoints, Vector3 minCoords, Vector3 maxCoords, Bounds spriteBounds, Material material,
            bool rotated, bool hFlipped, bool vFlipped, Vector2 parentCentroid, Vector2 uvOffset, bool isConcave,
            Color32 spriteColor)
        {
            MinCoords = minCoords;
            MaxCoords = maxCoords;
            SpriteBounds = spriteBounds;
            VFlipped = vFlipped;
            HFlipped = hFlipped;
            Rotated = rotated;
            SpriteBounds = spriteBounds;
            _mUVOffset = uvOffset;
            _mColor = spriteColor;

            gameObject.tag = parentObject.tag;
            gameObject.layer = parentObject.layer;

            var spriteMesh = new Mesh();
            spriteMesh.name = "SlicedSpriteMesh";
            _mMeshFilter.mesh = spriteMesh;

            var numVertices = polygonPoints.Length;
            var vertices = new Vector3[numVertices];
            var colors = new Color[numVertices];
            var uvs = new Vector2[numVertices];
            int[] triangles;

            // Convert vector2 -> vector3
            for (var loop = 0; loop < vertices.Length; loop++)
            {
                vertices[loop] = polygonPoints[loop];
                colors[loop] = spriteColor;
            }

            Vector2 uvWidth = maxCoords - minCoords;
            var boundsSize = spriteBounds.size;
            var invBoundsSize = new Vector2(1.0f / boundsSize.x, 1.0f / boundsSize.y);

            for (var vertexIndex = 0; vertexIndex < numVertices; vertexIndex++)
            {
                var vertex = polygonPoints[vertexIndex] + parentCentroid;
                var widthFraction = 0.5f + (vertex.x * invBoundsSize.x + uvOffset.x);
                var heightFraction = 0.5f + (vertex.y * invBoundsSize.y + uvOffset.y);

                if (hFlipped) widthFraction = 1.0f - widthFraction;

                if (vFlipped) heightFraction = 1.0f - heightFraction;

                var texCoords = new Vector2();

                if (rotated)
                {
                    texCoords.y = maxCoords.y - uvWidth.y * (1.0f - widthFraction);
                    texCoords.x = minCoords.x + uvWidth.x * heightFraction;
                }
                else
                {
                    texCoords.x = minCoords.x + uvWidth.x * widthFraction;
                    texCoords.y = minCoords.y + uvWidth.y * heightFraction;
                }

                uvs[vertexIndex] = texCoords;
            }

            if (isConcave)
            {
                var polyPointList = new List<Vector2>(polygonPoints);
                triangles = SpriteSlicer2D.Triangulate(ref polyPointList);
            }
            else

            {
                var triangleIndex = 0;
                triangles = new int[numVertices * 3];

                for (var vertexIndex = 1; vertexIndex < numVertices - 1; vertexIndex++)
                {
                    triangles[triangleIndex++] = 0;
                    triangles[triangleIndex++] = vertexIndex + 1;
                    triangles[triangleIndex++] = vertexIndex;
                }
            }

            spriteMesh.Clear();
            spriteMesh.vertices = vertices;
            spriteMesh.uv = uvs;
            spriteMesh.triangles = triangles;
            spriteMesh.colors = colors;
            spriteMesh.RecalculateBounds();
            spriteMesh.RecalculateNormals();

            Vector2 localCentroid = Vector3.zero;

            if (SpriteSlicer2D.SCentreChildSprites)
            {
                localCentroid = spriteMesh.bounds.center;

                // Finally, fix up our mesh, collider, and object position to at the same position as the pivot point
                for (var vertexIndex = 0; vertexIndex < numVertices; vertexIndex++)
                    vertices[vertexIndex] -= (Vector3)localCentroid;

                for (var vertexIndex = 0; vertexIndex < numVertices; vertexIndex++)
                    polygonPoints[vertexIndex] -= localCentroid;

                _mCentroid = localCentroid + parentCentroid;
                polygon.SetPath(0, polygonPoints);
                spriteMesh.vertices = vertices;
                spriteMesh.RecalculateBounds();
            }

            var parentTransform = parentObject.transform;
            _mTransform.parent = parentTransform.parent;
            _mTransform.position = parentTransform.position + parentTransform.rotation * localCentroid;
            _mTransform.rotation = parentTransform.rotation;
            _mTransform.localScale = parentTransform.localScale;
            MeshRenderer.material = material;

            MeshRenderer.sortingLayerID = parentRenderer.sortingLayerID;
            MeshRenderer.sortingOrder = parentRenderer.sortingOrder;
        }
    }
}