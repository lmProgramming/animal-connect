using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Core.DataStructures;

namespace Core.Models
{
    /// <summary>
    ///     Represents the state of all path connections in the game.
    ///     Uses Union-Find for efficient path merging and querying.
    ///     Each of the 24 path points can be connected to others via tiles.
    /// </summary>
    [Serializable]
    public class PathNetworkState
    {
        public const int StandardPathPointCount = 24; // 12 entities + 12 additional edge points
        private readonly int[] _connectionCounts; // How many tile sides connect to each path point
        private readonly UnionFind _connections;
        private readonly int _totalPathPoints;

        public PathNetworkState(int pathPointCount = StandardPathPointCount)
        {
            _totalPathPoints = pathPointCount;
            _connections = new UnionFind(pathPointCount);
            _connectionCounts = new int[pathPointCount];
        }

        /// <summary>
        ///     Resets all path connections.
        /// </summary>
        public void Reset()
        {
            _connections.Reset();
            Array.Clear(_connectionCounts, 0, _connectionCounts.Length);
        }

        /// <summary>
        ///     Gets the path ID (root) for a given path point.
        ///     All points with the same path ID are connected.
        /// </summary>
        public int GetPathId(int pathPoint)
        {
            ValidatePathPoint(pathPoint);
            return _connections.Find(pathPoint);
        }

        /// <summary>
        ///     Checks if two path points are connected.
        /// </summary>
        public bool AreConnected(int pathPoint1, int pathPoint2)
        {
            ValidatePathPoint(pathPoint1);
            ValidatePathPoint(pathPoint2);
            return _connections.Connected(pathPoint1, pathPoint2);
        }

        /// <summary>
        ///     Connects multiple path points together (they all share the same path).
        /// </summary>
        public void ConnectPoints(IEnumerable<int> pathPoints)
        {
            var points = pathPoints.ToArray();
            if (points.Length < 2)
                return; // Nothing to connect

            // Increment connection count for each point
            foreach (var point in points)
            {
                ValidatePathPoint(point);
                _connectionCounts[point]++;
            }

            // Connect all points to the first point
            for (var i = 1; i < points.Length; i++) _connections.Union(points[0], points[i]);
        }

        /// <summary>
        ///     Connects two path points together.
        /// </summary>
        public void ConnectPoints(int pathPoint1, int pathPoint2)
        {
            ValidatePathPoint(pathPoint1);
            ValidatePathPoint(pathPoint2);

            _connectionCounts[pathPoint1]++;
            _connectionCounts[pathPoint2]++;

            _connections.Union(pathPoint1, pathPoint2);
        }

        /// <summary>
        ///     Gets all path points that share the same path as the given point.
        /// </summary>
        public IEnumerable<int> GetPointsInPath(int pathPoint)
        {
            ValidatePathPoint(pathPoint);
            return _connections.GetSet(pathPoint);
        }

        /// <summary>
        ///     Gets the number of tile connections to a path point.
        ///     Valid counts are:
        ///     - 0: No connections (valid for non-entity points at edges)
        ///     - 1: One connection (required for entity points)
        ///     - 2: Two connections (valid for non-entity points)
        ///     - 3+: Invalid (creates branch point)
        /// </summary>
        public int GetConnectionCount(int pathPoint)
        {
            ValidatePathPoint(pathPoint);
            return _connectionCounts[pathPoint];
        }

        /// <summary>
        ///     Gets the number of distinct paths (connected components).
        /// </summary>
        public int GetPathCount()
        {
            return _connections.CountSets();
        }

        /// <summary>
        ///     Creates a copy of this PathNetworkState.
        /// </summary>
        public PathNetworkState Clone()
        {
            var clone = new PathNetworkState(_totalPathPoints);
            Array.Copy(_connectionCounts, clone._connectionCounts, _totalPathPoints);

            // Reconstruct the union-find structure
            for (var i = 0; i < _totalPathPoints; i++)
            for (var j = i + 1; j < _totalPathPoints; j++)
                if (AreConnected(i, j))
                    clone._connections.Union(i, j);

            return clone;
        }

        private void ValidatePathPoint(int pathPoint)
        {
            if (pathPoint < 0 || pathPoint >= _totalPathPoints)
                throw new ArgumentOutOfRangeException(nameof(pathPoint),
                    $"Path point must be between 0 and {_totalPathPoints - 1}");
        }

        public override string ToString()
        {
            var result = new StringBuilder();
            result.AppendLine($"PathNetworkState ({GetPathCount()} distinct paths):");

            for (var i = 0; i < _totalPathPoints; i++)
                result.AppendLine($"  Point {i}: Path {GetPathId(i)}, Connections: {GetConnectionCount(i)}");

            return result.ToString();
        }
    }
}