# Animal Connect - Complete Refactoring Plan

**Date:** October 17, 2025  
**Current State:** Working but with significant architectural debt  
**Goal:** Clean, testable, maintainable architecture with better performance

---

## Executive Summary

The current codebase suffers from:

1. **Tight coupling** between Unity components and game logic
2. **Inefficient algorithms** (full grid recalculation on every move)
3. **Unclear responsibilities** (static methods calling instance methods via singletons)
4. **Poor testability** (game logic requires Unity runtime)
5. **Scattered state management** (no single source of truth)

**Estimated Effort:** 20-30 hours over 2-3 weeks  
**Risk Level:** Medium (game is working, changes are incremental)  
**Testing Strategy:** Create parallel systems, validate equivalence, then switch

---

## Phase 1: Foundation - Pure Data Models (3-4 hours)

**Goal:** Create game logic that doesn't depend on Unity

### Step 1.1: Create Core Data Structures (1 hour)

**Location:** `Assets/Scripts/Core/Models/`

#### Files to Create

```
Core/
├── Models/
│   ├── TileData.cs          // Pure data for tiles
│   ├── GridState.cs         // Immutable grid state
│   ├── PathNetworkState.cs  // Path connection data
│   ├── GameState.cs         // Complete game state
│   └── Move.cs              // Represents a single move
```

#### TileData.cs

```csharp
public enum TileType { Curve, TwoCurves, Intersection, XIntersection, Bridge }

public struct TileData
{
    public TileType Type { get; }
    public int Rotation { get; } // 0-3
    public int GridPosition { get; } // 0-8
    
    // Connection patterns relative to rotation 0
    public IReadOnlyList<Connection> GetConnections();
}

public struct Connection
{
    public IReadOnlyList<int> ConnectedSides { get; } // e.g., [0, 2] for top-bottom
}
```

#### GridState.cs

```csharp
public class GridState
{
    private readonly TileData?[] _tiles; // 9 slots, nullable
    
    public GridState() => _tiles = new TileData?[9];
    
    public TileData? GetTile(int position) => _tiles[position];
    public GridState WithTile(int position, TileData tile) => /* immutable update */
    public GridState WithSwap(int pos1, int pos2) => /* swap two tiles */
    
    public IEnumerable<TileData> GetAllTiles();
}
```

#### PathNetworkState.cs

```csharp
public class PathNetworkState
{
    private readonly UnionFind _pathGroups; // For efficient merging
    private readonly int[] _connectionCounts; // Per path point (24 total)
    
    public int GetPathId(int pathPoint);
    public bool AreConnected(int point1, int point2);
    public IEnumerable<int> GetPointsInPath(int pathId);
    public int GetConnectionCount(int pathPoint);
}
```

**Acceptance Criteria:**

- [ ] All data classes are immutable or use defensive copying
- [ ] No Unity dependencies (UnityEngine namespace)
- [ ] All classes are serializable for save/load later
- [ ] Unit tests can instantiate and manipulate these objects

---

### Step 1.2: Create Configuration Data (1 hour)

**Location:** `Assets/Scripts/Core/Configuration/`

```
Configuration/
├── GridConfiguration.cs      // Grid layout constants
├── TileConfiguration.cs      // Tile type definitions
└── PathPointConfiguration.cs // Path point topology
```

#### GridConfiguration.cs

```csharp
public static class GridConfiguration
{
    public const int GridSize = 3;
    public const int TotalSlots = 9;
    public const int TotalPathPoints = 24;
    
    // Maps grid slot index to its 4 adjacent path points
    public static readonly int[][] SlotToPathPoints = new int[9][];
    
    // Maps path point to entity index (-1 if no entity)
    public static readonly int[] PathPointToEntity = new int[24];
    
    static GridConfiguration()
    {
        InitializeTopology();
    }
    
    private static void InitializeTopology() { /* ... */ }
}
```

#### TileConfiguration.cs

```csharp
public static class TileConfiguration
{
    public static readonly Dictionary<TileType, TileTypeInfo> TileTypes;
    
    public struct TileTypeInfo
    {
        public int MaxRotations; // 1, 2, or 4
        public Connection[] Connections; // Base connection pattern
    }
}
```

**Acceptance Criteria:**

- [ ] All magic numbers from current code are centralized here
- [ ] Grid topology is defined once and used everywhere
- [ ] Easy to modify for different grid sizes (future-proofing)

---

### Step 1.3: Implement Union-Find for Path Merging (1-2 hours)

**Location:** `Assets/Scripts/Core/DataStructures/UnionFind.cs`

```csharp
/// <summary>
/// Efficient data structure for tracking connected components (paths)
/// Provides near O(1) merge and find operations
/// </summary>
public class UnionFind
{
    private readonly int[] _parent;
    private readonly int[] _rank;
    
    public UnionFind(int size);
    public int Find(int element); // Find root with path compression
    public void Union(int element1, int element2); // Merge two sets
    public bool Connected(int element1, int element2);
    public void Reset();
}
```

**Why This Matters:**

- Current: O(n) to merge paths (iterate all 24 points)
- With Union-Find: O(α(n)) ≈ O(1) where α is inverse Ackermann (effectively constant)
- **Performance gain:** 24x faster path merging

**Acceptance Criteria:**

- [ ] Implements path compression optimization
- [ ] Implements union by rank optimization
- [ ] Has comprehensive unit tests
- [ ] Benchmarked against current approach

---

## Phase 2: Core Game Logic (5-7 hours)

**Goal:** Extract all game rules into testable, pure functions

### Step 2.1: Path Calculator (2-3 hours)

**Location:** `Assets/Scripts/Core/Logic/PathCalculator.cs`

```csharp
public class PathCalculator
{
    private readonly GridConfiguration _config;
    
    /// <summary>
    /// Calculates all path connections from current grid state
    /// Returns a PathNetworkState representing all connections
    /// </summary>
    public PathNetworkState CalculatePathNetwork(GridState gridState)
    {
        var network = new PathNetworkState();
        
        // For each grid slot with a tile
        foreach (var (slot, tile) in gridState.GetAllTiles())
        {
            // Get the 4 path points adjacent to this slot
            var pathPoints = _config.SlotToPathPoints[slot];
            
            // Get tile's connections in current rotation
            var connections = GetRotatedConnections(tile);
            
            // For each connection group (e.g., [1,2] for curve)
            foreach (var group in connections)
            {
                // Map tile sides to actual path points
                var actualPoints = group.Select(side => pathPoints[side]);
                
                // Connect all points in this group
                ConnectPoints(network, actualPoints);
            }
        }
        
        return network;
    }
    
    /// <summary>
    /// Incrementally updates path network when one tile changes
    /// Much faster than full recalculation
    /// </summary>
    public PathNetworkState UpdateForTileChange(
        PathNetworkState currentNetwork,
        GridState oldGrid,
        GridState newGrid,
        int changedSlot)
    {
        // Only recalculate affected paths
        // Implementation details...
    }
    
    private IEnumerable<int[]> GetRotatedConnections(TileData tile)
    {
        // Apply rotation to base connection pattern
    }
}
```

**Key Improvements:**

1. **Incremental updates** - Only recalculate when tile changes
2. **Pure function** - No side effects, returns new state
3. **Testable** - Can unit test with mock GridState
4. **Clear algorithm** - One method, one purpose

**Acceptance Criteria:**

- [ ] Produces identical results to current system
- [ ] Unit tests cover all tile types and rotations
- [ ] Incremental update is 5-10x faster than full recalculation
- [ ] Handles edge cases (empty slots, bridge tiles)

---

### Step 2.2: Connection Validator (1 hour)

**Location:** `Assets/Scripts/Core/Logic/ConnectionValidator.cs`

```csharp
public class ConnectionValidator
{
    /// <summary>
    /// Validates that all path connections follow game rules
    /// </summary>
    public ValidationResult ValidateConnections(PathNetworkState network)
    {
        var errors = new List<ValidationError>();
        
        for (int i = 0; i < 24; i++)
        {
            int connections = network.GetConnectionCount(i);
            bool isEntity = GridConfiguration.PathPointToEntity[i] != -1;
            
            if (isEntity && connections != 1)
            {
                errors.Add(new ValidationError(
                    $"Entity at point {i} must have exactly 1 connection, has {connections}"
                ));
            }
            else if (!isEntity && connections != 0 && connections != 2)
            {
                errors.Add(new ValidationError(
                    $"Non-entity point {i} must have 0 or 2 connections, has {connections}"
                ));
            }
        }
        
        return new ValidationResult(errors);
    }
}

public struct ValidationResult
{
    public bool IsValid => Errors.Count == 0;
    public IReadOnlyList<ValidationError> Errors { get; }
}
```

**Acceptance Criteria:**

- [ ] Replaces `CheckIfValidPaths()` logic
- [ ] Returns detailed error information (for debugging)
- [ ] Can be used for hint system later

---

### Step 2.3: Quest Evaluator (2 hours)

**Location:** `Assets/Scripts/Core/Logic/QuestEvaluator.cs`

```csharp
public class QuestEvaluator
{
    /// <summary>
    /// Checks if current path configuration satisfies quest requirements
    /// </summary>
    public QuestResult EvaluateQuest(Quest quest, PathNetworkState network)
    {
        // Check all "must connect" requirements
        foreach (var requirement in quest.EntitiesToConnect)
        {
            if (!AreAllConnected(requirement.EntityIds, network))
            {
                return QuestResult.Incomplete("Not all entities connected");
            }
        }
        
        // Check all "must disconnect" requirements  
        foreach (var (group1, group2) in quest.PathsToDisconnect)
        {
            var path1 = GetPathForEntityGroup(group1, network);
            var path2 = GetPathForEntityGroup(group2, network);
            
            if (path1 == path2)
            {
                return QuestResult.Failed("Groups should not be connected");
            }
        }
        
        return QuestResult.Success();
    }
    
    private bool AreAllConnected(IEnumerable<int> entityIds, PathNetworkState network)
    {
        var pathPoints = entityIds.Select(id => 
            GridConfiguration.EntityToPathPoint[id]
        );
        
        var firstPath = network.GetPathId(pathPoints.First());
        
        return pathPoints.All(p => network.GetPathId(p) == firstPath);
    }
}

public struct QuestResult
{
    public bool IsComplete { get; }
    public bool IsSuccessful { get; }
    public string Message { get; }
}
```

**Acceptance Criteria:**

- [ ] Replaces `Quest.CheckIfCompleted()` logic
- [ ] More detailed feedback than current system
- [ ] Can evaluate partial progress (for UI)

---

### Step 2.4: Move Processor (1-2 hours)

**Location:** `Assets/Scripts/Core/Logic/MoveProcessor.cs`

```csharp
public class MoveProcessor
{
    private readonly PathCalculator _pathCalculator;
    private readonly ConnectionValidator _validator;
    private readonly QuestEvaluator _questEvaluator;
    
    /// <summary>
    /// Processes a move and returns the new game state
    /// Pure function - no side effects
    /// </summary>
    public MoveResult ProcessMove(GameState currentState, Move move)
    {
        // Apply the move to grid state
        var newGridState = ApplyMove(currentState.Grid, move);
        
        // Recalculate paths (incrementally if possible)
        var newPathState = _pathCalculator.UpdateForTileChange(
            currentState.Paths,
            currentState.Grid,
            newGridState,
            move.AffectedSlot
        );
        
        // Validate connections
        var validation = _validator.ValidateConnections(newPathState);
        
        // Check quest completion
        var questResult = _questEvaluator.EvaluateQuest(
            currentState.Quest,
            newPathState
        );
        
        var newGameState = new GameState(newGridState, newPathState, currentState.Quest);
        
        return new MoveResult(
            newGameState,
            validation,
            questResult,
            move
        );
    }
    
    private GridState ApplyMove(GridState grid, Move move)
    {
        return move.Type switch
        {
            MoveType.Rotate => grid.WithRotation(move.Slot, move.Rotation),
            MoveType.Swap => grid.WithSwap(move.Slot, move.TargetSlot),
            _ => throw new ArgumentException("Unknown move type")
        };
    }
}

public struct Move
{
    public MoveType Type { get; }
    public int Slot { get; }
    public int? TargetSlot { get; } // For swaps
    public int? Rotation { get; } // For rotates
}

public enum MoveType { Rotate, Swap }

public struct MoveResult
{
    public GameState NewState { get; }
    public ValidationResult Validation { get; }
    public QuestResult QuestResult { get; }
    public Move Move { get; }
    
    public bool IsWinningMove => Validation.IsValid && QuestResult.IsSuccessful;
}
```

**Key Benefits:**

1. **Testable** - Pure function, easy to unit test
2. **Replayable** - Can reconstruct game from move history
3. **Undoable** - Keep old state, can implement undo
4. **Analyzable** - Can evaluate moves without applying them (AI/hints)

**Acceptance Criteria:**

- [ ] Handles all move types
- [ ] Returns complete information about move result
- [ ] Can be used to preview moves before committing
- [ ] Performance is equal or better than current system

---

## Phase 3: Adapters - Bridge to Unity (4-5 hours)

**Goal:** Create thin adapters that connect pure logic to Unity components

### Step 3.1: Game State Manager (2 hours)

**Location:** `Assets/Scripts/Managers/GameStateManager.cs`

```csharp
/// <summary>
/// Manages the authoritative game state
/// Bridges between Unity components and core logic
/// </summary>
public class GameStateManager : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private GridView _gridView;
    [SerializeField] private QuestVisualizer _questVisualizer;
    
    // Core logic (no Unity dependencies)
    private readonly MoveProcessor _moveProcessor;
    private readonly PathCalculator _pathCalculator;
    
    // Current state
    private GameState _currentState;
    private List<MoveResult> _moveHistory;
    
    // Events for loose coupling
    public event Action<GameState> OnStateChanged;
    public event Action<MoveResult> OnMoveMade;
    public event Action OnGameWon;
    
    public void Initialize(Quest quest)
    {
        var initialGrid = new GridState();
        var initialPaths = _pathCalculator.CalculatePathNetwork(initialGrid);
        
        _currentState = new GameState(initialGrid, initialPaths, quest);
        _moveHistory = new List<MoveResult>();
        
        OnStateChanged?.Invoke(_currentState);
    }
    
    public void ProcessMove(Move move)
    {
        var result = _moveProcessor.ProcessMove(_currentState, move);
        
        _currentState = result.NewState;
        _moveHistory.Add(result);
        
        OnMoveMade?.Invoke(result);
        OnStateChanged?.Invoke(_currentState);
        
        if (result.IsWinningMove)
        {
            OnGameWon?.Invoke();
        }
    }
    
    public bool CanUndo => _moveHistory.Count > 0;
    
    public void UndoLastMove()
    {
        if (!CanUndo) return;
        
        _moveHistory.RemoveAt(_moveHistory.Count - 1);
        // Reconstruct state from history
        // Or keep state stack
    }
    
    public GameState CurrentState => _currentState;
}
```

**Acceptance Criteria:**

- [ ] Replaces GameManager's game logic responsibilities
- [ ] Uses events instead of direct coupling
- [ ] Supports undo (bonus feature)
- [ ] Can serialize/deserialize for save/load

---

### Step 3.2: Grid View Adapter (1-2 hours)

**Location:** `Assets/Scripts/Views/GridView.cs`

```csharp
/// <summary>
/// Manages the visual representation of the grid
/// Syncs with GameState but doesn't contain game logic
/// </summary>
public class GridView : MonoBehaviour
{
    [SerializeField] private GridSlotView[] _slotViews;
    [SerializeField] private TileView _tilePrefab;
    
    private Dictionary<int, TileView> _tileViews;
    
    public void Initialize()
    {
        _tileViews = new Dictionary<int, TileView>();
    }
    
    public void UpdateFromState(GameState state)
    {
        for (int i = 0; i < 9; i++)
        {
            var tileData = state.Grid.GetTile(i);
            
            if (tileData.HasValue)
            {
                UpdateTileView(i, tileData.Value);
            }
            else
            {
                RemoveTileView(i);
            }
        }
    }
    
    private void UpdateTileView(int slot, TileData data)
    {
        if (!_tileViews.TryGetValue(slot, out var view))
        {
            view = CreateTileView(slot);
            _tileViews[slot] = view;
        }
        
        view.SetType(data.Type);
        view.SetRotation(data.Rotation);
        view.SetPosition(_slotViews[slot].Position);
    }
}

/// <summary>
/// Individual grid slot visual representation
/// </summary>
public class GridSlotView : MonoBehaviour
{
    public Vector2 Position => transform.position;
    public int SlotIndex { get; set; }
}

/// <summary>
/// Individual tile visual representation
/// </summary>
public class TileView : MonoBehaviour
{
    [SerializeField] private Image _image;
    [SerializeField] private TileSprites _sprites;
    
    public void SetType(TileType type)
    {
        _image.sprite = _sprites.GetSprite(type);
    }
    
    public void SetRotation(int rotation)
    {
        transform.rotation = Quaternion.Euler(0, 0, -rotation * 90);
    }
    
    public void SetPosition(Vector2 position)
    {
        transform.position = position;
    }
}
```

**Acceptance Criteria:**

- [ ] GridSlot no longer contains game logic
- [ ] Only responsible for visual representation
- [ ] Receives updates via events or direct calls
- [ ] Smooth animations between states

---

### Step 3.3: Input Handler Refactor (1 hour)

**Location:** `Assets/Scripts/Input/TileInputHandler.cs`

```csharp
/// <summary>
/// Handles tile interaction and converts to Move objects
/// No game logic, just input translation
/// </summary>
public class TileInputHandler : MonoBehaviour
{
    [SerializeField] private GameStateManager _stateManager;
    [SerializeField] private GridView _gridView;
    
    public event Action<Move> OnMoveRequested;
    
    private TileView _draggedTile;
    private float _dragStartTime;
    private int _draggedSlot;
    
    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            HandlePointerDown();
        }
        else if (Input.GetMouseButtonUp(0) && _draggedTile != null)
        {
            HandlePointerUp();
        }
        else if (_draggedTile != null)
        {
            HandleDrag();
        }
    }
    
    private void HandlePointerDown()
    {
        var slot = GetSlotUnderPointer();
        if (slot == -1) return;
        
        _draggedTile = _gridView.GetTileAt(slot);
        _draggedSlot = slot;
        _dragStartTime = Time.time;
    }
    
    private void HandlePointerUp()
    {
        float dragDuration = Time.time - _dragStartTime;
        
        if (dragDuration < 0.2f)
        {
            // Quick tap = rotate
            var move = new Move
            {
                Type = MoveType.Rotate,
                Slot = _draggedSlot,
                Rotation = (_stateManager.CurrentState.Grid
                    .GetTile(_draggedSlot).Value.Rotation + 1) % 4
            };
            OnMoveRequested?.Invoke(move);
        }
        else
        {
            // Long drag = swap
            var targetSlot = GetSlotUnderPointer();
            if (targetSlot != -1 && targetSlot != _draggedSlot)
            {
                var move = new Move
                {
                    Type = MoveType.Swap,
                    Slot = _draggedSlot,
                    TargetSlot = targetSlot
                };
                OnMoveRequested?.Invoke(move);
            }
        }
        
        _draggedTile = null;
    }
}
```

**Acceptance Criteria:**

- [ ] TileDragger logic moved here
- [ ] Emits Move objects instead of directly manipulating state
- [ ] Decoupled from game logic
- [ ] Easy to add new input methods (keyboard, gamepad)

---

## Phase 4: Migration Strategy (3-5 hours)

**Goal:** Safely transition from old to new architecture

### Step 4.1: Parallel Systems (2 hours)

**Strategy:** Run both old and new systems simultaneously, compare results

```csharp
public class MigrationValidator : MonoBehaviour
{
    [Header("Old System")]
    [SerializeField] private MyGrid _oldGrid;
    [SerializeField] private GameManager _oldGameManager;
    
    [Header("New System")]
    [SerializeField] private GameStateManager _newStateManager;
    
    [Header("Debug")]
    [SerializeField] private bool _useNewSystem = false;
    [SerializeField] private bool _validateEquivalence = true;
    
    private void OnMoveMade()
    {
        if (_validateEquivalence)
        {
            ValidateEquivalence();
        }
    }
    
    private void ValidateEquivalence()
    {
        // Compare old and new path states
        var oldPaths = ExtractPathStateFromOldSystem();
        var newPaths = _newStateManager.CurrentState.Paths;
        
        if (!AreEquivalent(oldPaths, newPaths))
        {
            Debug.LogError("Path state mismatch between old and new systems!");
            LogDetailedDiff(oldPaths, newPaths);
        }
        
        // Compare win conditions
        var oldWon = _oldGameManager.CheckIfWon();
        var newWon = _newStateManager.CurrentState.IsVictory;
        
        if (oldWon != newWon)
        {
            Debug.LogError($"Win condition mismatch! Old: {oldWon}, New: {newWon}");
        }
    }
}
```

**Steps:**

1. Add new system alongside old system
2. Process moves through both systems
3. Compare results after each move
4. Fix any discrepancies
5. Once validated, flip toggle to use new system
6. Remove old system

---

### Step 4.2: Incremental Class Replacement (2-3 hours)

**Order of replacement:**

1. **Phase 4.2.1: Data Models First**
   - Create new data classes
   - Keep old MonoBehaviour classes
   - Add conversion methods: `OldTile.ToTileData()`, `TileData.ToOldTile()`

2. **Phase 4.2.2: Logic Extraction**
   - Extract logic from `MyGrid.RecalculatePathConnections()` → `PathCalculator`
   - Extract logic from `GridSlot.UpdateConnections()` → `PathCalculator`
   - Keep old methods but call new logic internally

3. **Phase 4.2.3: State Management**
   - Create `GameStateManager`
   - Have `GameManager` delegate to `GameStateManager`
   - Maintain backward compatibility

4. **Phase 4.2.4: View Layer**
   - Create new View classes
   - Old MonoBehaviours remain but become shells
   - Forward all calls to new View classes

5. **Phase 4.2.5: Final Cleanup**
   - Remove old MonoBehaviour logic
   - Remove conversion methods
   - Delete deprecated classes

---

## Phase 5: Testing & Validation (3-4 hours)

### Step 5.1: Unit Tests (2 hours)

**Location:** `Assets/Tests/Core/`

```
Tests/
├── Core/
│   ├── Models/
│   │   ├── GridStateTests.cs
│   │   ├── TileDataTests.cs
│   │   └── PathNetworkStateTests.cs
│   ├── Logic/
│   │   ├── PathCalculatorTests.cs
│   │   ├── ConnectionValidatorTests.cs
│   │   ├── QuestEvaluatorTests.cs
│   │   └── MoveProcessorTests.cs
│   └── DataStructures/
│       └── UnionFindTests.cs
```

**Key Test Cases:**

```csharp
[TestFixture]
public class PathCalculatorTests
{
    [Test]
    public void CalculatePathNetwork_SingleCurveTile_ConnectsTwoPoints()
    {
        // Arrange
        var grid = new GridState();
        grid = grid.WithTile(0, new TileData(TileType.Curve, 0));
        var calculator = new PathCalculator();
        
        // Act
        var network = calculator.CalculatePathNetwork(grid);
        
        // Assert
        Assert.IsTrue(network.AreConnected(
            pathPoint1: 13, // Right of slot 0
            pathPoint2: 3   // Bottom of slot 0
        ));
    }
    
    [Test]
    public void CalculatePathNetwork_MatchesOldSystem()
    {
        // Arrange: Set up same scenario in both systems
        var oldGrid = CreateOldStyleGrid();
        var newGrid = ConvertToNewGrid(oldGrid);
        
        // Act
        oldGrid.RecalculatePathConnections();
        var newNetwork = new PathCalculator().CalculatePathNetwork(newGrid);
        
        // Assert: Compare results
        for (int i = 0; i < 24; i++)
        {
            for (int j = i + 1; j < 24; j++)
            {
                bool oldConnected = oldGrid.pathPoints[i].pathNum == 
                                   oldGrid.pathPoints[j].pathNum;
                bool newConnected = newNetwork.AreConnected(i, j);
                
                Assert.AreEqual(oldConnected, newConnected,
                    $"Mismatch for points {i} and {j}");
            }
        }
    }
}
```

**Test Coverage Goals:**

- [ ] 90%+ coverage of Core logic
- [ ] All tile types tested
- [ ] All rotation states tested
- [ ] Edge cases (empty grid, full grid, bridge tiles)
- [ ] Performance benchmarks

---

### Step 5.2: Integration Tests (1 hour)

**Location:** `Assets/Tests/Integration/`

```csharp
[TestFixture]
public class FullGameFlowTests
{
    [Test]
    public void CompleteSimpleQuest_FromStartToWin()
    {
        // Arrange: Set up a simple winnable puzzle
        var quest = CreateSimpleQuest(); // Connect 2 entities
        var stateManager = new GameStateManager();
        stateManager.Initialize(quest);
        
        bool wonEventFired = false;
        stateManager.OnGameWon += () => wonEventFired = true;
        
        // Act: Make winning moves
        stateManager.ProcessMove(new Move(MoveType.Rotate, 0, rotation: 1));
        stateManager.ProcessMove(new Move(MoveType.Rotate, 1, rotation: 2));
        // ... more moves
        
        // Assert
        Assert.IsTrue(wonEventFired);
        Assert.IsTrue(stateManager.CurrentState.IsVictory);
    }
}
```

---

### Step 5.3: Performance Tests (1 hour)

```csharp
[TestFixture]
public class PerformanceTests
{
    [Test]
    public void PathCalculation_IsSignificantlyFaster()
    {
        var grid = CreateComplexGrid();
        
        // Old system
        var oldTime = MeasureTime(() => {
            oldGrid.RecalculatePathConnections();
        });
        
        // New system
        var newTime = MeasureTime(() => {
            calculator.CalculatePathNetwork(newGrid);
        });
        
        Debug.Log($"Old: {oldTime}ms, New: {newTime}ms, Speedup: {oldTime/newTime}x");
        
        Assert.Less(newTime, oldTime * 0.5, 
            "New system should be at least 2x faster");
    }
    
    [Test]
    public void IncrementalUpdate_MuchFasterThanFullRecalculation()
    {
        // Test incremental vs full recalculation
        // Should be 5-10x faster
    }
}
```

---

## Phase 6: Polish & Future-Proofing (2-3 hours)

### Step 6.1: Add Features Enabled by New Architecture (1 hour)

Now that we have clean architecture, these become easy:

1. **Undo/Redo**

   ```csharp
   public void Undo() => RestoreState(_moveHistory[^2].NewState);
   public void Redo() => ProcessMove(_redoStack.Pop());
   ```

2. **Save/Load**

   ```csharp
   public string SaveGame() => JsonUtility.ToJson(_currentState);
   public void LoadGame(string json) => RestoreState(
       JsonUtility.FromJson<GameState>(json)
   );
   ```

3. **Hint System**

   ```csharp
   public Move GetHint()
   {
       foreach (var possibleMove in GeneratePossibleMoves())
       {
           var result = _moveProcessor.ProcessMove(_currentState, possibleMove);
           if (result.QuestResult.Progress > _currentState.Progress)
               return possibleMove;
       }
   }
   ```

4. **AI Solver**
   - Can now use A* search with proper state representation
   - Can evaluate moves without side effects
   - Can backtrack efficiently

---

### Step 6.2: Documentation (1 hour)

**Create:**

- [ ] Architecture diagram (before/after)
- [ ] API documentation for core classes
- [ ] Migration guide for future developers
- [ ] Performance comparison metrics

---

### Step 6.3: Code Cleanup (1 hour)

- [ ] Remove all commented-out code
- [ ] Remove old deprecated classes
- [ ] Consistent naming conventions
- [ ] XML documentation on public APIs
- [ ] Remove debug logs

---

## Risk Mitigation

### High-Risk Areas

1. **Path Calculation Algorithm**
   - **Risk:** New algorithm produces different results
   - **Mitigation:** Parallel validation, extensive testing
   - **Rollback:** Keep old system until 100% verified

2. **Performance Regression**
   - **Risk:** New system is slower
   - **Mitigation:** Benchmark at each phase
   - **Rollback:** Optimize or revert specific components

3. **Unity Integration Issues**
   - **Risk:** Serialization, scene references break
   - **Mitigation:** Test in actual scenes early
   - **Rollback:** Maintain adapter pattern

### Medium-Risk Areas

1. **Quest Evaluation Edge Cases**
   - **Risk:** Miss some quest validation logic
   - **Mitigation:** Comprehensive test suite

2. **Input Handling Changes**
   - **Risk:** User experience changes
   - **Mitigation:** Maintain exact same behavior

---

## Success Metrics

### Before vs After Comparison

| Metric | Before | Target After | Measurement |
|--------|--------|--------------|-------------|
| Path recalculation time | ~2-5ms | <0.5ms | Benchmark |
| Move processing time | ~3-8ms | <1ms | Benchmark |
| Lines of code in GameManager | ~120 | ~50 | LOC counter |
| Unit test coverage | 0% | 90%+ | Coverage tool |
| Cyclomatic complexity (avg) | 15+ | <5 | Analyzer |
| Coupling between classes | High | Low | Dependency graph |

---

## Timeline & Milestones

### Week 1: Foundation (Days 1-5)

- **Day 1-2:** Phase 1 - Data models and configuration
- **Day 3-4:** Phase 2 - Core logic extraction
- **Day 5:** Phase 3 - Start adapters

### Week 2: Integration (Days 6-10)

- **Day 6-7:** Phase 3 - Complete adapters
- **Day 8-9:** Phase 4 - Migration and validation
- **Day 10:** Phase 5 - Testing

### Week 3: Polish (Days 11-14)

- **Day 11:** Phase 5 - Complete testing
- **Day 12:** Phase 6 - Polish and new features
- **Day 13:** Documentation
- **Day 14:** Buffer for issues, final validation

---

## Appendix A: Class Dependency Graph

### Current Architecture (Tangled)

```
GameManager (Singleton)
    ↕ (bidirectional)
MyGrid (MonoBehaviour) ←→ GridSlot (MonoBehaviour)
    ↕                          ↕
TileDragger (Singleton) ←→ Tile (MonoBehaviour)
    ↓
GameInput (Singleton)
```

### Target Architecture (Clean Layers)

```
┌─────────────────────────────────────────┐
│         Unity Presentation Layer         │
│  GameController │ GridView │ TileView   │
└─────────────┬───────────────────────────┘
              │ (events)
┌─────────────▼───────────────────────────┐
│        Game State Management Layer       │
│         GameStateManager                 │
└─────────────┬───────────────────────────┘
              │ (pure functions)
┌─────────────▼───────────────────────────┐
│           Core Logic Layer               │
│  PathCalculator │ MoveProcessor │        │
│  QuestEvaluator │ ConnectionValidator    │
└─────────────┬───────────────────────────┘
              │
┌─────────────▼───────────────────────────┐
│            Data Layer                    │
│  GameState │ GridState │ PathNetwork    │
└──────────────────────────────────────────┘
```

---

## Appendix B: Key Algorithms Explained

### Current Path Merging (O(n²) worst case)

```
For each move:
  1. Reset all 24 path points → O(24)
  2. For each of 9 grid slots → O(9)
     3. For each connection in tile → O(4)
        4. Merge paths by iterating all 24 points → O(24)
        
Total: 24 + (9 * 4 * 24) = 888 operations per move
```

### New Path Merging (O(1) amortized)

```
For each move:
  1. Get affected path points (4 max) → O(1)
  2. For each connection → O(4)
     3. Union-Find merge → O(α(n)) ≈ O(1)
     
Total: ~4 operations per move (222x faster!)
```

---

## Appendix C: Example Refactored Method

### Before: MyGrid.RecalculatePathConnections()

```csharp
public void RecalculatePathConnections()
{
    _numberOfPaths = 0;

    for (var i = 0; i < pathPoints.Length; i++)
    {
        pathPoints[i].pathNum = -1;
        pathPoints[i].ResetConectionsNumber();
    }

    for (var i = 0; i < gridSlots.Length; i++) 
        gridSlots[i].UpdateConnections(this);
}
```

**Issues:** Mutates state, O(n²), uses "this" parameter, unclear responsibilities

### After: PathCalculator.CalculatePathNetwork()

```csharp
public PathNetworkState CalculatePathNetwork(GridState grid)
{
    var network = new PathNetworkState(24); // 24 path points
    
    for (int slot = 0; slot < 9; slot++)
    {
        var tile = grid.GetTile(slot);
        if (!tile.HasValue) continue;
        
        var connections = GetRotatedConnections(tile.Value);
        var pathPoints = GridConfiguration.SlotToPathPoints[slot];
        
        foreach (var connectionGroup in connections)
        {
            var points = connectionGroup.Select(side => pathPoints[side]);
            network.ConnectPoints(points);
        }
    }
    
    return network;
}
```

**Improvements:** Pure function, returns new state, clear algorithm, O(n) complexity

---

## Questions & Answers

**Q: Will this break existing scenes?**  
A: Not if we use the migration strategy. Adapters maintain the same public interface.

**Q: How long until we see benefits?**  
A: Immediately after Phase 2. Better code organization helps even before full migration.

**Q: Can we do this incrementally?**  
A: Yes! Each phase can be done independently. You can stop at any phase and still have improvements.

**Q: What if we find issues mid-refactor?**  
A: The parallel systems approach means we can always fall back to the old system.

**Q: Will this make adding features easier?**  
A: Dramatically! Undo, save/load, hints, AI - all become simple after refactoring.

---

## Conclusion

This refactoring will transform Animal Connect from a working prototype into a maintainable, extensible, professional codebase. The investment of 20-30 hours will pay dividends in:

- **Faster development** of new features
- **Easier debugging** with clear separation of concerns  
- **Better performance** with optimized algorithms
- **Testable code** enabling confidence in changes
- **Professional architecture** suitable for portfolio/publication

**Recommended Approach:** Start with Phase 1 this week. It's low-risk, high-value, and will immediately improve code quality even if you don't complete all phases.
