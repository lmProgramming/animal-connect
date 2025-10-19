using System.Text;
using Core.Configuration;
using Managers;
using UnityEngine;

namespace DebugTools
{
    /// <summary>
    ///     Debug visualizer for path points showing connections with colors and numbers.
    ///     Can be toggled on/off during Unity playback.
    /// </summary>
    public class PathPointDebugVisualizer : MonoBehaviour
    {
        [Header("Debug Settings")]
        [SerializeField] private bool showDebug = true;

        [Header("Path Points (Manually Place 24 Points)")]
        [SerializeField] private Transform[] pathPointTransforms = new Transform[24];

        [Header("Visual Settings")]
        [SerializeField] private float sphereRadius = 0.2f;

        [SerializeField] private float lineWidth = 0.05f;
        [SerializeField] private float numberOffset = 0.5f;

        [Header("Colors")]
        [SerializeField] private Color entityPointColor = Color.yellow;

        [SerializeField] private Color nonEntityPointColor = Color.cyan;
        [SerializeField] private Color connectedLineColor = Color.green;
        [SerializeField] private Color textColor = Color.white;
        private GUIStyle _labelStyle;

        private GameStateManager _stateManager;

        private void Awake()
        {
            // Setup GUI style for labels
            _labelStyle = new GUIStyle
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
        }

        private void Start()
        {
            // Try to find GameStateManager
            _stateManager = FindObjectOfType<GameStateManager>();
            if (_stateManager == null) Debug.LogWarning("PathPointDebugVisualizer: GameStateManager not found!");
        }

        private void Update()
        {
            // Toggle debug with F3 key during gameplay
            if (Input.GetKeyDown(KeyCode.F3)) ToggleDebug();
        }

        private void OnGUI()
        {
            if (!showDebug) return;

            DrawPathPointNumbers();
        }

        private void OnDrawGizmos()
        {
            if (!showDebug) return;

            DrawPathPoints();
            DrawConnections();
        }

        /// <summary>
        ///     Draw spheres at each path point with different colors for entity/non-entity points
        /// </summary>
        private void DrawPathPoints()
        {
            for (var i = 0; i < GridConfiguration.TotalPathPoints; i++)
            {
                var pathPoint = pathPointTransforms[i];
                if (pathPoint == null) continue;

                // Get color based on hash of path index
                var pathIndex = GetPathIndexForPoint(i);
                Gizmos.color = GetColorFromHash(pathIndex);

                // Draw sphere at path point
                Gizmos.DrawSphere(pathPoint.position, sphereRadius);

                // Draw wire sphere outline
                Gizmos.color = Color.white;
                Gizmos.DrawWireSphere(pathPoint.position, sphereRadius);
            }
        }

        /// <summary>
        ///     Draw connections between path points based on current game state
        /// </summary>
        private void DrawConnections()
        {
            if (_stateManager == null) return;

            var currentState = _stateManager.CurrentState;
            if (currentState == null) return;

            var pathNetwork = currentState.Paths;
            if (pathNetwork == null) return;

            // Draw connections between path points that are in the same network
            for (var i = 0; i < GridConfiguration.TotalPathPoints; i++)
            {
                var pointA = pathPointTransforms[i];
                if (pointA == null) continue;

                for (var j = i + 1; j < GridConfiguration.TotalPathPoints; j++)
                {
                    var pointB = pathPointTransforms[j];
                    if (pointB == null) continue;

                    // Check if these points are connected
                    if (pathNetwork.AreConnected(i, j))
                    {
                        // Draw line between connected points
                        Gizmos.color = connectedLineColor;
                        Gizmos.DrawLine(pointA.position, pointB.position);

                        // Draw thicker line by drawing multiple slightly offset lines
                        var perpendicular = Vector3.Cross(
                            (pointB.position - pointA.position).normalized,
                            Vector3.forward
                        ).normalized * lineWidth;

                        Gizmos.DrawLine(
                            pointA.position + perpendicular,
                            pointB.position + perpendicular
                        );
                        Gizmos.DrawLine(
                            pointA.position - perpendicular,
                            pointB.position - perpendicular
                        );
                    }
                }
            }
        }

        /// <summary>
        ///     Draw path point numbers using GUI labels
        /// </summary>
        private void DrawPathPointNumbers()
        {
            var cam = Camera.main;
            if (cam == null) return;

            _labelStyle.normal.textColor = textColor;

            for (var i = 0; i < GridConfiguration.TotalPathPoints; i++)
            {
                var pathPoint = pathPointTransforms[i];
                if (pathPoint == null) continue;

                // Convert world position to screen position
                var worldPos = pathPoint.position + Vector3.up * numberOffset;
                var screenPos = cam.WorldToScreenPoint(worldPos);

                // Only draw if in front of camera
                if (screenPos.z > 0)
                {
                    // Flip Y coordinate for GUI
                    screenPos.y = Screen.height - screenPos.y;

                    // Draw background rectangle
                    var backgroundRect = new Rect(screenPos.x - 20, screenPos.y - 12, 40, 24);
                    GUI.color = new Color(0, 0, 0, 0.7f);
                    GUI.Box(backgroundRect, "");

                    // Draw number
                    GUI.color = Color.white;
                    var labelRect = new Rect(screenPos.x - 20, screenPos.y - 12, 40, 24);
                    GUI.Label(labelRect, i.ToString(), _labelStyle);

                    // Draw entity indicator if this is an entity point
                    if (GridConfiguration.IsEntityPoint(i))
                    {
                        var entityIndex = GridConfiguration.GetEntityAtPoint(i);
                        var entityRect = new Rect(screenPos.x - 25, screenPos.y + 12, 50, 20);
                        GUI.color = new Color(1, 1, 0, 0.8f);
                        GUI.Label(entityRect, $"E{entityIndex}", _labelStyle);
                    }
                }
            }

            GUI.color = Color.white;
        }

        /// <summary>
        ///     Toggle debug visualization on/off (can be called from UI or inspector)
        /// </summary>
        public void ToggleDebug()
        {
            showDebug = !showDebug;
            Debug.Log($"Path Point Debug: {(showDebug ? "ON" : "OFF")}");
        }

        /// <summary>
        ///     Set debug visualization state
        /// </summary>
        public void SetDebug(bool enabled)
        {
            showDebug = enabled;
        }

        /// <summary>
        ///     Get information about a specific path point
        /// </summary>
        public string GetPathPointInfo(int pathPointIndex)
        {
            if (pathPointIndex < 0 || pathPointIndex >= GridConfiguration.TotalPathPoints)
                return "Invalid path point index";

            var info = $"Path Point {pathPointIndex}:\n";

            if (GridConfiguration.IsEntityPoint(pathPointIndex))
            {
                var entityIndex = GridConfiguration.GetEntityAtPoint(pathPointIndex);
                info += $"  Entity: E{entityIndex}\n";
            }
            else
            {
                info += "  No entity\n";
            }

            if (_stateManager != null && _stateManager.CurrentState != null)
            {
                var network = _stateManager.CurrentState.Paths;
                if (network != null)
                {
                    info += $"  Network Root: {pathPointIndex}\n";

                    // Find connected points
                    var connected = new StringBuilder("  Connected to: ");
                    var hasConnections = false;
                    for (var i = 0; i < GridConfiguration.TotalPathPoints; i++)
                        if (i != pathPointIndex && network.AreConnected(pathPointIndex, i))
                        {
                            if (hasConnections) connected.Append(", ");
                            connected.Append(i);
                            hasConnections = true;
                        }

                    if (!hasConnections) connected.Append("None");
                    info += connected.ToString();
                }
            }

            return info;
        }

        /// <summary>
        ///     Log all path point connections to console
        /// </summary>
        [ContextMenu("Log All Path Point Connections")]
        public void LogAllConnections()
        {
            if (_stateManager == null || _stateManager.CurrentState == null)
            {
                Debug.LogWarning("No game state available");
                return;
            }

            var network = _stateManager.CurrentState.Paths;
            if (network == null)
            {
                Debug.LogWarning("No path network available");
                return;
            }

            Debug.Log("===== PATH POINT CONNECTIONS =====");
            for (var i = 0; i < GridConfiguration.TotalPathPoints; i++) Debug.Log(GetPathPointInfo(i));
            Debug.Log("==================================");
        }

        /// <summary>
        ///     Get the path index (network root) for a given path point
        /// </summary>
        private int GetPathIndexForPoint(int pathPointIndex)
        {
            if (_stateManager == null || _stateManager.CurrentState == null)
                return pathPointIndex;

            var pathNetwork = _stateManager.CurrentState.Paths;
            if (pathNetwork == null)
                return pathPointIndex;

            // Find the root of this path point's network
            return pathNetwork.GetPathId(pathPointIndex);
        }

        /// <summary>
        ///     Generate a deterministic color from a hash value
        /// </summary>
        private Color GetColorFromHash(int hash)
        {
            // Use a simple hash function to generate RGB values
            var h = hash * 2654435761; // Large prime for better distribution

            var r = ((h & 0xFF0000) >> 16) / 255f;
            var g = ((h & 0x00FF00) >> 8) / 255f;
            var b = (h & 0x0000FF) / 255f;

            // Ensure minimum brightness for visibility
            var brightness = (r + g + b) / 3f;
            if (brightness < 0.3f)
            {
                var boost = 0.3f - brightness;
                r += boost;
                g += boost;
                b += boost;
            }

            return new Color(r, g, b, 1f);
        }
    }
}