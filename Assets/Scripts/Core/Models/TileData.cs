using System;
using System.Collections.Generic;
using System.Linq;

namespace Core.Models
{
    /// <summary>
    ///     Immutable representation of a tile's type and rotation.
    ///     No Unity dependencies - pure data structure.
    /// </summary>
    [Serializable]
    public struct TileData : IEquatable<TileData>
    {
        public TileType Type { get; }
        public int Rotation { get; } // 0-3 for most tiles, limited for some types

        public TileData(TileType type, int rotation = 0)
        {
            Type = type;
            Rotation = rotation;
        }

        /// <summary>
        ///     Creates a new TileData with the rotation incremented.
        /// </summary>
        public TileData WithRotation(int rotation)
        {
            return new TileData(Type, rotation);
        }

        /// <summary>
        ///     Gets the connection pattern for this tile in its current rotation.
        ///     Each connection is a list of sides (0=top, 1=right, 2=bottom, 3=left) that connect together.
        /// </summary>
        public IReadOnlyList<Connection> GetConnections()
        {
            var baseConnections = GetBaseConnections();
            var rotation = Rotation; // Copy to local variable to avoid struct 'this' capture
            return baseConnections.Select(c => c.WithRotation(rotation)).ToList();
        }

        private IReadOnlyList<Connection> GetBaseConnections()
        {
            return Type switch
            {
                TileType.Curve => new[] { new Connection(1, 2) }, // Right to Bottom
                TileType.TwoCurves => new[] { new Connection(0, 3), new Connection(1, 2) }, // Two separate curves
                TileType.Intersection => new[] { new Connection(0, 1, 2) }, // Top, Right, Bottom (T-junction)
                TileType.XIntersection => new[] { new Connection(0, 1, 2, 3) }, // All 4 sides
                TileType.Bridge => new[] { new Connection(0, 2), new Connection(1, 3) }, // Two separate straight paths
                _ => Array.Empty<Connection>()
            };
        }

        public int GetMaxRotations()
        {
            return Type switch
            {
                TileType.Curve => 4,
                TileType.TwoCurves => 2,
                TileType.Intersection => 4,
                TileType.XIntersection => 1,
                TileType.Bridge => 2,
                _ => 4
            };
        }

        public bool Equals(TileData other)
        {
            return Type == other.Type && Rotation == other.Rotation;
        }

        public override bool Equals(object obj)
        {
            return obj is TileData other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Type, Rotation);
        }

        public static bool operator ==(TileData left, TileData right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(TileData left, TileData right)
        {
            return !left.Equals(right);
        }

        public override string ToString()
        {
            return $"{Type}(R{Rotation})";
        }
    }

    /// <summary>
    ///     Represents a connection between tile sides.
    ///     Sides are numbered: 0=top, 1=right, 2=bottom, 3=left
    /// </summary>
    [Serializable]
    public struct Connection
    {
        public IReadOnlyList<int> ConnectedSides { get; }

        public Connection(params int[] sides)
        {
            ConnectedSides = sides.ToList();
        }

        /// <summary>
        ///     Applies rotation to this connection.
        ///     Each side index is rotated clockwise by rotation * 90 degrees.
        /// </summary>
        public Connection WithRotation(int rotation)
        {
            var rotatedSides = ConnectedSides
                .Select(side => (side + rotation) % 4)
                .ToArray();
            return new Connection(rotatedSides);
        }

        public override string ToString()
        {
            return $"[{string.Join(",", ConnectedSides)}]";
        }
    }

    /// <summary>
    ///     Tile types matching the original game.
    /// </summary>
    public enum TileType
    {
        Curve, // 90-degree turn
        TwoCurves, // S-shape (two separate curves)
        Intersection, // T-junction (connects 3 sides)
        XIntersection, // Cross (connects all 4 sides)
        Bridge // Two straight paths crossing (two separate connections)
    }
}