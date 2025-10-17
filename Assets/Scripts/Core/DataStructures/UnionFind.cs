using System;

namespace Core.DataStructures
{
    /// <summary>
    /// Union-Find (Disjoint Set Union) data structure with path compression and union by rank.
    /// Used for efficiently tracking connected components (paths) in the game.
    /// 
    /// Time Complexity:
    /// - Find: O(α(n)) amortized, where α is the inverse Ackermann function (effectively constant)
    /// - Union: O(α(n)) amortized
    /// - Connected: O(α(n)) amortized
    /// 
    /// This is a MASSIVE improvement over the current O(n) linear search approach.
    /// </summary>
    public class UnionFind
    {
        private readonly int[] _parent;
        private readonly int[] _rank;
        private readonly int _size;
        
        public UnionFind(int size)
        {
            _size = size;
            _parent = new int[size];
            _rank = new int[size];
            Reset();
        }
        
        /// <summary>
        /// Resets the data structure so all elements are in their own set.
        /// </summary>
        public void Reset()
        {
            for (int i = 0; i < _size; i++)
            {
                _parent[i] = i; // Each element is its own parent (root)
                _rank[i] = 0;   // Initial rank is 0
            }
        }
        
        /// <summary>
        /// Finds the root (representative) of the set containing element.
        /// Uses path compression for optimization.
        /// </summary>
        public int Find(int element)
        {
            ValidateElement(element);
            
            // Path compression: make every node on the path point directly to the root
            if (_parent[element] != element)
            {
                _parent[element] = Find(_parent[element]);
            }
            
            return _parent[element];
        }
        
        /// <summary>
        /// Merges the sets containing element1 and element2.
        /// Uses union by rank for optimization.
        /// Returns true if the elements were in different sets (and thus merged).
        /// Returns false if they were already in the same set.
        /// </summary>
        public bool Union(int element1, int element2)
        {
            int root1 = Find(element1);
            int root2 = Find(element2);
            
            // Already in the same set
            if (root1 == root2)
                return false;
            
            // Union by rank: attach smaller tree under larger tree
            if (_rank[root1] < _rank[root2])
            {
                _parent[root1] = root2;
            }
            else if (_rank[root1] > _rank[root2])
            {
                _parent[root2] = root1;
            }
            else
            {
                // Same rank: choose one as root and increment its rank
                _parent[root2] = root1;
                _rank[root1]++;
            }
            
            return true;
        }
        
        /// <summary>
        /// Checks if two elements are in the same set (connected).
        /// </summary>
        public bool Connected(int element1, int element2)
        {
            return Find(element1) == Find(element2);
        }
        
        /// <summary>
        /// Gets the number of distinct sets.
        /// Note: This is O(n) as it requires checking all elements.
        /// </summary>
        public int CountSets()
        {
            int count = 0;
            for (int i = 0; i < _size; i++)
            {
                if (_parent[i] == i) // Element is a root
                    count++;
            }
            return count;
        }
        
        /// <summary>
        /// Gets all elements in the same set as the given element.
        /// Note: This is O(n) as it requires checking all elements.
        /// </summary>
        public int[] GetSet(int element)
        {
            int root = Find(element);
            var result = new System.Collections.Generic.List<int>();
            
            for (int i = 0; i < _size; i++)
            {
                if (Find(i) == root)
                    result.Add(i);
            }
            
            return result.ToArray();
        }
        
        private void ValidateElement(int element)
        {
            if (element < 0 || element >= _size)
                throw new ArgumentOutOfRangeException(nameof(element), 
                    $"Element must be between 0 and {_size - 1}");
        }
        
        public override string ToString()
        {
            var sets = new System.Collections.Generic.Dictionary<int, System.Collections.Generic.List<int>>();
            
            for (int i = 0; i < _size; i++)
            {
                int root = Find(i);
                if (!sets.ContainsKey(root))
                    sets[root] = new System.Collections.Generic.List<int>();
                sets[root].Add(i);
            }
            
            var result = new System.Text.StringBuilder();
            result.AppendLine($"UnionFind ({sets.Count} sets):");
            foreach (var kvp in sets)
            {
                result.AppendLine($"  Set {kvp.Key}: [{string.Join(", ", kvp.Value)}]");
            }
            
            return result.ToString();
        }
    }
}
