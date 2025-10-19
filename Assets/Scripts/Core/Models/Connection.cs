using System;
using System.Collections.Generic;
using System.Linq;

namespace Core.Models
{
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
}