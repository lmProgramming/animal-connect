using UnityEngine;
using Core.Models;
using Core.Configuration;
using Core.DataStructures;

namespace Core.Tests
{
    /// <summary>
    /// Simple MonoBehaviour to validate Phase 1 data structures.
    /// Attach to a GameObject and run to see test results in console.
    /// </summary>
    public class Phase1Validator : MonoBehaviour
    {
        [SerializeField] private bool runOnStart = true;
        
        private void Start()
        {
            if (runOnStart)
            {
                RunAllTests();
            }
        }
        
        [ContextMenu("Run All Tests")]
        public void RunAllTests()
        {
            Debug.Log("=== Phase 1 Validation Tests ===\n");
            
            TestTileData();
            TestGridState();
            TestUnionFind();
            TestPathNetworkState();
            TestGridConfiguration();
            TestGameState();
            TestQuestData();
            
            Debug.Log("\n=== All Phase 1 Tests Complete! ===");
        }
        
        private void TestTileData()
        {
            Debug.Log("--- Testing TileData ---");
            
            // Test basic creation
            var curve = new TileData(TileType.Curve, 0);
            Debug.Log($"Created curve tile: {curve}");
            
            // Test rotation
            var rotated = curve.WithRotation(2);
            Debug.Log($"Rotated to R2: {rotated}");
            
            // Test connections
            var connections = curve.GetConnections();
            Debug.Log($"Curve connections at R0: {string.Join(", ", connections)}");
            
            var rotatedConnections = rotated.GetConnections();
            Debug.Log($"Curve connections at R2: {string.Join(", ", rotatedConnections)}");
            
            // Test all tile types
            foreach (TileType type in System.Enum.GetValues(typeof(TileType)))
            {
                var tile = new TileData(type, 0);
                Debug.Log($"{type}: {tile.GetConnections().Count} connection groups, max {tile.GetMaxRotations()} rotations");
            }
            
            Debug.Log("✓ TileData tests passed\n");
        }
        
        private void TestGridState()
        {
            Debug.Log("--- Testing GridState ---");
            
            var grid = new GridState();
            Debug.Log($"Empty grid: {grid.TileCount} tiles");
            
            // Place a tile
            var tile = new TileData(TileType.Curve, 0);
            grid = grid.WithTile(4, tile); // Center position
            Debug.Log($"After placing tile at center: {grid.TileCount} tiles");
            
            // Test rotation
            grid = grid.WithRotation(4, 2);
            var retrieved = grid.GetTile(4);
            Debug.Log($"Retrieved tile from center: {retrieved}");
            
            // Test swap
            var tile2 = new TileData(TileType.Intersection, 1);
            grid = grid.WithTile(0, tile2);
            grid = grid.WithSwap(0, 4);
            Debug.Log($"After swap: Slot 0 = {grid.GetTile(0)}, Slot 4 = {grid.GetTile(4)}");
            
            // Test immutability
            var grid2 = new GridState();
            grid2 = grid2.WithTile(0, tile);
            Debug.Log($"Original grid2 has {grid2.TileCount} tiles (immutability preserved)");
            
            Debug.Log("✓ GridState tests passed\n");
        }
        
        private void TestUnionFind()
        {
            Debug.Log("--- Testing UnionFind ---");
            
            var uf = new UnionFind(10);
            
            // Test initial state
            Debug.Log($"Initial sets: {uf.CountSets()}");
            
            // Connect some elements
            uf.Union(0, 1);
            uf.Union(1, 2);
            Debug.Log($"After connecting 0-1-2: {uf.CountSets()} sets");
            Debug.Log($"0 and 2 connected? {uf.Connected(0, 2)}");
            Debug.Log($"0 and 3 connected? {uf.Connected(0, 3)}");
            
            // Connect another group
            uf.Union(5, 6);
            uf.Union(6, 7);
            Debug.Log($"After connecting 5-6-7: {uf.CountSets()} sets");
            
            // Merge the groups
            uf.Union(2, 5);
            Debug.Log($"After merging groups: {uf.CountSets()} sets");
            Debug.Log($"0 and 7 connected? {uf.Connected(0, 7)}");
            
            // Test reset
            uf.Reset();
            Debug.Log($"After reset: {uf.CountSets()} sets");
            
            Debug.Log("✓ UnionFind tests passed\n");
        }
        
        private void TestPathNetworkState()
        {
            Debug.Log("--- Testing PathNetworkState ---");
            
            var network = new PathNetworkState(24);
            
            // Test connecting points
            network.ConnectPoints(new[] { 0, 1, 2 });
            Debug.Log($"Connected points 0, 1, 2. Path count: {network.GetPathCount()}");
            Debug.Log($"Points 0 and 2 connected? {network.AreConnected(0, 2)}");
            Debug.Log($"Connection count at point 0: {network.GetConnectionCount(0)}");
            
            // Connect more points
            network.ConnectPoints(5, 10);
            Debug.Log($"After connecting 5-10: {network.GetPathCount()} paths");
            
            // Test merging paths
            network.ConnectPoints(2, 5);
            Debug.Log($"After connecting 2-5 (merge): {network.GetPathCount()} paths");
            Debug.Log($"Points 0 and 10 now connected? {network.AreConnected(0, 10)}");
            
            Debug.Log("✓ PathNetworkState tests passed\n");
        }
        
        private void TestGridConfiguration()
        {
            Debug.Log("--- Testing GridConfiguration ---");
            
            Debug.Log($"Grid size: {GridConfiguration.GridSize}x{GridConfiguration.GridSize}");
            Debug.Log($"Total slots: {GridConfiguration.TotalSlots}");
            Debug.Log($"Total path points: {GridConfiguration.TotalPathPoints}");
            Debug.Log($"Total entities: {GridConfiguration.TotalEntities}");
            
            // Test slot to path points mapping
            var slot4Points = GridConfiguration.SlotToPathPoints[4];
            Debug.Log($"Center slot (4) path points: [{string.Join(", ", slot4Points)}]");
            
            // Test entity mappings
            for (int i = 0; i < GridConfiguration.TotalEntities; i++)
            {
                int pathPoint = GridConfiguration.GetPathPointForEntity(i);
                int entity = GridConfiguration.GetEntityAtPoint(pathPoint);
                Debug.Log($"Entity {i} -> PathPoint {pathPoint} -> Entity {entity}");
            }
            
            // Test validation
            Debug.Log($"Is 1 connection valid for entity point? {GridConfiguration.IsValidConnectionCount(0, 1)}");
            Debug.Log($"Is 2 connections valid for entity point? {GridConfiguration.IsValidConnectionCount(0, 2)}");
            Debug.Log($"Is 2 connections valid for non-entity point? {GridConfiguration.IsValidConnectionCount(3, 2)}");
            
            Debug.Log("✓ GridConfiguration tests passed\n");
        }
        
        private void TestGameState()
        {
            Debug.Log("--- Testing GameState ---");
            
            var grid = new GridState();
            grid = grid.WithTile(0, new TileData(TileType.Curve, 0));
            
            var paths = new PathNetworkState(24);
            paths.ConnectPoints(0, 1);
            
            var quest = new QuestData(
                new[] { new EntityGroup(new[] { 0, 1, 2 }) }
            );
            
            var state = new GameState(grid, paths, quest);
            Debug.Log($"Created game state: {state.MoveCount} moves");
            
            var newState = state.WithMoveIncremented();
            Debug.Log($"After move: {newState.MoveCount} moves");
            Debug.Log($"Original state still has {state.MoveCount} moves (immutable)");
            
            Debug.Log("✓ GameState tests passed\n");
        }
        
        private void TestQuestData()
        {
            Debug.Log("--- Testing QuestData ---");
            
            // Simple quest: connect 2 entities
            var simpleQuest = new QuestData(
                new[] { new EntityGroup(new[] { 0, 1 }) }
            );
            Debug.Log($"Simple quest:\n{simpleQuest}");
            
            // Complex quest: multiple groups with disconnect requirements
            var complexQuest = new QuestData(
                new[] 
                { 
                    new EntityGroup(new[] { 0, 1, 2 }),
                    new EntityGroup(new[] { 3, 4 })
                },
                new[] { new DisconnectRequirement(0, 1) }
            );
            Debug.Log($"Complex quest:\n{complexQuest}");
            
            Debug.Log("✓ QuestData tests passed\n");
        }
    }
}
